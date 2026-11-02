using LinePutScript.Localization.WPF;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Panuon.WPF.UI;
using Panuon.WPF.UI.Configurations;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Resources;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows;

namespace VPet_Simulator.Windows;

internal static class WorkshopProbablyCheck
{
    // 只读 plugin 下的 DLL，不解析外部依赖，不执行资源对象或插件代码。
    private const int MaxFiles = 512, MaxFileBytes = 256 * 1024 * 1024;
    private const long MaxTotalBytes = 1024L * 1024 * 1024;
    private const int MaxResourceBytes = 32 * 1024 * 1024, MaxDetails = 1000;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal sealed class PluginFile
    {
        public string Path = "", RelativePath = "", Hash = "", Publisher = "";
        public bool Trusted, Official, Blocked;
    }

    internal sealed class Finding
    {
        public string Category = "", File = "", Method = "", Api = "";
        public bool Compatibility;
    }

    internal sealed class PluginScanResult
    {
        public readonly List<PluginFile> Files = new();
        public readonly List<Finding> Findings = new();
        public readonly List<string> BlockReasons = new();
        public readonly HashSet<string> Categories = new();
        private readonly HashSet<(string File, string Category)> findingKeys = new();
        public bool DetailsTruncated;
        private bool hasUnanalyzedContentBlock;
        public long ElapsedMilliseconds;
        public bool Blocked => BlockReasons.Count != 0;
        public bool AllTrusted => Files.Count != 0 && Files.All(x => x.Trusted);

        public void Add(string category, string file, string method = "", string api = "", bool compatibility = false, MemberReference? member = null)
        {
            Categories.Add(category);
            // 同一 DLL 的同类能力只记录一次；保留一个定位样本供诊断，不逐条展示调用。
            if (findingKeys.Contains((file, category))) return;
            if (Findings.Count >= MaxDetails) { DetailsTruncated = true; return; }
            findingKeys.Add((file, category));
            if (member is MethodReference target) api = target.FullName + " [" + target.DeclaringType.Scope.Name + "]";
            else if (member != null) api = member.FullName;
            Findings.Add(new Finding { Category = category, File = file, Method = method, Api = api, Compatibility = compatibility });
        }

        public void BlockEntryConflict(PluginFile file, string loadedPath)
        {
            file.Blocked = true;
            BlockReasons.Add("LOAD_CONFLICT: " + "插件入口 DLL 冲突, 本次无法加载这个插件。".Translate() +
                Environment.NewLine + "要加载的文件: {0}".Translate(SafeText(file.Path)) +
                Environment.NewLine + "已加载的文件: {0}".Translate(SafeText(loadedPath)));
        }

        public void BlockUnanalyzedContent(string reason)
        {
            hasUnanalyzedContentBlock = true;
            BlockReasons.Add(reason);
        }

        public string Report()
        {
            var text = new StringBuilder();
            text.AppendLine((Blocked ? "代码检测发现高危险项, 已阻止加载。" : "代码检测出以下结果:").Translate());
            var limited = new List<string>(3);
            if (hasUnanalyzedContentBlock) limited.Add("高风险（未能检查的 DLL 或资源）".Translate());
            if (Categories.Contains("调用原生函数") || Categories.Contains("包含原生代码")) limited.Add("中风险（原生代码）".Translate());
            if (Categories.Contains("包含 WPF 界面资源")) limited.Add("低风险（WPF 界面资源）".Translate());
            if (limited.Count != 0) text.AppendLine("【无法分析内部行为】".Translate() + " " + string.Join(", ", limited));
            if (Blocked)
            {
                text.AppendLine("- " + "高危险行为".Translate() + ":\t");
                foreach (string reason in BlockReasons) text.AppendLine(reason);
            }
            void AppendCategories(IEnumerable<string> categories, string prefix = "")
            {
                foreach (var group in categories.GroupBy(RiskLevel).OrderByDescending(x => x.Key))
                    text.AppendLine(prefix + RiskLabels[group.Key].Translate() + ":\t" + string.Join(", ", group.Select(x => x.Translate())));
            }
            AppendCategories(Categories, "- ");
            if (!Blocked && Files.Count == 0) text.AppendLine("没有需要检测的代码 DLL".Translate());

            var findings = Findings.ToLookup(x => x.File, StringComparer.OrdinalIgnoreCase);
            foreach (var file in Files.OrderByDescending(x => x.Blocked ? 3 :
                findings[x.RelativePath].Select(x => RiskLevel(x.Category)).DefaultIfEmpty(-1).Max()))
            {
                text.AppendLine();
                text.AppendLine("> " + SafeText(file.RelativePath));
                if (file.Blocked) text.AppendLine("已阻止加载".Translate());
                if (file.Trusted)
                    text.AppendLine($"{(file.Official ? "官方认证" : "有效签名").Translate()} — {SafeText(file.Publisher)}");
                else if (!findings[file.RelativePath].Any() && !file.Blocked)
                    text.AppendLine((DetailsTruncated ? "行为详情未完整显示" : "未检测到需要提示的行为").Translate());
                AppendCategories(findings[file.RelativePath].Select(x => x.Category));
            }
            if (DetailsTruncated) text.AppendLine("详情过多, 仅显示前 1000 项；检测仍覆盖全部 DLL。".Translate());
            return text.ToString();
        }
    }

    // 仅用于报告分级和排序，不改变签名放行、授权或阻止规则。
    // 高危险栏目展示已有阻止项；解析失败仍保留原原因，不推断成已执行的危险操作。
    private static readonly string[] RiskLabels = { "普通行为", "低风险行为", "中危险行为" };

    private static int RiskLevel(string category) => category switch
    {
        "加载 XAML 或界面资源" or
        "保存游戏数据, 可能同步云存档" or "重启桌宠" or
        "导出图片文件" or "读取图片资源" or "访问桌宠设置" => 0,
        "读取 MOD 目录" or "写入 MOD 数据目录" or "读取桌宠目录" or "写入桌宠目录" or "打开桌宠目录内的文件（读写方式不明）" or "写入系统临时目录" or "监视文件变化" or
        "包含 WPF 界面资源" or "读取注册表" or "访问网络" or "检查剪贴板格式" or "修改剪贴板" or
        "向其他玩家发送数据" or "通过 Steam 收发数据" or "使用 Steam 语音" => 1,
        _ => 2
    };

    private sealed class ScanBudget
    {
        public readonly Stopwatch Watch = new();
        public CancellationToken Cancellation;
        public int Methods, Instructions, Resources;
        public long Bytes, ResourceBytes;
        public void Check()
        {
            Cancellation.ThrowIfCancellationRequested();
            if (Watch.Elapsed.TotalSeconds > 60 || Methods > 300000 || Instructions > 8000000 ||
                Resources > 20000 || ResourceBytes > 256L * 1024 * 1024)
                throw new InvalidDataException("SCAN_LIMIT");
        }
    }

