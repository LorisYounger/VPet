using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 跨平台的数据目录
/// </summary>
/// Windows 版 VPet 是"绿色软件": 存档/设置/缓存/MOD 全都写在 exe 旁边. 这在
/// Windows 上没问题, 但在 Linux 和 macOS 上必定失败 —— 程序可能被装在
/// /usr/lib 或 /opt 下(只读), macOS 的 .app 包在签名之后内部也不可写.
///
/// 所以跨平台版把"读程序自带的资源"和"写用户数据"分开:
///   InstallDirectory 只用来读
///   DataRoot / CacheRoot 才是可写的地方, 按各平台的惯例存放
///
/// 宿主程序应当在启动最早期(任何 MOD 或存档代码跑起来之前)完成初始化,
/// 需要时可以直接给这几个属性赋值来覆盖默认位置.
public static class AppPaths
{
    /// <summary>
    /// 程序安装目录, 只读
    /// </summary>
    public static string InstallDirectory { get; } = ResolveInstallDirectory();

    private static string? dataRoot;
    /// <summary>
    /// 可写的用户数据根目录 (存档/设置/MOD)
    /// </summary>
    /// 写成惰性求值而不是静态字段初始化器: 字段初始化器按声明顺序执行, 而
    /// ResolveDataRoot 依赖 IsPortable —— 一旦 IsPortable 声明在后面, 求值时它
    /// 还是默认的 false, Windows 上就会错误地走到 XDG 分支去.
    public static string DataRoot
    {
        get => dataRoot ??= ResolveDataRoot();
        set => dataRoot = value;
    }

    private static string? cacheRoot;
    /// <summary>
    /// 可写的缓存根目录 (雪碧图等可再生内容)
    /// </summary>
    public static string CacheRoot
    {
        get => cacheRoot ??= ResolveCacheRoot();
        set => cacheRoot = value;
    }

    /// <summary>
    /// MOD 目录
    /// </summary>
    public static string ModRoot => Path.Combine(DataRoot, "mod");

    /// <summary>
    /// 存档目录
    /// </summary>
    public static string SaveRoot => Path.Combine(DataRoot, "Saves");

    /// <summary>
    /// MOD 根目录, 按优先级排列
    /// </summary>
    /// 便携模式下就一个; 非便携时用户数据目录优先于安装目录, 这样系统装的 MOD
    /// 能被用户自己放的同名 MOD 覆盖
    public static IEnumerable<string> ModRoots
    {
        get
        {
            yield return ModRoot;
            if (!IsPortable)
                yield return Path.Combine(InstallDirectory, "mod");
        }
    }

    /// <summary>
    /// MOD 可写的数据目录
    /// </summary>
    /// 与 Windows 版 ExtensionValue.BaseDirectory 一个意思, 只是落点不同
    public static string DataDirectory => DataRoot;

    /// <summary>
    /// 某个 MOD 专用的可写目录, 不存在会自动建
    /// </summary>
    public static string GetModStorage(string modName)
        => EnsureDirectory(Path.Combine(DataRoot, "ModStorage", modName));

    /// <summary>
    /// 确保目录存在并返回它
    /// </summary>
    public static string EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// 是否为便携模式 (数据就放在安装目录旁边)
    /// </summary>
    /// Windows 上默认便携, 以保持与现有 Windows 版完全一致的行为;
    /// 其他平台只有在安装目录旁放了 portable.txt 且该目录可写时才便携.
    public static bool IsPortable => isPortable ??= ResolveIsPortable();

    private static bool? isPortable;

    private static string ResolveInstallDirectory()
    {
        // 单文件发布时 Assembly.Location 是空字符串, 这时要退回 AppContext
        var location = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(location))
        {
            var directory = Path.GetDirectoryName(location);
            if (!string.IsNullOrEmpty(directory))
                return directory;
        }
        return AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    }

    private static bool ResolveIsPortable()
    {
        if (OperatingSystem.IsWindows())
            return true;
        var marker = Path.Combine(InstallDirectory, "portable.txt");
        return File.Exists(marker) && IsWritable(InstallDirectory);
    }

    private static string ResolveDataRoot()
    {
        if (IsPortable)
            return InstallDirectory;
        if (OperatingSystem.IsMacOS())
            return Path.Combine(HomeDirectory(), "Library", "Application Support", "VPet");
        // Linux 及其他类 Unix: 遵循 XDG 基础目录规范
        return Path.Combine(EnvironmentDirectory("XDG_DATA_HOME", ".local", "share"), "vpet");
    }

    private static string ResolveCacheRoot()
    {
        if (IsPortable)
            return Path.Combine(InstallDirectory, "cache");
        if (OperatingSystem.IsMacOS())
            return Path.Combine(HomeDirectory(), "Library", "Caches", "VPet");
        return Path.Combine(EnvironmentDirectory("XDG_CACHE_HOME", ".cache"), "vpet");
    }

    /// <summary>
    /// 读取 XDG 环境变量, 未设置时回退到 ~ 下的默认相对路径
    /// </summary>
    private static string EnvironmentDirectory(string variable, params string[] fallback)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        // XDG 规范要求必须是绝对路径, 相对路径按"未设置"处理
        if (!string.IsNullOrEmpty(value) && Path.IsPathRooted(value))
            return value;
        var parts = new string[fallback.Length + 1];
        parts[0] = HomeDirectory();
        Array.Copy(fallback, 0, parts, 1, fallback.Length);
        return Path.Combine(parts);
    }

    private static string HomeDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(home))
            return home;
        // 某些容器环境里 UserProfile 是空的
        return Environment.GetEnvironmentVariable("HOME") ?? InstallDirectory;
    }

    private static bool IsWritable(string directory)
    {
        try
        {
            var probe = Path.Combine(directory, ".vpet_write_probe");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
