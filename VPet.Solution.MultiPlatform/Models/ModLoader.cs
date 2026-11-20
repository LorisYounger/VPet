using Avalonia.Media.Imaging;
using LinePutScript;
using LinePutScript.Dictionary;
using LinePutScript.Localization;

namespace VPet.Solution.MultiPlatform.Models;

/// <summary>
/// 模组加载器
/// </summary>
public class ModLoader
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 如果是上传至 Steam, 则为 SteamUserID
    /// </summary>
    public long AuthorID { get; set; }

    /// <summary>
    /// 上传至 Steam 的 ItemID
    /// </summary>
    public ulong ItemID { get; set; }

    /// <summary>
    /// 简介
    /// </summary>
    public string Intro { get; set; } = string.Empty;

    /// <summary>
    /// 模组路径
    /// </summary>
    public string ModPath { get; set; }

    /// <summary>
    /// 支持的游戏版本
    /// </summary>
    public int GameVer { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    public int Ver { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    public HashSet<string> Tags { get; set; } = [];

    /// <summary>
    /// 缓存数据日期
    /// </summary>
    public DateTime CacheDate { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 模组图标
    /// </summary>
    public Bitmap? Image { get; set; }

    /// <summary>
    /// 读取是否成功
    /// </summary>
    public bool IsSuccesses { get; set; } = true;

    /// <summary>
    /// 主题列表
    /// </summary>
    public List<string> Themes { get; set; } = [];

    /// <summary>
    /// 字体列表
    /// </summary>
    public List<string> Fonts { get; set; } = [];

    /// <summary>
    /// 桌宠动画列表
    /// </summary>
    public List<string> PetGraphs { get; set; } = [];

    public ModLoader(string path)
    {
        ModPath = path;
        var infoFile = Path.Combine(path, "info.lps");
        if (File.Exists(infoFile) is false)
        {
            Name = Path.GetFileName(path);
            IsSuccesses = false;
            return;
        }

        var modlps = new LPS(File.ReadAllText(infoFile));
        Name = modlps.FindLine("vupmod")?.Info ?? Path.GetFileName(path);
        Intro = modlps.FindLine("intro")?.Info ?? string.Empty;
        GameVer = modlps.FindSub("gamever")?.InfoToInt ?? 0;
        Ver = modlps.FindSub("ver")?.InfoToInt ?? 0;
        Author = modlps.FindSub("author")?.Info.Split('[').First() ?? string.Empty;
        AuthorID = modlps.FindLine("authorid")?.InfoToInt64 ?? 0;
        var itemStr = modlps.FindLine("itemid")?.info;
        if (string.IsNullOrWhiteSpace(itemStr) is false)
            ItemID = Convert.ToUInt64(itemStr);
        CacheDate = modlps.GetDateTime("cachedate", DateTime.MinValue);
        var imagePath = Path.Combine(path, "icon.png");

        // 加载翻译
        foreach (var line in modlps.FindAllLine("lang"))
        {
            var lps = new LPS();
            foreach (var sub in line)
                lps.Add(new Line(sub.Name, sub.Info));
            LocalizeCore.AddCulture(line.Info, lps);
        }

        if (File.Exists(imagePath))
        {
            try
            {
                Image = NativeUtils.LoadImageToStream(imagePath);
            }
            catch { }
        }

        foreach (var dir in Directory.EnumerateDirectories(path))
        {
            var dirName = Path.GetFileName(dir);
            switch (dirName.ToLowerInvariant())
            {
                case "pet":
                    Tags.Add("pet");
                    foreach (var petFile in Directory.EnumerateFiles(dir, "*.lps"))
                    {
                        PetGraphs.Add(Path.GetFileNameWithoutExtension(petFile));
                    }
                    break;
                case "food":
                    Tags.Add("food");
                    break;
                case "image":
                    Tags.Add("image");
                    break;
                case "text":
                    Tags.Add("text");
                    break;
                case "theme":
                    foreach (var themeFile in Directory.EnumerateFiles(dir, "*.lps"))
                    {
                        Themes.Add(Path.GetFileNameWithoutExtension(themeFile));
                    }
                    if (Themes.Count > 0)
                        Tags.Add("theme");

                    var fontsDir = Path.Combine(dir, "fonts");
                    if (Directory.Exists(fontsDir))
                    {
                        foreach (var fontFile in Directory.EnumerateFiles(fontsDir))
                        {
                            Fonts.Add(Path.GetFileNameWithoutExtension(fontFile));
                        }
                    }
                    if (Fonts.Count > 0)
                        Tags.Add("theme");
                    break;
                case "lang":
                    Tags.Add("lang");
                    LoadLanguageDirectory(dir);
                    break;
            }
        }
    }

    /// <summary>
    /// 加载本体语言 (部署后位于 mod/0000_core/lang), 需在加载默认语言前调用
    /// </summary>
    public static void LoadCoreLanguage()
    {
        var langDir = Path.Combine(AppContext.BaseDirectory, "mod", "0000_core", "lang");
        if (Directory.Exists(langDir))
            LoadLanguageDirectory(langDir);
    }

    private static void LoadLanguageDirectory(string langDir)
    {
        foreach (var langFile in Directory.EnumerateFiles(langDir, "*.lps"))
        {
            LocalizeCore.AddCulture(
                Path.GetFileNameWithoutExtension(langFile),
                new LPS_D(File.ReadAllText(langFile))
            );
        }
        foreach (var langSubDir in Directory.EnumerateDirectories(langDir))
        {
            foreach (var langFile in Directory.EnumerateFiles(langSubDir, "*.lps"))
            {
                LocalizeCore.AddCulture(
                    Path.GetFileName(langSubDir),
                    new LPS_D(File.ReadAllText(langFile))
                );
            }
        }
    }
}