    // Cecil 读取字段布局等元数据时也可能间接 Resolve；禁止默认搜索 MOD 外的文件。
    private sealed class NoExternalAssemblyResolver : IAssemblyResolver
    {
        public AssemblyDefinition Resolve(AssemblyNameReference name) => throw new AssemblyResolutionException(name);
        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) => throw new AssemblyResolutionException(name);
        public void Dispose() { }
    }

    internal static PluginScanResult Scan(DirectoryInfo modDirectory, CancellationToken cancellation = default, IEnumerable<DirectoryInfo>? modPaths = null)
    {
        var result = new PluginScanResult();
        var modRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.TrimEndingDirectorySeparator(modDirectory.FullName) };
        if (modPaths != null)
            foreach (var root in modPaths) modRoots.Add(Path.TrimEndingDirectorySeparator(root.FullName));
        var elapsed = Stopwatch.StartNew();
        var budget = new ScanBudget { Cancellation = cancellation };
        string plugin = Path.Combine(modDirectory.FullName, "plugin");
        try
        {
            foreach (string path in EnumerateDlls(plugin))
            {
                cancellation.ThrowIfCancellationRequested();
                string relative = Path.GetRelativePath(plugin, path);
                var file = new PluginFile { Path = path, RelativePath = relative };
                result.Files.Add(file);
                if (result.Files.Count > MaxFiles) throw new InvalidDataException("SCAN_LIMIT");
                int blockCount = result.BlockReasons.Count;
                try
                {
                    // 持有只读共享句柄期间验证签名、哈希和解析，防止正常写入并发修改。
                    using var stream = File.OpenRead(path);
                    try { file.Trusted = Win32.AuthenticodeHelper.Verify(path); }
                    catch { file.Trusted = false; }
                    if (!file.Trusted)
                    {
                        budget.Bytes += stream.Length;
                        if (stream.Length > MaxFileBytes || budget.Bytes > MaxTotalBytes) throw new InvalidDataException("SCAN_LIMIT");
                    }
                    file.Hash = Convert.ToHexString(SHA256.HashData(stream));
                    if (file.Trusted)
                    {
                        // 证书显示失败不能撤销 WinVerifyTrust 的放行结论。
                        try
                        {
#pragma warning disable SYSLIB0057
                            using var certificate = new X509Certificate2(path);
#pragma warning restore SYSLIB0057
                            file.Official = CoreMOD.IsLBGameCertificate(certificate);
                            file.Publisher = certificate.GetNameInfo(X509NameType.SimpleName, false);
                        }
                        catch { file.Publisher = "有效签名".Translate(); }
                        continue;
                    }
                    // 签名验证沿用 Windows 策略；协作时间预算只计算本地解析。
                    budget.Watch.Start();
                    budget.Check();
                    if (IsUnixNativeLibrary(stream))
                    {
                        result.Add("包含原生代码", relative, compatibility: true);
                        continue;
                    }
                    stream.Position = 0;
                    using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
                    if (CheckNativePe(pe, stream.Length, relative, "", result)) continue;
                    if (!pe.HasMetadata || pe.PEHeaders.CorHeader == null) throw new BadImageFormatException();
                    var metadata = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(pe);
                    foreach (var handle in metadata.AssemblyFiles)
                        if (metadata.GetAssemblyFile(handle).ContainsMetadata)
                            throw new InvalidDataException("EXTERNAL_MODULE");
                    if ((pe.PEHeaders.CorHeader.Flags & CorFlags.ILOnly) == 0)
                        result.Add("包含原生代码", relative, compatibility: true);
                    stream.Position = 0;
                    using var module = ModuleDefinition.ReadModule(stream, new ReaderParameters { AssemblyResolver = new NoExternalAssemblyResolver() });
                    // GetTypes 包含嵌套、异步和初始化类型；不调用 Resolve/Assembly.Load。
                    foreach (var type in module.GetTypes())
                    {
                        budget.Check();
                        foreach (var field in type.Fields)
                        {
                            byte[] data = field.InitialValue;
                            if (field.FieldType.IsPointer) result.Add("访问原生代码或内存", relative, type.FullName, field.FullName, true);
                            if (data.Length >= 512 && !IsMedia(data) && (IsExecutable(data) || IsCompressed(data) || Entropy(data) > 7.5))
                                CheckResource(data, field.FullName, relative, result, budget, 0);
                        }
                        foreach (var method in type.Methods)
                        {
                            budget.Methods++;
                            budget.Check();
                            if (method.IsPInvokeImpl)
                                result.Add("调用原生函数", relative, method.FullName,
                                    method.PInvokeInfo.Module.Name + "!" + method.PInvokeInfo.EntryPoint, true);
                            if (!method.HasBody) continue;
                            var body = method.Body ?? throw new BadImageFormatException();
                            string methodName = method.FullName;
                            if (body.Instructions.Count == 0 || body.Instructions[^1].Offset + body.Instructions[^1].GetSize() != body.CodeSize)
                                throw new BadImageFormatException();
                            if (method.ReturnType.IsPointer || method.Parameters.Any(x => x.ParameterType.IsPointer) || body.Variables.Any(x => x.VariableType.IsPointer))
                                result.Add("访问原生代码或内存", relative, methodName, compatibility: true);
                            budget.Instructions += body.Instructions.Count;
                            budget.Check();
                            var arguments = new MethodArguments(body);
                            foreach (var instruction in body.Instructions)
                            {
                                if (instruction.Operand is Instruction[] targets && targets.Any(x => x == null)) throw new BadImageFormatException();
                                if (instruction.OpCode.Code == Code.Calli)
                                    result.Add("间接调用原生或动态代码", relative, methodName, "calli", true);
                                if (instruction.Operand is MethodReference target)
                                    CheckCall(target, instruction, relative, methodName, result, modRoots, ref arguments);
                                else if (instruction.Operand is FieldReference field)
                                    CheckMember(field, relative, methodName, result);
                                else if (instruction.Operand is string value)
                                    CheckString(value, relative, methodName, result, budget);
                                else if (instruction.Operand == null && instruction.OpCode.OperandType != OperandType.InlineNone)
                                    throw new BadImageFormatException();
                            }
                            // Cecil 会把读过的方法体缓存在定义上。此方法已检查完，可以释放整组 IL 指令。
                            method.Body = null;
                        }
                    }
                    foreach (var resource in module.Resources)
                    {
                        if (resource is not EmbeddedResource embedded)
                        {
                            result.BlockUnanalyzedContent($"EXTERNAL_RESOURCE: {SafeText(relative)} — {SafeText(resource.Name)}: " + "DLL 引用了外置资源, 未读取其内容".Translate());
                            continue;
                        }
                        using var data = embedded.GetResourceStream();
                        if (data.Length > MaxResourceBytes) throw new InvalidDataException("SCAN_LIMIT");
                        byte[] bytes = new byte[(int)data.Length];
                        data.ReadExactly(bytes);
                        CheckResource(bytes, resource.Name, relative, result, budget, 0);
                    }
                    if (module.HasExportedTypes && module.ExportedTypes.Any(x => x.Scope is ModuleReference))
                        result.BlockUnanalyzedContent($"EXTERNAL_MODULE: {SafeText(relative)}: " + "包含额外托管模块, 无法在 DLL 范围内完成检查".Translate());
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    result.BlockUnanalyzedContent($"{(ex.Message == "SCAN_LIMIT" ? "SCAN_LIMIT" : "READ_FAILED")}: {SafeText(relative)}: " +
                        (ex.Message == "SCAN_LIMIT" ? "文件或代码超出检测上限" : "无法完整读取 DLL, 方法体或资源").Translate());
                }
                finally
                {
                    file.Blocked = result.BlockReasons.Count > blockCount;
                    budget.Watch.Stop();
                }
            }
        }
        catch (Exception ex)
        {
            result.BlockReasons.Add((ex is OperationCanceledException ? "CANCELLED: " : "SCAN_INCOMPLETE: ") +
                "检测未完成, 未启用代码插件".Translate());
        }
        result.ElapsedMilliseconds = elapsed.ElapsedMilliseconds;
        return result;
    }

    internal static bool HasUnsignedDll(DirectoryInfo modDirectory)
    {
        try { return EnumerateDlls(Path.Combine(modDirectory.FullName, "plugin")).Any(path => !Win32.AuthenticodeHelper.Verify(path)); }
        catch { return true; }
    }

    private static IEnumerable<string> EnumerateDlls(string plugin)
    {
        if (!Directory.Exists(plugin)) yield break;
        var pending = new Stack<string>();
        pending.Push(plugin);
        int directories = 0;
        while (pending.Count != 0)
        {
            string directory = pending.Pop();
            if (++directories > 4096 || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("SCAN_SCOPE");
            foreach (string path in Directory.EnumerateFiles(directory, "*.dll"))
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("SCAN_SCOPE");
                yield return path;
            }
            foreach (string child in Directory.EnumerateDirectories(directory)) pending.Push(child);
        }
    }

    internal static bool CheckBeforeLoad(DirectoryInfo directory, PluginScanResult result)
    {
        if (result.Blocked) return false;
        PluginFile? failedFile = null;
        bool Changed(string reason)
        {
            if (failedFile != null) failedFile.Blocked = true;
            result.BlockReasons.Add("LOAD_CHANGED: " + (failedFile == null ? "" : SafeText(failedFile.RelativePath) + " — ") + reason.Translate());
            return false;
        }
        try
        {
            var paths = EnumerateDlls(Path.Combine(directory.FullName, "plugin")).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!paths.SetEquals(result.Files.Select(x => x.Path))) return Changed("检测后, 插件目录中的 DLL 清单发生了变化。");
            foreach (var file in result.Files)
            {
                failedFile = file;
                using var stream = File.OpenRead(file.Path);
                if (Convert.ToHexString(SHA256.HashData(stream)) != file.Hash) return Changed("检测后, 这个 DLL 的内容发生了变化。");
                // 只有顶层插件入口会复用已加载的类型；其他 DLL 仍检查文件内容。
                if (!CoreMOD.LoadPlug.TryGetValue(file.RelativePath, out var loaded)) continue;
                if (!string.Equals(loaded.Type.Assembly.Location, file.Path, StringComparison.OrdinalIgnoreCase))
                {
                    result.BlockEntryConflict(file, loaded.Type.Assembly.Location);
                    return false;
                }
                if (loaded.Hash != file.Hash) return Changed("桌宠仍在使用更新前的入口 DLL, 请重启桌宠以加载新版本。");
            }
            return true;
        }
        catch { return Changed("无法重新核对插件文件, 请检查文件后重试。"); }
    }

    private static void CheckCall(MethodReference target, Instruction instruction, string file, string method, PluginScanResult result, HashSet<string> modRoots,
        ref MethodArguments arguments)
    {
        string type = target.DeclaringType.FullName, name = target.Name;
        CheckMember(target, file, method, result);
        string? category = null;
        if (type is "System.IO.File" or "System.IO.Directory" or "System.IO.FileInfo" or "System.IO.DirectoryInfo" or "System.IO.FileStream" ||
            (type is "System.IO.StreamReader" or "System.IO.StreamWriter" && name == ".ctor" &&
                target.Parameters.Count > 0 && target.Parameters[0].ParameterType.FullName == "System.String"))
        {
            bool instanceInfo = type is "System.IO.FileInfo" or "System.IO.DirectoryInfo";
            if ((instanceInfo && name == ".ctor") || name is "Close" or "Dispose" or "ToString" or "get_FullName" or
                "get_Name" or "get_DirectoryName" or "get_Directory" or "get_Extension") return;
            bool writes = name.StartsWith("Write") || name.StartsWith("Set") || name.StartsWith("set_") || name.StartsWith("Append") ||
                name.StartsWith("Create") || name.StartsWith("Move") || name.StartsWith("Delete") ||
                name is "OpenWrite" or "Replace" or "Flush" || type == "System.IO.StreamWriter";
            if (name.StartsWith("Delete")) result.Add("删除文件或目录", file, method, member: target);
            // 复制分别检查源与目标；移动/替换会修改两端。正文、编码、通配符都不是路径。
            int first = instanceInfo ? -1 : 0;
            int last = name is "Copy" or "Move" or "MoveTo" || (instanceInfo && name == "CopyTo") ? first + 1 : name == "Replace" ? first + 2 : first;
            for (int i = first; i <= last; i++)
            {
                if (i != first && (i >= target.Parameters.Count || target.Parameters[i].ParameterType.FullName != "System.String")) continue;
                bool write = writes || (name is "Copy" or "CopyTo" && i > first);
                object? path = null;
                if (i < 0 || i < target.Parameters.Count && target.Parameters[i].ParameterType.FullName == "System.String")
                    path = arguments.Read(instruction, i);
                // GetFiles 等的搜索模式也可以带目录，不能仅凭首参把越界枚举标为低风险。
                int patternIndex = instanceInfo ? 0 : 1;
                if (path != null && type is "System.IO.Directory" or "System.IO.DirectoryInfo" &&
                    name is "GetFiles" or "GetDirectories" or "GetFileSystemEntries" or "GetFileSystemInfos" or
                        "EnumerateFiles" or "EnumerateDirectories" or "EnumerateFileSystemEntries" or "EnumerateFileSystemInfos" &&
                    target.Parameters.Count > patternIndex && target.Parameters[patternIndex].ParameterType.FullName == "System.String")
                {
                    if (arguments.Read(instruction, patternIndex) is not string pattern ||
                        pattern.IndexOfAny(['/', '\\', ':']) >= 0 || pattern == "..") path = null;
                }
                // File.Open / new FileStream 的 FileAccess 或 FileMode 决定是否读写。
                bool unspecified = name == "Open" || (type == "System.IO.FileStream" && name == ".ctor");
                if (unspecified)
                {
                    int access = -1, mode = -1;
                    for (int n = 0; n < target.Parameters.Count; n++)
                    {
                        if (target.Parameters[n].ParameterType.FullName == "System.IO.FileAccess") access = n;
                        if (target.Parameters[n].ParameterType.FullName == "System.IO.FileMode") mode = n;
                    }
                    int? flags = access >= 0 || mode >= 0 ? arguments.Read(instruction, access >= 0 ? access : mode) as int? : null;
                    if (access >= 0 && flags is >= 1 and <= 3)
                    {
                        write = flags != (int)FileAccess.Read;
                        if (flags == (int)FileAccess.ReadWrite) result.Add(FileCategory(modRoots, path, false), file, method, member: target);
                        unspecified = false;
                    }
                    else if (access < 0 && flags is >= 1 and <= 6)
                    {
                        write = true;
                        if (flags != (int)FileMode.Append) result.Add(FileCategory(modRoots, path, false), file, method, member: target);
                        unspecified = false;
                    }
                }
                result.Add(FileCategory(modRoots, path, unspecified ? null : write), file, method, member: target);
            }
        }
        else if (type == "System.IO.Path" && name == "GetTempFileName") category = "写入系统临时目录";
        else if (type == "VPet_Simulator.Windows.Interface.ExtensionValue" && target.DeclaringType.Scope.Name == "VPet-Simulator.Windows.Interface" && name == "GetMODStorage")
        {
            string? modName = arguments.Read(instruction, 0) as string;
            category = FileCategory(modRoots, ModStoragePath(modName), true);
        }
        else if (type == "System.IO.FileSystemWatcher") category = "监视文件变化";
        else if (type.StartsWith("System.IO.MemoryMappedFiles.", StringComparison.Ordinal)) category = "访问内存映射文件";
        else if (type == "System.IO.Compression.ZipFile" && target.Parameters.Count > 0)
        {
            if (target.Parameters[0].ParameterType.FullName == "System.String")
                result.Add(FileCategory(modRoots, arguments.Read(instruction, 0), name == "Open" ? null : false), file, method, member: target);
            if (name is "ExtractToDirectory" or "CreateFromDirectory" && target.Parameters.Count > 1 && target.Parameters[1].ParameterType.FullName == "System.String")
                result.Add(FileCategory(modRoots, arguments.Read(instruction, 1), true), file, method, member: target);
        }
        else if (type == "VPet_Simulator.Windows.Interface.Photo" && name == "SaveAs" && target.Parameters.Count == 2)
            category = FileCategory(modRoots, arguments.Read(instruction, 1), true);
        else if (type is "Microsoft.Win32.Registry" or "Microsoft.Win32.RegistryKey" or "Microsoft.Win32.RegistryAclExtensions")
        {
            if (name.StartsWith("Delete")) category = "删除注册表项";
            else if (name.StartsWith("Set") || name.StartsWith("Create")) category = "写入注册表";
            else if (name == "OpenSubKey")
            {
                bool readOnly = target.Parameters.Count == 1;
                if (!readOnly && instruction.OpCode.Code is Code.Call or Code.Callvirt)
                {
                    readOnly = true;
                    for (int i = target.Parameters.Count - 1; i > 0; i--)
                    {
                        int? value = arguments.Read(instruction, i) as int?;
                        readOnly = value.HasValue && (target.Parameters[i].ParameterType.FullName switch
                        {
                            "System.Boolean" => value == 0,
                            "Microsoft.Win32.RegistryKeyPermissionCheck" => value == (int)Microsoft.Win32.RegistryKeyPermissionCheck.ReadSubTree,
                            "System.Security.AccessControl.RegistryRights" => value > 0 && (value & ~(int)System.Security.AccessControl.RegistryRights.ReadKey) == 0,
                            _ => false
                        });
                        if (!readOnly) break;
                    }
                }
                category = readOnly ? "读取注册表" : "请求注册表写权限或权限不明";
            }
            else if (name.StartsWith("Get") || name is "get_Name" or "get_SubKeyCount" or "get_ValueCount" or "get_View") category = "读取注册表";
            else if (name is not ("Close" or "Dispose")) category = "访问注册表";
        }
        else if (type.StartsWith("System.Net.Http.", StringComparison.Ordinal) || type.StartsWith("System.Net.Sockets.", StringComparison.Ordinal) ||
            type is "System.Net.WebClient" or "System.Net.WebRequest" or "System.Net.HttpWebRequest" or "System.Net.HttpListener") category = "访问网络";
        else if (type == "System.Diagnostics.Process" && name == "Start") category = "启动外部程序";
        else if (type == "System.Windows.Forms.SendKeys") category = "模拟键盘输入";
        else if (type is "System.Windows.Clipboard" or "System.Windows.Forms.Clipboard")
            category = name.StartsWith("Contains") || name == "IsCurrent" ? "检查剪贴板格式" :
                name.StartsWith("Get") ? "读取剪贴板" : name.StartsWith("Set") || name is "Clear" or "Flush" ? "修改剪贴板" : null;
        else if ((type == "System.Reflection.Assembly" && name.StartsWith("Load")) || type.StartsWith("System.Runtime.Loader.AssemblyLoadContext") ||
            type.StartsWith("System.Reflection.Emit.") || type.StartsWith("System.CodeDom.Compiler.") || type.StartsWith("Microsoft.CodeAnalysis.") ||
            (type.StartsWith("System.Linq.Expressions.Expression") && name == "Compile") ||
            (type == "System.AppDomain" && (name.StartsWith("Load") || name.StartsWith("ExecuteAssembly") || name.StartsWith("CreateInstance")))) category = "加载或生成动态代码";
        else if (type.StartsWith("HarmonyLib.", StringComparison.Ordinal)) category = "使用 Harmony, 可修改程序运行时的方法行为";
        else if ((type == "System.Type" && name is "GetType" or "GetMethod" or "GetMethods" or "GetField" or "GetFields" or "GetProperty" or "GetProperties" or "GetMember" or "GetMembers" or "GetConstructor" or "InvokeMember") ||
            type is "System.Reflection.MethodInfo" or "System.Reflection.MethodBase" or "System.Reflection.FieldInfo" or "System.Reflection.PropertyInfo" or "System.Reflection.ConstructorInfo" ||
            type == "System.Activator" || type.StartsWith("Microsoft.CSharp.RuntimeBinder.")) category = "反射或修改运行时行为";
        else if (type is "System.Runtime.InteropServices.NativeLibrary" or "System.Runtime.InteropServices.Marshal" or "System.Runtime.CompilerServices.Unsafe") category = "访问原生代码或内存";
        else if (type == "System.Windows.Markup.XamlReader" || (type == "System.Windows.Application" && name == "LoadComponent") ||
            (type == "System.Windows.ResourceDictionary" && name == "set_Source")) category = "加载 XAML 或界面资源";
        else if (type is "System.Runtime.Serialization.Formatters.Binary.BinaryFormatter" or "System.Runtime.Serialization.NetDataContractSerializer" or "System.Web.UI.LosFormatter" or "System.Web.UI.ObjectStateFormatter") category = "反序列化对象";
        if (category != null) result.Add(category, file, method, member: target);
        if ((type == "VPet_Simulator.Windows.Interface.ISetting" || type == "VPet_Simulator.Windows.Setting" || type.StartsWith("LinePutScript.")) && name is "get_Item" or "set_Item" or "FindLine" or "FindorAddLine")
            for (var previous = instruction.Previous; previous != null && instruction.Offset - previous.Offset < 40; previous = previous.Previous)
                if (previous.Operand is string key && key is "diy" or "passmod" or "onmod" or "dllskip")
                    result.Add("访问插件授权或快捷操作设置", file, method, key);
    }

    private enum PathOrigin { ModList, ModEnumerator, ModDirectory, OwnAssembly }

    private struct MethodArguments(MethodBody body)
    {
        private HashSet<Instruction>? targets;

        // 分支可能从别处把不同的值送到同一个调用点。只在一段连续指令内认定参数值。
        private bool StraightLineArgument(Instruction first, Instruction call)
        {
            if (targets == null)
            {
                targets = new HashSet<Instruction>();
                foreach (var ins in body.Instructions)
                    if (ins.Operand is Instruction branch) targets.Add(branch);
                    else if (ins.Operand is Instruction[] branches) targets.UnionWith(branches);
                foreach (var handler in body.ExceptionHandlers)
                {
                    targets.Add(handler.HandlerStart);
                    if (handler.FilterStart != null) targets.Add(handler.FilterStart);
                }
            }
            for (var ins = call; ins != first; ins = ins.Previous)
                if (targets.Contains(ins)) return false;
            return true;
        }

        public object? Read(Instruction call, int index)
        {
            if (call.OpCode.Code is not (Code.Call or Code.Callvirt or Code.Newobj)) return null;
            var cursor = call.Previous;
            int budget = 32;
            object? value = null;
            Instruction? first = null;
            try
            {
                for (int i = ((MethodReference)call.Operand).Parameters.Count - 1; i >= index; i--)
                    value = ReadPathOperand(ref cursor, ref budget, ref first);
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException) { return null; }
            return value != null && first != null && StraightLineArgument(first, call) ? value : null;
        }

        private static int StoredLocal(Instruction ins) => ins.OpCode.Code switch
        {
            Code.Stloc_0 or Code.Stloc_1 or Code.Stloc_2 or Code.Stloc_3 => (int)ins.OpCode.Code - (int)Code.Stloc_0,
            Code.Stloc or Code.Stloc_S => ((VariableDefinition)ins.Operand).Index,
            _ => -1
        };

        // 只计算简单的路径表达式。未知调用仅消费其栈参数，不执行插件方法。
        private object? ReadPathOperand(ref Instruction? cursor, ref int budget, ref Instruction? first)
        {
            if (cursor == null || --budget < 0) { cursor = null; return null; }
            var ins = cursor;
            first = ins;
            cursor = ins.Previous;
            // Debug 构建常把一个表达式先存入局部变量再取出。只回溯紧邻的同一槽位。
            int load = ins.OpCode.Code switch
            {
                Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3 => (int)ins.OpCode.Code - (int)Code.Ldloc_0,
                Code.Ldloc or Code.Ldloc_S or Code.Ldloca or Code.Ldloca_S => ((VariableDefinition)ins.Operand).Index, _ => -1
            };
            if (load >= 0 && load < body.Variables.Count)
            {
                bool address = ins.OpCode.Code is Code.Ldloca or Code.Ldloca_S;
                if (!address && cursor != null && StoredLocal(cursor) == load)
                {
                    cursor = cursor.Previous;
                    return ReadPathOperand(ref cursor, ref budget, ref first);
                }
                // foreach 把目录和枚举器存在局部变量中。仅跟踪短方法里唯一的赋值；
                // 验证赋值表达式和读取表达式各自的分支入口，不展开循环。
                string localType = body.Variables[load].VariableType.FullName;
                if (body.Instructions.Count <= 256 && localType is "System.IO.DirectoryInfo" or "System.Collections.Generic.List`1/Enumerator<System.IO.DirectoryInfo>")
                {
                    Instruction? store = null;
                    foreach (var other in body.Instructions)
                    {
                        if (StoredLocal(other) == load)
                        {
                            if (store != null || other.Offset >= ins.Offset) return null;
                            store = other;
                        }
                        if (other.OpCode.Code is Code.Ldloca or Code.Ldloca_S && ((VariableDefinition)other.Operand).Index == load)
                        {
                            var use = other.Next;
                            if (use?.OpCode.Code == Code.Constrained) use = use.Next;
                            if (use?.OpCode.Code is not (Code.Call or Code.Callvirt) || use.Operand is not MethodReference access ||
                                !((access.DeclaringType.FullName == localType && access.Name is "get_Current" or "MoveNext") ||
                                  (access.DeclaringType.FullName == "System.IDisposable" && access.Name == "Dispose"))) return null;
                        }
                    }
                    if (store != null)
                    {
                        Instruction? source = store.Previous, start = null;
                        var origin = ReadPathOperand(ref source, ref budget, ref start);
                        if (origin is PathOrigin.ModEnumerator or PathOrigin.ModDirectory && start != null &&
                            StraightLineArgument(start, store)) return origin;
                    }
                }
                return null;
            }
            if (ins.OpCode.Code == Code.Nop) return ReadPathOperand(ref cursor, ref budget, ref first);
            if (ins.OpCode.Code == Code.Ldstr && ins.Operand is string literal) return literal.Length <= 4096 ? literal : null;
            if (ins.OpCode.Code >= Code.Ldc_I4_0 && ins.OpCode.Code <= Code.Ldc_I4_8) return (int)ins.OpCode.Code - (int)Code.Ldc_I4_0;
            if (ins.OpCode.Code is Code.Ldc_I4 or Code.Ldc_I4_S) return Convert.ToInt32(ins.Operand);
            if (ins.OpCode.Code == Code.Ldsfld && ins.Operand is FieldReference field &&
                field.DeclaringType.FullName == "VPet_Simulator.Windows.Interface.ExtensionValue" && field.Name == "BaseDirectory")
                return AppContext.BaseDirectory;
            if (ins.OpCode.Code == Code.Ldfld)
            {
                // 字段值未知，但可以跳过所属对象，继续定位 WriteAllText 等调用的路径参数。
                ReadPathOperand(ref cursor, ref budget, ref first);
                return null;
            }
            if (ins.OpCode.Code is Code.Call or Code.Callvirt or Code.Newobj && ins.Operand is MethodReference call && call.Parameters.Count <= 8)
            {
                var args = new object?[call.Parameters.Count];
                for (int i = args.Length - 1; i >= 0; i--) args[i] = ReadPathOperand(ref cursor, ref budget, ref first);
                var receiver = call.HasThis && ins.OpCode.Code != Code.Newobj ? ReadPathOperand(ref cursor, ref budget, ref first) : null;
                string type = call.DeclaringType.FullName;
                string scope = call.DeclaringType.Scope.Name;
                if (call.Name == "get_MODPath" && (type == "VPet_Simulator.Windows.Interface.IMainWindow" && scope == "VPet-Simulator.Windows.Interface" ||
                    type == "VPet_Simulator.Windows.MainWindow" && scope == "VPet-Simulator.Windows")) return PathOrigin.ModList;
                if (receiver is PathOrigin.ModList && type == "System.Collections.Generic.List`1<System.IO.DirectoryInfo>" && call.Name == "GetEnumerator")
                    return PathOrigin.ModEnumerator;
                if (receiver is PathOrigin.ModEnumerator && type == "System.Collections.Generic.List`1/Enumerator<System.IO.DirectoryInfo>" && call.Name == "get_Current")
                    return PathOrigin.ModDirectory;
                if (receiver is PathOrigin.ModList && type == "System.Collections.Generic.List`1<System.IO.DirectoryInfo>" && call.Name is "get_Item" or "Find" or "FindLast")
                    return PathOrigin.ModDirectory;
                if (type == "System.Linq.Enumerable" && args.Length > 0 && args[0] is PathOrigin.ModList)
                {
                    if (call.Name == "Where") return PathOrigin.ModList;
                    if (call.Name is "First" or "Last" or "Single" or "ElementAt") return PathOrigin.ModDirectory;
                    if (call.Name is "FirstOrDefault" or "LastOrDefault" or "SingleOrDefault" &&
                        (args.Length == 1 || (args.Length == 2 && call.Parameters[1].ParameterType.FullName.StartsWith("System.Func`2<", StringComparison.Ordinal))))
                        return PathOrigin.ModDirectory;
                }
                if (call.Name == "GetMODStorage" && type == "VPet_Simulator.Windows.Interface.ExtensionValue" && scope == "VPet-Simulator.Windows.Interface" &&
                    args is [string modName]) return ModStoragePath(modName);
                if (type == "System.Reflection.Assembly" && call.Name == "GetExecutingAssembly") return PathOrigin.OwnAssembly;
                if (type == "System.Reflection.Assembly" && call.Name == "get_Location" && receiver is PathOrigin.OwnAssembly)
                    return call.Module.FileName;
                if (args is [string input] && Path.IsPathFullyQualified(input) &&
                    (type == "System.IO.Path" && call.Name == "GetDirectoryName" || type == "System.IO.Directory" && call.Name == "GetParent"))
                    return Path.GetDirectoryName(input);
                if (type == "System.IO.DirectoryInfo" && call.Name == "get_Parent" && receiver is string dir && Path.IsPathFullyQualified(dir))
                    return Path.GetDirectoryName(dir);
                if (type is "System.IO.FileSystemInfo" or "System.IO.DirectoryInfo" && call.Name == "get_FullName") return receiver;
                if (args.Length is >= 2 and <= 4 && args[0] is PathOrigin.ModDirectory && call.Parameters.All(p => p.ParameterType.FullName == "System.String") &&
                    (type == "System.IO.Path" && call.Name is "Combine" or "Join" || type == "System.String" && call.Name == "Concat"))
                {
                    // 不知道具体是哪个 MOD。只接受固定子路径，不能把绝对路径、兄弟目录或 .. 当作 MOD 内部。
                    for (int i = 1; i < args.Length; i++)
                    {
                        if (args[i] is not string part) return null;
                        if (type == "System.String" && i == 1)
                        {
                            if (part.Length == 0 || part[0] is not ('/' or '\\')) return null;
                            part = part[1..];
                        }
                        if (Path.IsPathRooted(part) || part.Split('/', '\\').Any(x => x.EndsWith('.') || x.EndsWith(' ') ||
                            x.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)) return null;
                    }
                    return PathOrigin.ModDirectory;
                }
                if (type is "System.AppContext" or "System.AppDomain" && call.Name == "get_BaseDirectory") return AppContext.BaseDirectory;
                if (type == "System.IO.Path" && call.Name == "GetTempPath") return Path.GetTempPath();
                if (type == "System.IO.Path" && call.Name == "GetTempFileName") return Path.Combine(Path.GetTempPath(), "vpet-scan.tmp");
                if (type == "System.IO.Path" && call.Name == "GetRandomFileName") return "vpet-scan.tmp";
                if (type == "System.Environment" && call.Name == "GetFolderPath" && args is [int id] && Enum.IsDefined((Environment.SpecialFolder)id))
                {
                    string folder = Environment.GetFolderPath((Environment.SpecialFolder)id, Environment.SpecialFolderOption.DoNotVerify);
                    return folder.Length == 0 ? null : folder;
                }
                if (type is "System.IO.FileInfo" or "System.IO.DirectoryInfo" && call.Name == ".ctor" && args.Length == 1) return args[0] as string;
                if (args.Length is >= 2 and <= 4 && args.All(x => x is string) && call.Parameters.All(p => p.ParameterType.FullName == "System.String"))
                {
                    var parts = Array.ConvertAll(args, x => (string)x!);
                    if (parts.Sum(x => x.Length) > 4096) return null;
                    if (type == "System.IO.Path" && call.Name == "Combine") return Path.Combine(parts);
                    if (type == "System.IO.Path" && call.Name == "Join") return Path.Join(parts);
                    if (type == "System.String" && call.Name == "Concat") return string.Concat(parts);
                }
                return null;
            }
            // 未知单值（局部变量、参数等）仍可跳过，以定位更早的实参。
            if (ins.OpCode.StackBehaviourPop != StackBehaviour.Pop0 ||
                ins.OpCode.StackBehaviourPush is not (StackBehaviour.Push1 or StackBehaviour.Pushi or StackBehaviour.Pushref)) cursor = null;
            return null;
        }
    }

    // 模拟宿主的路径拼接；直接调用 GetMODStorage 会创建真实目录。
    private static string? ModStoragePath(string? modName) => modName == null ? null : Path.Combine(AppContext.BaseDirectory, "ModData", modName);

    private static string FileCategory(HashSet<string> modRoots, object? value, bool? write)
    {
        if (value is PathOrigin.ModDirectory && write == false) return "读取 MOD 目录";
        if (value is string path && Path.IsPathFullyQualified(path) &&
            !path.StartsWith(@"\\?\", StringComparison.Ordinal) && !path.StartsWith(@"\\.\", StringComparison.Ordinal))
        {
            try
            {
                string full = Path.GetFullPath(path);
                static bool Under(string full, string root)
                {
                    string relative = Path.GetRelativePath(root, full);
                    return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !Path.IsPathRooted(relative);
                }
                if (Under(full, Path.Combine(AppContext.BaseDirectory, "ModData")) && write.HasValue)
                    return write.Value ? "写入 MOD 数据目录" : "读取 MOD 目录";
                if (write == false)
                    for (string? parent = Path.TrimEndingDirectorySeparator(full); parent != null; parent = Path.GetDirectoryName(parent))
                        if (modRoots.Contains(parent)) return "读取 MOD 目录";
                if (Under(full, AppContext.BaseDirectory)) return write switch { true => "写入桌宠目录", false => "读取桌宠目录", null => "打开桌宠目录内的文件（读写方式不明）" };
                if (write == true && Under(full, Path.GetTempPath())) return "写入系统临时目录";
                return write switch { true => "写入桌宠目录外", false => "读取桌宠目录外", null => "打开桌宠目录外的文件（读写方式不明）" };
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException) { }
        }
        return write switch { true => "写入文件（目录不明）", false => "读取文件（目录不明）", null => "打开文件（目录和读写方式不明）" };
    }

    private static void CheckMember(MemberReference member, string file, string method, PluginScanResult result)
    {
        string type = member.DeclaringType.FullName, name = member.Name;
        string? category = null;
        if (type is "VPet_Simulator.Windows.MainWindow" or "VPet_Simulator.Windows.Interface.IMainWindow")
            category = name switch
            {
                "RunDIY" or "LoadDIY" => "执行快捷操作, 启动程序或模拟输入",
                "GenerateAuthKey" => "获取 Steam 身份认证票据",
                "GetVPetRoom" => "访问联机服务及 Steam 身份认证",
                "DiagnosisUPLoad" => "上传游戏存档和设置",
                "Save" => "保存游戏数据, 可能同步云存档",
                "Restart" => "重启桌宠",
                _ => null
            };
        else if (type == "VPet_Simulator.Windows.Interface.ExtensionFunction" && name == "StartURL") category = "打开外部链接, 文件或程序";
        else if (type is "VPet_Simulator.Windows.Interface.IMPWindows" or "VPet_Simulator.Windows.winMutiPlayer" && name is "SendMessage" or "SendMessageALL") category = "向其他玩家发送数据";
        else if (type == "VPet_Simulator.Windows.Interface.Photo")
            category = name switch { "SaveAs" => "导出图片文件", "CopyImageToClipboard" => "修改剪贴板", "LoadImage" or "GetGifImage" => "读取图片资源", _ => null };
        else if (type is "VPet_Simulator.Windows.Interface.ISetting" or "VPet_Simulator.Windows.Setting" && name is "get_Item" or "set_Item") category = "访问桌宠设置";
        else if (type == "VPet_Simulator.Windows.CoreMOD" || type == "VPet_Simulator.Windows.ExtensionSetting" || type == "VPet_Simulator.Windows.WorkshopProbablyCheck" || type.StartsWith("VPet_Simulator.Windows.Win32/AuthenticodeHelper")) category = "访问宿主加载或授权逻辑";
        else if (type.StartsWith("Steamworks.", StringComparison.Ordinal))
        {
            if (name.Contains("AuthSession") || name.Contains("AuthTicket") || name.Contains("WebApi")) category = "获取或使用 Steam 身份认证票据";
            else if (type.Contains("SteamRemoteStorage") && (name.Contains("Write") || name.Contains("Delete"))) category = "写入或删除 Steam 云文件";
            else if ((type.Contains("Ugc.") || type.Contains("SteamUGC")) && (name.Contains("Submit") || name.Contains("Delete") || name.Contains("Publish") || name.Contains("Create") || name.StartsWith("With"))) category = "发布或修改创意工坊内容";
            else if (type.Contains("Networking") || name.Contains("SendChat") || name.Contains("SendMessage") || name.Contains("ReplyToFriend")) category = "通过 Steam 收发数据";
            else if (name.Contains("Voice")) category = "使用 Steam 语音";
        }
        if (category != null) result.Add(category, file, method, member: member);
    }

    private static void CheckString(string value, string file, string method, PluginScanResult result, ScanBudget budget, int depth = 0)
    {
        // 不在报告中输出原字符串、网址、票据或查询参数。
        string lower = value.Length < 4096 ? value.ToLowerInvariant() : "";
        if (lower.Contains("login data") || lower.Contains("local state") || lower.Contains("cookies") || lower.Contains("wallet.dat") ||
            lower.Contains("discord") || lower.Contains("loginusers.vdf") || lower.Contains("ssfn"))
            result.Add("包含浏览器, 钱包或账号数据的路径特征", file, method);
        if (value.Length < 1024 || value.Length % 4 != 0 || !value.All(c => char.IsAsciiLetterOrDigit(c) || c is '+' or '/' or '=')) return;
        if (value.Length > MaxResourceBytes) throw new InvalidDataException("SCAN_LIMIT");
        byte[] decoded;
        try { decoded = Convert.FromBase64String(value); }
        catch (FormatException) { return; }
        CheckResource(decoded, "Base64", file, result, budget, depth);
    }

    private static void CheckResource(byte[] data, string name, string file, PluginScanResult result, ScanBudget budget, int depth)
    {
        budget.Resources++;
        budget.ResourceBytes += data.Length;
        budget.Check();
        if (depth > 4 || data.Length > MaxResourceBytes) throw new InvalidDataException("SCAN_LIMIT");
        string Label() => SafeText(file) + " — " + SafeText(name);
        using var stream = new MemoryStream(data, false);
        if (name == "ILRepack.List")
        {
            // ILRepack 的 StringArrayBinaryFormatter 固定格式；不使用 BinaryFormatter，
            // 不解析任意对象、对象引用或类型。名称只用于展示合并信息，不作为信任依据。
            using var reader = new BinaryReader(stream, StrictUtf8);
            if (reader.ReadByte() != 0 || reader.ReadInt32() != 1 || reader.ReadInt32() != -1 ||
                reader.ReadInt32() != 1 || reader.ReadInt32() != 0 || reader.ReadByte() != 17 || reader.ReadInt32() != 1)
                throw new InvalidDataException();
            int count = reader.ReadInt32();
            if (count < 0 || count > 4096) throw new InvalidDataException();
            for (int i = 0; i < count; i++)
            {
                if (reader.ReadByte() != 6 || reader.ReadInt32() != i + 2) throw new InvalidDataException();
                int length = reader.Read7BitEncodedInt();
                if (length < 1 || length > 4096 || length > stream.Length - stream.Position) throw new InvalidDataException();
                string assembly = StrictUtf8.GetString(reader.ReadBytes(length));
                if (assembly.Any(char.IsControl) || string.IsNullOrWhiteSpace(new System.Reflection.AssemblyName(assembly).Name))
                    throw new InvalidDataException();
            }
            if (reader.ReadByte() != 11 || stream.Position != stream.Length) throw new InvalidDataException();
            return;
        }
        if (IsUnixNativeLibrary(stream))
        {
            result.Add("包含原生代码", file, api: SafeText(name), compatibility: true);
            return;
        }
        stream.Position = 0;
        if (data.AsSpan().StartsWith("MZ"u8))
        {
            using var pe = new PEReader(stream);
            if (CheckNativePe(pe, data.Length, file, name, result)) return;
            // 内嵌托管程序集继续按可执行载荷阻止。
        }
        if (IsExecutable(data))
        {
            result.BlockUnanalyzedContent("PAYLOAD: " + Label() + ": " + "资源中包含程序映像或脚本".Translate());
            return;
        }
        if (data.Length >= 4 && BitConverter.ToUInt32(data, 0) == 0xBEEFCACE)
        {
            using var reader = new ResourceReader(stream);
            var entries = reader.GetEnumerator();
            while (entries.MoveNext())
            {
                string key = (string)entries.Key; // 不读取 Value, 不实例化自定义对象。
                reader.GetResourceData(key, out string resourceType, out byte[] raw);
                budget.Resources++;
                budget.Check();
                if (resourceType == "ResourceTypeCode.String")
                {
                    using var valueStream = new MemoryStream(raw, false);
                    using var valueReader = new BinaryReader(valueStream, Encoding.UTF8);
                    CheckString(valueReader.ReadString(), file, "resource", result, budget, depth + 1);
                    if (valueStream.Position != valueStream.Length) throw new InvalidDataException();
                }
                else if (resourceType is "ResourceTypeCode.ByteArray" or "ResourceTypeCode.Stream")
                {
                    if (BinaryPrimitives.ReadInt32LittleEndian(raw) != raw.Length - sizeof(int)) throw new InvalidDataException();
                    CheckResource(raw[sizeof(int)..], key, file, result, budget, depth + 1);
                }
                else
                {
                    int size = resourceType switch
                    {
                        "ResourceTypeCode.Null" => 0,
                        "ResourceTypeCode.Boolean" or "ResourceTypeCode.Byte" or "ResourceTypeCode.SByte" => 1,
                        "ResourceTypeCode.Char" or "ResourceTypeCode.Int16" or "ResourceTypeCode.UInt16" => 2,
                        "ResourceTypeCode.Int32" or "ResourceTypeCode.UInt32" or "ResourceTypeCode.Single" => 4,
                        "ResourceTypeCode.Int64" or "ResourceTypeCode.UInt64" or "ResourceTypeCode.Double" or "ResourceTypeCode.DateTime" or "ResourceTypeCode.TimeSpan" => 8,
                        "ResourceTypeCode.Decimal" => 16,
                        _ => -1
                    };
                    if (size < 0) result.BlockUnanalyzedContent("RESOURCE_OBJECT: " + Label() + ": " + "包含自定义序列化资源".Translate());
                    else if (raw.Length != size) throw new InvalidDataException();
                }
            }
            return;
        }
        if (name.EndsWith(".baml", StringComparison.OrdinalIgnoreCase) && data.Length >= 36 && data[^1] == 2 &&
            BitConverter.ToInt32(data, 0) == 12 && Encoding.Unicode.GetString(data, 4, 12) == "MSBAML" &&
            BitConverter.ToUInt16(data, 18) == 0x60 && BitConverter.ToUInt16(data, 22) == 0x60 && BitConverter.ToUInt16(data, 26) == 0x60 && data[28] == 1)
        {
            result.Add("包含 WPF 界面资源", file, api: SafeText(name), compatibility: true);
            return;
        }
        if (IsMedia(data)) return;
        if (data.Length >= 4 && data[0] == 0x50 && data[1] == 0x4b && data[2] == 3 && data[3] == 4)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            foreach (var entry in zip.Entries)
            {
                if (entry.Name.Length == 0) continue;
                if (entry.Length > MaxResourceBytes || entry.Length + budget.ResourceBytes > 256L * 1024 * 1024) throw new InvalidDataException("SCAN_LIMIT");
                using var input = entry.Open();
                byte[] bytes = new byte[(int)entry.Length];
                input.ReadExactly(bytes);
                if (input.ReadByte() != -1) throw new InvalidDataException();
                CheckResource(bytes, entry.Name, file, result, budget, depth + 1);
            }
            return;
        }
        if (data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b)
        {
            using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            using var expanded = new MemoryStream();
            byte[] buffer = new byte[8192];
            int count;
            while ((count = gzip.Read(buffer)) > 0)
            {
                if (expanded.Length + count > MaxResourceBytes) throw new InvalidDataException("SCAN_LIMIT");
                expanded.Write(buffer, 0, count);
                budget.Check();
            }
            CheckResource(expanded.ToArray(), Path.GetFileNameWithoutExtension(name), file, result, budget, depth + 1);
            return;
        }
        string extension = Path.GetExtension(name).ToLowerInvariant();
        if (extension is ".txt" or ".json" or ".xml" or ".lps" or ".csv" or ".xaml")
        {
            string value = StrictUtf8.GetString(data);
            if (value.All(c => !char.IsControl(c) || c is '\r' or '\n' or '\t'))
            {
                CheckString(value, file, "resource", result, budget, depth + 1);
                if (extension == ".xaml") result.Add("包含 WPF 界面资源", file, api: SafeText(name), compatibility: true);
                return;
            }
        }
        result.BlockUnanalyzedContent("UNKNOWN_RESOURCE: " + Label() + ": " + "无法识别资源内容, 未完成检查".Translate());
    }

    private static bool IsExecutable(byte[] data)
    {
        if (data.Length < 2) return false;
        if (data.AsSpan().StartsWith("MZ"u8) || data.AsSpan().StartsWith("\u007fELF"u8)) return true;
        string prefix = Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 256)).TrimStart('\uFEFF', ' ', '\r', '\n');
        return prefix.StartsWith("#!") || prefix.StartsWith("@echo", StringComparison.OrdinalIgnoreCase) ||
            prefix.StartsWith("powershell", StringComparison.OrdinalIgnoreCase) || prefix.StartsWith("<script", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCompressed(byte[] data) => data.Length >= 4 &&
        ((data[0] == 'P' && data[1] == 'K' && data[2] == 3 && data[3] == 4) || (data[0] == 0x1f && data[1] == 0x8b));

    // 独立与内嵌 PE 共用结构检查；只为原生 DLL 提供兼容性提示，不信任文件名。
    private static bool CheckNativePe(PEReader pe, long length, string file, string resource, PluginScanResult result)
    {
        if (pe.PEHeaders.PEHeader == null || pe.PEHeaders.SectionHeaders.Length == 0) throw new BadImageFormatException();
        foreach (var section in pe.PEHeaders.SectionHeaders)
            if (section.PointerToRawData < 0 || section.SizeOfRawData < 0 ||
                (long)section.PointerToRawData + section.SizeOfRawData > length) throw new BadImageFormatException();
        var clr = pe.PEHeaders.PEHeader.CorHeaderTableDirectory;
        if (clr.Size != 0 || clr.RelativeVirtualAddress != 0 || pe.HasMetadata) return false;
        if ((pe.PEHeaders.CoffHeader.Characteristics & Characteristics.Dll) == 0) return false;
        var imports = pe.PEHeaders.PEHeader.ImportTableDirectory;
        if (imports.Size > 0)
        {
            var descriptors = pe.GetSectionData(imports.RelativeVirtualAddress).GetReader(0, imports.Size);
            bool ended = false;
            for (int i = 0; i < 4096 && descriptors.RemainingBytes >= 20; i++)
            {
                uint lookup = descriptors.ReadUInt32(), stamp = descriptors.ReadUInt32(), forward = descriptors.ReadUInt32();
                int nameRva = descriptors.ReadInt32();
                uint address = descriptors.ReadUInt32();
                if ((lookup | stamp | forward | (uint)nameRva | address) == 0) { ended = true; break; }
                var nameData = pe.GetSectionData(nameRva);
                byte[] bytes = nameData.GetContent(0, Math.Min(260, nameData.Length)).ToArray();
                int end = Array.IndexOf(bytes, (byte)0);
                if (end < 1) throw new BadImageFormatException();
                result.Add("包含原生代码", file, resource, Encoding.ASCII.GetString(bytes, 0, end), true);
            }
            if (!ended) throw new BadImageFormatException();
        }
        result.Add("包含原生代码", file, api: SafeText(resource), compatibility: true);
        return true;
    }

    // NuGet 的 runtimes 目录也会带扩展名为 .dll 的 ELF/Mach-O 依赖。
    // 只接受有完整文件表的普通共享库，不能把损坏的 CLR 映像当原生库。
    private static bool IsUnixNativeLibrary(Stream stream)
    {
        if (stream.Length < 64) return false;
        stream.Position = 0;
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        ulong ReadWord(bool is64) => is64 ? reader.ReadUInt64() : reader.ReadUInt32();
        void CheckRange(ulong offset, ulong length)
        {
            if (offset > (ulong)stream.Length || length > (ulong)stream.Length - offset) throw new BadImageFormatException();
        }
        uint magic = reader.ReadUInt32();
        if (magic == 0x464c457f)
        {
            byte bits = reader.ReadByte(), endian = reader.ReadByte(), version = reader.ReadByte();
            if (bits is not (1 or 2) || endian != 1 || version != 1) throw new BadImageFormatException();
            stream.Position = 16;
            if (reader.ReadUInt16() != 3) throw new BadImageFormatException();
            stream.Position = bits == 2 ? 40 : 32;
            ulong sections = ReadWord(bits == 2);
            stream.Position = bits == 2 ? 58 : 46;
            int size = reader.ReadUInt16(), count = reader.ReadUInt16();
            if (size < (bits == 2 ? 64 : 40) || count == 0) throw new BadImageFormatException();
            CheckRange(sections, (ulong)size * (ulong)count);
            for (int i = 0; i < count; i++)
            {
                long start = (long)sections + (long)i * size;
                stream.Position = start + 4;
                uint sectionType = reader.ReadUInt32();
                if (sectionType == 8) continue; // SHT_NOBITS 不占文件空间
                stream.Position = start + (bits == 2 ? 24 : 16);
                CheckRange(ReadWord(bits == 2), ReadWord(bits == 2));
            }
            return true;
        }
        if (magic is 0xfeedfacf or 0xfeedface)
        {
            stream.Position = 12;
            if (reader.ReadUInt32() != 6) throw new BadImageFormatException();
            uint count = reader.ReadUInt32(), bytes = reader.ReadUInt32();
            long end = (magic == 0xfeedfacf ? 32 : 28) + (long)bytes;
            if (count == 0 || count > 65536 || end > stream.Length) throw new BadImageFormatException();
            stream.Position = magic == 0xfeedfacf ? 32 : 28;
            for (int i = 0; i < count; i++)
            {
                long start = stream.Position;
                if (start + 8 > end) throw new BadImageFormatException();
                uint command = reader.ReadUInt32(), size = reader.ReadUInt32();
                if (size < 8 || start + size > end) throw new BadImageFormatException();
                if (command is 1 or 0x19)
                {
                    if (size < (command == 0x19 ? 72 : 56)) throw new BadImageFormatException();
                    stream.Position = start + (command == 0x19 ? 40 : 32);
                    CheckRange(ReadWord(command == 0x19), ReadWord(command == 0x19));
                }
                stream.Position = start + size;
            }
            if (stream.Position != end) throw new BadImageFormatException();
            return true;
        }
        return false;
    }

    private static bool IsMedia(byte[] data)
    {
        if (data.Length < 12) return false;
        if (data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            int offset = 8;
            while (offset <= data.Length - 12)
            {
                uint length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
                if (length > data.Length - offset - 12) return false;
                var kind = data.AsSpan(offset + 4, 4);
                if (offset == 8 && (!kind.SequenceEqual("IHDR"u8) || length != 13)) return false;
                offset += (int)length + 12;
                if (kind.SequenceEqual("IEND"u8)) return length == 0 && offset == data.Length;
            }
            return false;
        }
        var header = data.AsSpan(0, 4);
        if (data[0] == 0xff && data[1] == 0xd8) return data[^2] == 0xff && data[^1] == 0xd9;
        if (header.SequenceEqual("GIF8"u8)) return data[^1] == 0x3b;
        if (data[0] == 'B' && data[1] == 'M') return BitConverter.ToUInt32(data, 2) == data.Length;
        if (header.SequenceEqual("RIFF"u8)) return BitConverter.ToUInt32(data, 4) + 8 == data.Length && Encoding.ASCII.GetString(data, 8, 4) is "WEBP" or "WAVE";
        if (data[0] == 0 && data[1] == 0 && data[2] == 1 && data[3] == 0)
        {
            int count = BitConverter.ToUInt16(data, 4);
            if (count == 0 || 6 + count * 16 > data.Length) return false;
            for (int i = 0; i < count; i++)
                if ((long)BitConverter.ToUInt32(data, 6 + i * 16 + 8) + BitConverter.ToUInt32(data, 6 + i * 16 + 12) > data.Length) return false;
            return true;
        }
        if (header.SequenceEqual("OTTO"u8) || data.AsSpan(0, 4).SequenceEqual(new byte[] { 0, 1, 0, 0 }))
        {
            int count = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(4, 2));
            if (count == 0 || 12 + count * 16 > data.Length) return false;
            for (int i = 0; i < count; i++)
            {
                int offset = 12 + i * 16;
                if ((long)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 8, 4)) +
                    BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 12, 4)) > data.Length) return false;
            }
            return true;
        }
        return false;
    }

    private static double Entropy(byte[] data)
    {
        Span<int> counts = stackalloc int[256];
        counts.Clear();
        foreach (byte value in data) counts[value]++;
        double entropy = 0;
        foreach (int count in counts)
            if (count != 0) { double p = (double)count / data.Length; entropy -= p * Math.Log2(p); }
        return entropy;
    }

    private static string SafeText(string text) => new string(text.Take(300).Select(c => char.IsControl(c) ? ' ' : c).ToArray());

    internal static bool ShowResult(Window owner, string name, PluginScanResult result, bool allow)
    {
        bool canEnable = allow && !result.Blocked;
        return MessageBoxX.Show(owner, result.Report(), "代码插件检测: {0}".Translate(SafeText(name)),
            canEnable ? MessageBoxButton.YesNo : MessageBoxButton.OK,
            result.Blocked ? MessageBoxIcon.Warning : MessageBoxIcon.Info,
            canEnable ? DefaultButton.NoCancel : DefaultButton.YesOK,
            new MessageBoxXSetting
            {
                YesButtonContent = "启用代码插件".Translate(),
                NoButtonContent = "取消".Translate(),
                OKButtonContent = "关闭".Translate()
            }) == MessageBoxResult.Yes;
    }
}
