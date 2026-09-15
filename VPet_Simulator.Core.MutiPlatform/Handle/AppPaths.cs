using System;
using System.IO;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 跨平台程序的目录, 所有资源和数据都放在运行文件所在目录下。
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// 当前运行目录。AppContext.BaseDirectory 在普通发布和单文件发布时都指向运行文件所在目录。
    /// </summary>
    public static string InstallDirectory { get; } = Path.GetFullPath(AppContext.BaseDirectory);

    /// <summary>数据根目录, 与运行目录一致。</summary>
    public static string DataRoot => InstallDirectory;

    /// <summary>缓存目录。</summary>
    public static string CacheRoot => Path.Combine(InstallDirectory, "cache");

    /// <summary>本地 MOD 目录。</summary>
    public static string ModRoot => Path.Combine(InstallDirectory, "mod");

    /// <summary>存档目录。</summary>
    public static string SaveRoot => Path.Combine(InstallDirectory, "Saves");

    /// <summary>数据目录, 与运行目录一致。</summary>
    public static string DataDirectory => InstallDirectory;

    /// <summary>某个 MOD 专用的存储目录, 不存在会自动创建。</summary>
    public static string GetModStorage(string modName)
        => EnsureDirectory(Path.Combine(InstallDirectory, "ModStorage", modName));

    /// <summary>确保目录存在并返回它。</summary>
    public static string EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }
}
