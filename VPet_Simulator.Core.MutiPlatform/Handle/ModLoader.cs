using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Dictionary;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 纯数据 MOD 加载器
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Windows/Function/CoreMOD.cs 里除 plugin 之外的部分.
///
/// 代码型 MOD 分两种: 老的 Windows 专用插件是编译好的 WPF DLL, 依赖
/// PresentationFramework 和 Panuon.WPF.UI, 在 Linux/macOS 上根本加载不了 ——
/// 遇到它们会记一条提示而不是尝试加载(静默跳过会让用户以为 MOD 生效了);
/// 按统一契约写的插件两个平台都能跑, 由 PluginLoader 负责加载.
///
/// 目前支持 pet(宠物动画)、lang(语言包)、food(食物)、image(图片) 和 text(说话文本).
/// photo/theme 依赖 VPet-Simulator.Windows.Interface 里的数据模型,
/// 那一层还没有跨平台化.
public class ModLoader
{
    /// <summary>
    /// MOD 名称
    /// </summary>
    public string Name { get; private set; } = "";

    /// <summary>
    /// MOD 简介
    /// </summary>
    public string Intro { get; private set; } = "";

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; private set; } = "";

    /// <summary>
    /// MOD 目录
    /// </summary>
    public DirectoryInfo Path { get; }

    /// <summary>
    /// 这个 MOD 提供了哪些内容
    /// </summary>
    public HashSet<string> Tag { get; } = new HashSet<string>();

    /// <summary>
    /// 作者的 Steam 号
    /// </summary>
    public long AuthorID { get; private set; }

    /// <summary>
    /// 创意工坊物品号
    /// </summary>
    public ulong ItemID { get; private set; }

    /// <summary>
    /// 要求的游戏版本
    /// </summary>
    public int GameVer { get; private set; }

    /// <summary>
    /// MOD 自己的版本
    /// </summary>
    public int Ver { get; private set; }

    /// <summary>
    /// 代码插件目录, 没有则为 null
    /// </summary>
    /// 由 PluginLoader 在读档之前统一处理: 这里只负责找出来, 不负责加载
    public DirectoryInfo? PluginDirectory { get; private set; }

    /// <summary>
    /// 加载过程中遇到的问题
    /// </summary>
    public List<string> Warnings { get; } = new List<string>();

    private ModLoader(DirectoryInfo path)
    {
        Path = path;
    }

    /// <summary>
    /// 扫描一组 MOD 根目录, 把里面的宠物和语言包都加载进来
    /// </summary>
    /// <param name="roots">MOD 根目录, 按优先级排列</param>
    /// <param name="pets">加载出来的宠物, 同名宠物会被合并</param>
    /// <param name="resources">加载出来的食物和图片</param>
    /// <returns>扫描到的 MOD</returns>
    public static List<ModLoader> LoadAll(IEnumerable<string> roots, List<PetLoader> pets, ModResources resources)
    {
        var result = new List<ModLoader>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                continue;
            foreach (var directory in new DirectoryInfo(root).EnumerateDirectories()
                .OrderBy(d => d.Name, StringComparer.Ordinal))
            {
                // 同名 MOD 只取优先级最高的那一份, 避免用户数据目录和安装目录里
                // 各有一份时被加载两遍
                if (!seen.Add(directory.Name))
                    continue;
                var mod = Load(directory, pets, resources);
                if (mod != null)
                    result.Add(mod);
            }
        }
        // 食物和图片可能来自不同的 MOD, 全部扫完才能配对
        resources.ResolveFoodImages();
        return result;
    }

    /// <summary>
    /// 加载单个 MOD 目录
    /// </summary>
    /// <returns>没有 info.lps 的目录不是 MOD, 返回 null</returns>
    public static ModLoader? Load(DirectoryInfo directory, List<PetLoader> pets, ModResources resources)
    {
        var infoPath = System.IO.Path.Combine(directory.FullName, "info.lps");
        if (!File.Exists(infoPath))
            return null;

        var mod = new ModLoader(directory);
        //info.lps 的解析走共享后端, 与 Windows 版读出来的字段逐项一致
        var meta = ModInfoReader.Parse(directory);
        if (meta == null)
            return null;
        mod.Name = meta.Name;
        mod.Intro = meta.Intro;
        mod.Author = meta.Author;
        mod.AuthorID = meta.AuthorID;
        mod.ItemID = meta.ItemID;
        mod.GameVer = meta.GameVer;
        mod.Ver = meta.Ver;
        mod.Warnings.AddRange(meta.Warnings);
        //MOD 没启用时也要能翻译它的名字和简介
        foreach (var (culture, line) in meta.PreTranslations)
        {
            var translations = new List<ILine>();
            foreach (var sub in line)
                translations.Add(new Line(sub.Name, sub.info));
            LocalizeCore.AddCulture(culture, translations);
        }
        if (meta.Warnings.Count != 0)
            return mod;

        foreach (var sub in directory.EnumerateDirectories())
        {
            try
            {
                switch (sub.Name.ToLowerInvariant())
                {
                    case "pet":
                        mod.LoadPet(sub, pets);
                        break;
                    case "lang":
                        mod.LoadLanguage(sub);
                        break;
                    case "food":
                        mod.LoadFood(sub, resources);
                        break;
                    case "text":
                        mod.LoadText(sub, resources);
                        break;
                    case "image":
                        mod.Tag.Add("image");
                        resources.AddImages(sub);
                        break;
                    case "theme":
                        mod.LoadTheme(sub, resources);
                        break;
                    case "photo":
                        mod.Tag.Add("photo");
                        resources.Photos.LoadPhotos(sub);
                        break;
                    case "file":
                        //照片本体压在这里的 zlps/zip 里, 同时也建一份文件索引给 MOD 用
                        mod.Tag.Add("file");
                        resources.Photos.LoadArchives(sub);
                        resources.AddFiles(sub);
                        break;
                    case "plugin":
                        // 认不认得出来交给 PluginLoader, 那里会区分老的 Windows
                        // 专用插件和按统一契约写的插件
                        mod.Tag.Add("plugin");
                        mod.PluginDirectory = sub;
                        break;
                }
            }
            catch (Exception ex)
            {
                mod.Warnings.Add($"{sub.Name} 加载失败: {ex.Message}");
            }
        }
        return mod;
    }

    /// <summary>
    /// 加载宠物动画
    /// </summary>
    /// 同名宠物会被合并: 后来的 MOD 往已有宠物上追加动画路径并叠加设置,
    /// 这是 MOD 给官方宠物加动作的标准做法.
    private void LoadPet(DirectoryInfo directory, List<PetLoader> pets)
    {
        foreach (var file in directory.EnumerateFiles("*.lps"))
        {
            var lps = new LpsDocument(File.ReadAllText(file.FullName));
            var first = lps.First();
            if (first == null || first.Name.ToLowerInvariant() != "pet")
                continue;

            var name = first.Info;
            if (name == "默认虚拟桌宠")
                name = "vup";//旧版本名称兼容

            var exist = pets.FirstOrDefault(x => x.Name == name);
            if (exist == null)
            {
                Tag.Add("pet");
                var loader = new PetLoader(lps, directory);
                if (loader.Config.Works.Count > 0)
                    Tag.Add("work");
                pets.Add(loader);
            }
            else
            {
                if (lps.FindAllLine("work").Length >= 0)
                {
                    Tag.Add("work");
                }
                var extra = System.IO.Path.Combine(directory.FullName, NormalizeRelativePath(first[(gstr)"path"] ?? ""));
                var extraDirectory = new DirectoryInfo(extra);
                if (extraDirectory.Exists && extraDirectory.GetDirectories().Length > 0)
                    Tag.Add("pet");
                exist.path.Add(extra);
                exist.Config.Set(lps);
            }
        }
    }

    /// <summary>
    /// 加载食物
    /// </summary>
    /// 同名食物后来的覆盖之前的, 与 Windows 版一致 —— MOD 靠这个调整官方食物的数值
    private void LoadFood(DirectoryInfo directory, ModResources resources)
    {
        Tag.Add("food");
        foreach (var file in directory.EnumerateFiles("*.lps"))
        {
            foreach (var line in new LpsDocument(File.ReadAllText(file.FullName)))
            {
                if (line.Name.ToLowerInvariant() != "food")
                    continue;
                var food = LPSConvert.DeserializeObject<FoodItem>(line);
                if (string.IsNullOrEmpty(food.Name))
                    continue;
                var index = resources.Foods.FindIndex(x => x.Name == food.Name);
                if (index >= 0)
                    resources.Foods[index] = food;
                else
                    resources.Foods.Add(food);
            }
        }
    }

    /// <summary>
    /// 加载说话文本
    /// </summary>
    /// 认 Windows 版 CoreMOD 里那四种行名. schedulepackage(日程) 依赖还没移植的界面, 跳过.
    private void LoadText(DirectoryInfo directory, ModResources resources)
    {
        Tag.Add("text");
        foreach (var file in directory.EnumerateFiles("*.lps"))
        {
            foreach (var line in new LpsDocument(File.ReadAllText(file.FullName)))
            {
                switch (line.Name.ToLowerInvariant())
                {
                    case "lowfoodtext":
                        resources.LowFoodTexts.Add(LPSConvert.DeserializeObject<LowText>(line));
                        Tag.Add("lowtext");
                        break;
                    case "lowdrinktext":
                        resources.LowDrinkTexts.Add(LPSConvert.DeserializeObject<LowText>(line));
                        Tag.Add("lowtext");
                        break;
                    case "clicktext":
                        resources.ClickTexts.Add(LPSConvert.DeserializeObject<ClickText>(line));
                        Tag.Add("clicktext");
                        break;
                    case "selecttext":
                        resources.SelectTexts.Add(LPSConvert.DeserializeObject<SelectText>(line));
                        Tag.Add("selecttext");
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 加载主题
    /// </summary>
    /// 同名主题后来的覆盖之前的, 与 Windows 版一致 —— MOD 换皮肤靠的就是这个
    private void LoadTheme(DirectoryInfo directory, ModResources resources)
    {
        Tag.Add("theme");
        //MOD 自带的 ttf 装不上(见 FontLoader 的说明), 但要让玩家知道有这么回事
        foreach (var font in Display.FontLoader.FindModFonts(directory))
            Warnings.Add($"自带字体「{font}」跨平台版装不上, 需要手动装进系统才能用");
        foreach (var file in directory.EnumerateFiles("*.lps"))
        {
            var theme = ThemeInfo.Parse(new LpsDocument(File.ReadAllText(file.FullName)));
            if (theme == null)
                continue;
            theme.LoadImages(directory);
            var index = resources.Themes.FindIndex(x => x.XName == theme.XName);
            if (index >= 0)
                resources.Themes[index] = theme;
            else
                resources.Themes.Add(theme);
        }
    }

    /// <summary>
    /// 加载语言包
    /// </summary>
    private void LoadLanguage(DirectoryInfo directory)
    {
        Tag.Add("lang");
        foreach (var file in directory.EnumerateFiles("*.lps"))
        {
            LocalizeCore.AddCulture(System.IO.Path.GetFileNameWithoutExtension(file.Name),
                new LPS_D(File.ReadAllText(file.FullName)));
        }
        foreach (var sub in directory.EnumerateDirectories())
        {
            foreach (var file in sub.EnumerateFiles("*.lps"))
            {
                LocalizeCore.AddCulture(sub.Name, new LPS_D(File.ReadAllText(file.FullName)));
            }
        }
    }

    /// <summary>
    /// 把 MOD 数据里声明的相对路径转换成当前平台的写法
    /// </summary>
    /// MOD 的 lps 里路径是按 Windows 习惯写的(例如 path#Happy\back_lay), 这是数据
    /// 不是代码, 没法要求 MOD 作者改, 只能在读取时统一转换.
    private static string NormalizeRelativePath(string path)
    {
        if (System.IO.Path.DirectorySeparatorChar == '\\')
            return path;
        return path.Replace('\\', System.IO.Path.DirectorySeparatorChar);
    }
}
