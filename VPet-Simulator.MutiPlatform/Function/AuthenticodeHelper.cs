using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.MutiPlatform;

/// <summary>跨平台 Authenticode 验证。仅 osslsigncode 验证通过后才返回签名者证书。</summary>
internal static class AuthenticodeHelper
{
    public static bool Verify(string filePath)
    {
        using var certificate = GetVerifiedCertificate(filePath);
        return certificate != null;
    }

    // 启动加载和设置页均在后台调用；不缓存文件的信任结果。
    public static X509Certificate2? GetVerifiedCertificate(string filePath) =>
        VerifyAsync(filePath).GetAwaiter().GetResult();

    // 仅供已验证签名者的认证标签使用；不同系统会把相同 OID 格式化为不同属性名。
    internal static bool HasDistinguishedName(X500DistinguishedName actual, string expected)
    {
        var actualParts = actual.EnumerateRelativeDistinguishedNames().ToArray();
        var expectedParts = new X500DistinguishedName(expected).EnumerateRelativeDistinguishedNames().ToArray();
        return actualParts.Length == expectedParts.Length && actualParts.Zip(expectedParts).All(pair =>
            !pair.First.HasMultipleElements && !pair.Second.HasMultipleElements
            && pair.First.GetSingleElementType().Value == pair.Second.GetSingleElementType().Value
            && pair.First.GetSingleElementValue() == pair.Second.GetSingleElementValue());
    }

    private static async Task<X509Certificate2?> VerifyAsync(string filePath)
    {
        DirectoryInfo? temporary = null;
        try
        {
            filePath = Path.GetFullPath(filePath);
            using var signer = ReadPrimarySigner(filePath);
            if (signer == null)
                return null;

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            string executable = FindExecutable();
            var version = await RunAsync(executable, ["--version"], timeout.Token, true).ConfigureAwait(false);
            string[] versionParts = version.Output.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (version.ExitCode != 0 || versionParts.Length < 2 || versionParts[0] != "osslsigncode"
                || !Version.TryParse(versionParts[1].TrimEnd(','), out var toolVersion) || toolVersion < new Version(2, 13))
                throw new InvalidOperationException("需要 osslsigncode 2.13 或更新版本。");

            temporary = Directory.CreateTempSubdirectory("vpet-authenticode-");
            string microsoftCA = Path.Combine(temporary.FullName, "microsoft-root-2011.pem");
            string microsoftTSA = Path.Combine(temporary.FullName, "microsoft-root-2010.pem");
            foreach (string path in new[] { microsoftCA, microsoftTSA })
            {
                using var source = typeof(AuthenticodeHelper).Assembly.GetManifestResourceStream(
                    "Authenticode." + Path.GetFileName(path))
                    ?? throw new InvalidOperationException("缺少内置微软根证书。");
                using var destination = File.Create(path);
                source.CopyTo(destination);
            }

            // 固定主签名及其叶证书，保证 [认证] 标签使用的就是验证过的签名者。
            string[] signature = ["-index", "0", "-require-leaf-hash",
                "sha256:" + signer.GetCertHashString(HashAlgorithmName.SHA256), "-in", filePath];
            // 已授权的微软离线兼容：仅固定微软根证书链可跳过吊销检查。
            var microsoft = await RunAsync(executable,
                ["verify", "-ignore-crl", "-CAfile", microsoftCA, "-TSA-CAfile", microsoftTSA, .. signature],
                timeout.Token).ConfigureAwait(false);
            if (microsoft.ExitCode == 0)
            {
                Trace.TraceInformation($"Authenticode: 微软离线兼容验证通过（未检查吊销）：{filePath}");
                return new X509Certificate2(signer);
            }

            string? caFile = Environment.GetEnvironmentVariable("VPET_AUTHENTICODE_CA_FILE");
            if (string.IsNullOrWhiteSpace(caFile))
            {
                caFile = Path.Combine(temporary.FullName, "system-roots.pem");
                // OpenSSL 不直接使用 Windows 证书存储或 macOS Keychain，显式导出系统信任锚。
                using var writer = new StreamWriter(caFile);
                foreach (var location in new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser })
                {
                    using var store = new X509Store(StoreName.Root, location);
                    store.Open(OpenFlags.ReadOnly);
                    foreach (var certificate in store.Certificates)
                    {
                        using (certificate)
                            writer.WriteLine(certificate.ExportCertificatePem());
                    }
                }
            }
            string? tsaFile = Environment.GetEnvironmentVariable("VPET_AUTHENTICODE_TSA_CA_FILE");
            if (string.IsNullOrWhiteSpace(tsaFile))
                tsaFile = caFile;
            if (!File.Exists(caFile) || !File.Exists(tsaFile))
                throw new FileNotFoundException("Authenticode CA/TSA 文件不存在。");

            var standard = await RunAsync(executable,
                ["verify", "-CAfile", Path.GetFullPath(caFile), "-TSA-CAfile", Path.GetFullPath(tsaFile), .. signature],
                timeout.Token).ConfigureAwait(false);
            if (standard.ExitCode == 0)
                return new X509Certificate2(signer);
            Trace.TraceWarning($"Authenticode 验证失败（退出码 {standard.ExitCode}）：{filePath}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException
            or BadImageFormatException or InvalidOperationException or System.ComponentModel.Win32Exception
            or OperationCanceledException or ArgumentException or NotSupportedException)
        {
            Trace.TraceWarning($"Authenticode 未获得信任：{filePath}；{ex.Message}");
        }
        finally
        {
            if (temporary != null)
            {
                try { temporary.Delete(true); }
                catch (IOException ex) { Trace.TraceWarning($"清理验签临时目录失败：{ex.Message}"); }
                catch (UnauthorizedAccessException ex) { Trace.TraceWarning($"清理验签临时目录失败：{ex.Message}"); }
            }
        }
        return null;
    }

