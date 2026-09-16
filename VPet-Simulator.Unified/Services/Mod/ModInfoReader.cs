using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 一个 MOD 的 info.lps 里记着什么
/// </summary>
public class ModMetadata
{
    /// <summary>MOD 名称, 也是整个体系里的主键</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>简介</summary>
    public string Intro { get; set; } = string.Empty;
    /// <summary>作者</summary>
    public string Author { get; set; } = string.Empty;
    /// <summary>作者的 Steam 号</summary>
    public long AuthorID { get; set; }
    /// <summary>创意工坊物品号</summary>
    public ulong ItemID { get; set; }
    /// <summary>要求的游戏版本</summary>
    public int GameVer { get; set; }
    /// <summary>MOD 自己的版本</summary>
    public int Ver { get; set; }
    /// <summary>缓存失效日期</summary>
    public DateTime CacheDate { get; set; }
    /// <summary>要跳过加载的 dll 文件名</summary>
    public List<string> DllSkip { get; } = new List<string>();
    /// <summary>MOD 未启用时也要生效的翻译(用来翻译 MOD 自己的名字和简介)</summary>
    public List<(string Culture, ILine Line)> PreTranslations { get; } = new List<(string, ILine)>();
    /// <summary>MOD 目录</summary>
    public DirectoryInfo? Path { get; set; }
    /// <summary>这个 MOD 提供了哪些内容(子目录名)</summary>
    public HashSet<string> ContentTags { get; } = new HashSet<string>();
    /// <summary>解析过程中遇到的问题</summary>
    public List<string> Warnings { get; } = new List<string>();
}

/// <summary>
/// 读 MOD 的 info.lps
/// </summary>
/// 从 Windows 版 CoreMOD 的构造函数里抽出来的, 键名和取值方式一字未改.
/// 两个平台共用, 这样"MOD 列表长什么样"在两边永远一致.
public static class ModInfoReader
{
    /// <summary>
    /// 解析一个 MOD 目录
    /// </summary>
    /// <returns>没有 info.lps 的目录不是 MOD, 返回 null</returns>
    public static ModMetadata? Parse(DirectoryInfo directory)
    {
        var infoPath = System.IO.Path.Combine(directory.FullName, "info.lps");
        if (!File.Exists(infoPath))
            return null;

        var mod = new ModMetadata { Path = directory };
        try
        {
            var info = new LpsDocument(File.ReadAllText(infoPath));
            mod.Name = info.FindLine("vupmod")?.Info ?? string.Empty;
            mod.Intro = info.FindLine("intro")?.Info ?? string.Empty;
            mod.GameVer = info.FindSub("gamever")?.InfoToInt ?? 0;
            mod.Ver = info.FindSub("ver")?.InfoToInt ?? 0;

            // 作者名后面可能跟着 [xxx] 之类的标记, 与 Windows 版一样在第一个 [ 处截断
            var author = info.FindSub("author")?.Info ?? string.Empty;
            int bracket = author.IndexOf('[');
            mod.Author = bracket >= 0 ? author.Substring(0, bracket) : author;

            // authorid / itemid 是**行**不是子项, 与 gamever / ver / author 那几个不同
            var authorId = info.FindLine("authorid");
            if (authorId != null)
                mod.AuthorID = authorId.InfoToInt64;
            var itemId = info.FindLine("itemid");
            if (itemId != null && ulong.TryParse(itemId.Info, out var id))
                mod.ItemID = id;

            mod.CacheDate = info.GetDateTime("cachedate", DateTime.MinValue);
            // 去掉不合理的清理缓存日期: 有人把日期填到未来就能让缓存永远失效
            if (mod.CacheDate > DateTime.Now)
                mod.CacheDate = DateTime.MinValue;

            foreach (var skip in info["dllskip"])
                mod.DllSkip.Add(skip.Name);

            // MOD 没启用时也要能翻译它的名字和简介, 所以这些翻译行单独收着
            foreach (var line in info.FindAllLine("lang"))
                mod.PreTranslations.Add((line.Info, line));
        }
        catch (Exception ex)
        {
            mod.Warnings.Add($"info.lps 解析失败: {ex.Message}");
            return mod;
        }

        foreach (var sub in directory.EnumerateDirectories())
            mod.ContentTags.Add(sub.Name.ToLowerInvariant());

        return mod;
    }

    /// <summary>
    /// 扫描一组 MOD 根目录
    /// </summary>
    /// <param name="roots">MOD 根目录, 按优先级排列</param>
    /// <returns>扫描到的 MOD; 同名的只保留优先级最高的那份并给后来者记一条警告</returns>
    /// MOD 名是整个体系的主键: Windows 版遇到重名会给后来的那个加"(MOD名称重复)"
    /// 并整个不加载. 这里同样只留第一份, 把重复记进警告让界面显示.
    public static List<ModMetadata> ScanAll(IEnumerable<string> roots)
    {
        var result = new List<ModMetadata>();
        var seenDirectory = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                continue;
            foreach (var directory in new DirectoryInfo(root).EnumerateDirectories()
                .OrderBy(d => d.Name, StringComparer.Ordinal))
            {
                // 同名目录只取优先级最高的那份, 避免用户数据目录和安装目录里各有一份
                if (!seenDirectory.Add(directory.Name))
                    continue;
                var mod = Parse(directory);
                if (mod == null)
                    continue;
                if (!string.IsNullOrEmpty(mod.Name) && !seenName.Add(mod.Name))
                    mod.Warnings.Add("MOD 名称重复, 已跳过");
                result.Add(mod);
            }
        }
        return result;
    }
}
