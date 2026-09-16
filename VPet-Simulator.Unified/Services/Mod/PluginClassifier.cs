using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 一个插件 dll 属于哪一类
/// </summary>
public enum PluginKind
{
    /// <summary>不是插件, 是 MOD 自带的依赖库</summary>
    Library,
    /// <summary>老的 Windows 专用插件(引用了 Windows.Interface 或 WPF)</summary>
    LegacyWindows,
    /// <summary>统一契约插件, 两个平台都能加载</summary>
    Unified,
}

/// <summary>
/// 不加载就判断一个 dll 是什么
/// </summary>
/// 存在的理由: 跨平台宿主必须能识别出 Windows 专用插件并**明确报告**跳过, 而不是
/// 加载它然后吃一个 FileNotFoundException. 更重要的是不能真的把它加载起来 ——
/// 那会执行它的模块初始化器, 在 Linux 上后果不可预期.
///
/// 所以这里只用 System.Reflection.Metadata 读 PE 头里的程序集引用表, 全程不执行
/// 任何目标代码.
public static class PluginClassifier
{
    /// <summary>
    /// 统一契约的程序集名
    /// </summary>
    public const string UnifiedInterfaceName = "VPet-Simulator.Unified";

    /// <summary>
    /// 引用了这些就说明是 Windows 专用的
    /// </summary>
    private static readonly string[] WindowsOnlyReferences =
    {
        "VPet-Simulator.Windows.Interface",
        "VPet-Simulator.Core",
        "PresentationFramework",
        "PresentationCore",
        "WindowsBase",
        "Panuon.WPF",
        "Panuon.WPF.UI",
    };

    /// <summary>
    /// 判断一个 dll 属于哪一类
    /// </summary>
    /// <param name="dllPath">dll 路径</param>
    /// <returns>读不了(不是托管程序集等)时按依赖库处理</returns>
    public static PluginKind Classify(string dllPath)
    {
        try
        {
            using var stream = File.OpenRead(dllPath);
            using var reader = new PEReader(stream);
            if (!reader.HasMetadata)
                return PluginKind.Library;
            var metadata = reader.GetMetadataReader();

            bool unified = false;
            foreach (var handle in metadata.AssemblyReferences)
            {
                var name = metadata.GetString(metadata.GetAssemblyReference(handle).Name);
                if (WindowsOnlyReferences.Contains(name, StringComparer.OrdinalIgnoreCase))
                    return PluginKind.LegacyWindows;
                if (string.Equals(name, UnifiedInterfaceName, StringComparison.OrdinalIgnoreCase))
                    unified = true;
            }
            return unified ? PluginKind.Unified : PluginKind.Library;
        }
        catch (Exception)
        {
            return PluginKind.Library;
        }
    }

    /// <summary>
    /// 按 load.lps 的规则决定要不要跳过某个 dll
    /// </summary>
    /// <param name="fileName">dll 文件名</param>
    /// <param name="rules">load.lps 的内容, 没有就传 null</param>
    /// <returns>跳过时返回 true</returns>
    /// 与 Windows 版 CoreMOD 的插件循环一致: 先看文件名里的 x86/x64 标记,
    /// 再看 load.lps 里这个文件的 skip / cpu 两项.
    public static bool ShouldSkip(string fileName, LinePutScript.ILPS? rules)
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        bool is64 = arch == Architecture.X64 || arch == Architecture.Arm64;
        // 大小写敏感是有意的: Windows 版用的就是 Name.Contains("x86"), 改成不敏感会让
        // 名字里带大写 X86 的现有 MOD 突然不加载
        if (is64 && fileName.Contains("x86", StringComparison.Ordinal))
            return true;
        if (!is64 && fileName.Contains("x64", StringComparison.Ordinal))
            return true;

        var line = rules?.FindLine(fileName);
        if (line == null)
            return false;
        if (line.GetBool("skip"))
            return true;
        var cpu = line.GetString("cpu", "anycpu")!.ToLowerInvariant();
        if (cpu == "anycpu")
            return false;
        return cpu != (is64 ? "x64" : "x86");
    }
}
