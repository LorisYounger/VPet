using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 多开: 一个进程里养几只桌宠
/// </summary>
/// 每只桌宠有自己的一套设置和存档, 靠"前缀"区分:
///
///   Setting.lps        / Saves/Save_100001.lps        —— 默认那只(前缀为空)
///   Setting-小黑.lps   / Saves/Save-小黑_100001.lps   —— 叫"小黑"的那只
///
/// 命名与 Windows 版逐字一致, 所以整个数据目录可以在两个平台之间拷。
///
/// 名字里的连字符是前缀的一部分: Windows 版存的时候拼的就是 "Setting" + 前缀,
/// 而前缀本身带着连字符(见 Normalize)。
public static class MultiPetStore
{
    /// <summary>
    /// 设置文件名的前半段
    /// </summary>
    public const string SettingPrefix = "Setting";

    /// <summary>
    /// 设置文件的扩展名
    /// </summary>
    public const string SettingExtension = ".lps";

    /// <summary>
    /// 把玩家输入的名字变成真正的前缀
    /// </summary>
    /// <returns>空名字给空前缀(那是默认的那只)</returns>
    /// 与 Windows 版一致: 非空名字前面带一个连字符
    public static string Normalize(string? name)
    {
        var trimmed = name?.Trim().Trim('-') ?? string.Empty;
        return string.IsNullOrEmpty(trimmed) ? string.Empty : "-" + trimmed;
    }

    /// <summary>
    /// 把前缀变回给玩家看的名字
    /// </summary>
    public static string DisplayName(string prefix)
        => string.IsNullOrEmpty(prefix) ? string.Empty : prefix.TrimStart('-');

    /// <summary>
    /// 某个前缀对应的设置文件路径
    /// </summary>
    public static string SettingPath(string dataRoot, string prefix)
        => Path.Combine(dataRoot, SettingPrefix + prefix + SettingExtension);

    /// <summary>
    /// 数据目录里现有哪几只桌宠
    /// </summary>
    /// <returns>前缀列表, 默认那只(空前缀)永远在第一个</returns>
    public static List<string> List(string dataRoot)
    {
        var result = new List<string>();
        if (Directory.Exists(dataRoot))
        {
            foreach (var file in new DirectoryInfo(dataRoot).GetFiles(SettingPrefix + "*" + SettingExtension))
            {
                var name = Path.GetFileNameWithoutExtension(file.Name);
                // "Setting" 之后的部分就是前缀
                var prefix = name.Length > SettingPrefix.Length
                    ? name.Substring(SettingPrefix.Length)
                    : string.Empty;
                // 旧版本多开留下过一个空名字的 Setting-.lps, 它不是一只真的桌宠
                if (prefix == "-")
                    continue;
                if (!result.Contains(prefix, StringComparer.Ordinal))
                    result.Add(prefix);
            }
        }
        if (!result.Contains(string.Empty, StringComparer.Ordinal))
            result.Insert(0, string.Empty);
        else
        {
            result.Remove(string.Empty);
            result.Insert(0, string.Empty);
        }
        return result;
    }

    /// <summary>
    /// 从命令行参数里取出要开哪只
    /// </summary>
    /// <returns>没指定时返回 null</returns>
    /// 认两种写法: `--prefix 小黑` 和 Windows 版那种 `prefix#小黑:|`
    public static string? ReadPrefixArgument(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--prefix" && i + 1 < args.Length)
                return Normalize(args[i + 1]);
            if (args[i].StartsWith("prefix#", StringComparison.Ordinal))
                return Normalize(args[i].Substring(7).TrimEnd('|').TrimEnd(':'));
        }
        return null;
    }

    /// <summary>
    /// 数据目录里有没有 startup_ 标记指定默认开哪只
    /// </summary>
    /// 与 Windows 版一致: 文件名 startup_<名字>
    public static string? ReadStartupMarker(string dataRoot)
    {
        if (!Directory.Exists(dataRoot))
            return null;
        var file = new DirectoryInfo(dataRoot).GetFiles("startup_*").FirstOrDefault();
        return file == null ? null : Normalize(file.Name.Substring(8));
    }

    /// <summary>
    /// 记下默认开哪只
    /// </summary>
    /// <param name="prefix">空前缀表示不指定</param>
    public static void WriteStartupMarker(string dataRoot, string prefix)
    {
        if (!Directory.Exists(dataRoot))
            return;
        foreach (var old in new DirectoryInfo(dataRoot).GetFiles("startup_*"))
        {
            try { old.Delete(); }
            catch (IOException) { }
        }
        if (string.IsNullOrEmpty(prefix))
            return;
        try { File.WriteAllText(Path.Combine(dataRoot, "startup_" + DisplayName(prefix)), string.Empty); }
        catch (IOException) { }
    }
}