    private static X509Certificate2? ReadPrimarySigner(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
        var directory = pe.PEHeaders.PEHeader?.CertificateTableDirectory;
        if (directory == null || directory.Value.Size == 0)
            return null;
        // CertificateTableDirectory 的地址是文件偏移，不是 RVA。
        int offset = directory.Value.RelativeVirtualAddress;
        int size = directory.Value.Size;
        if (offset < 0 || size < 8 || (long)offset + size > stream.Length)
            throw new BadImageFormatException("无效的 PE 签名目录。");
        stream.Position = offset;
        using var reader = new BinaryReader(stream);
        uint length = reader.ReadUInt32();
        if (length < 8 || length > size || length > 16 * 1024 * 1024
            || reader.ReadUInt16() != 0x200 || reader.ReadUInt16() != 2)
            throw new BadImageFormatException("无效或过大的 WIN_CERTIFICATE。");
        var cms = new SignedCms();
        cms.Decode(reader.ReadBytes((int)length - 8));
        if (cms.SignerInfos.Count != 1 || cms.SignerInfos[0].Certificate is not { } certificate)
            return null;
        // 这里只提取证书，信任必须由后续 osslsigncode 验证决定。
        return new X509Certificate2(certificate);
    }

    private static string FindExecutable()
    {
        string? configured = Environment.GetEnvironmentVariable("VPET_OSSLSIGNCODE");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (!Path.IsPathFullyQualified(configured))
                throw new ArgumentException("VPET_OSSLSIGNCODE 必须是绝对路径。");
            return configured;
        }
        string name = OperatingSystem.IsWindows() ? "osslsigncode.exe" : "osslsigncode";
        string bundled = Path.Combine(AppContext.BaseDirectory, "tools", "osslsigncode", name);
        if (File.Exists(bundled))
        {
            // Windows 上交叉发布的 ZIP 可能没有保留 Unix 可执行位。
            if (!OperatingSystem.IsWindows())
            {
                var mode = File.GetUnixFileMode(bundled);
                if ((mode & UnixFileMode.UserExecute) == 0)
                    File.SetUnixFileMode(bundled, mode | UnixFileMode.UserExecute);
            }
            return bundled;
        }
        // 不搜索 MOD 目录或当前目录；忽略 PATH 中的相对路径。
        string[] directories = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
        foreach (string directory in directories.Concat(new[] { "/opt/homebrew/bin", "/usr/local/bin", "/usr/bin" }))
        {
            if (Path.IsPathFullyQualified(directory) && File.Exists(Path.Combine(directory, name)))
                return Path.Combine(directory, name);
        }
        throw new FileNotFoundException("未找到 osslsigncode 2.13+；请安装或设置 VPET_OSSLSIGNCODE。");
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(
        string executable, string[] arguments, CancellationToken cancellation, bool captureOutput = false)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            }
        };
        foreach (string argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);
        // macOS 包携带同目录的 OpenSSL；仅影响验签子进程，不要求安装 Homebrew。
        string directory = Path.GetDirectoryName(executable)!;
        if (OperatingSystem.IsMacOS() && File.Exists(Path.Combine(directory, "libcrypto.3.dylib"))
            && File.Exists(Path.Combine(directory, "libssl.3.dylib")))
            process.StartInfo.Environment["DYLD_LIBRARY_PATH"] = directory;
        process.Start();
        Task<string>? output = captureOutput ? process.StandardOutput.ReadToEndAsync(cancellation) : null;
        Task drainOutput = output ?? (Task)process.StandardOutput.BaseStream.CopyToAsync(Stream.Null, cancellation);
        Task drainError = process.StandardError.BaseStream.CopyToAsync(Stream.Null, cancellation);
        try
        {
            await process.WaitForExitAsync(cancellation).ConfigureAwait(false);
            await Task.WhenAll(drainOutput, drainError).ConfigureAwait(false);
            return (process.ExitCode, output == null ? "" : await output.ConfigureAwait(false));
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
            try { await Task.WhenAll(drainOutput, drainError).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            throw;
        }
    }
}
