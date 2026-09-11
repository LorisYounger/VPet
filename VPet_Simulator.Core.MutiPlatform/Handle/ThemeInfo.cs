using LinePutScript;
using LinePutScript.Localization;
using System.Collections.Generic;
using System.IO;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 一套主题
/// </summary>
/// 与 Windows 版 Theme 是同一份数据的两个读法: 首行是名字和图片包目录,
/// 其余每行一个配色. MOD 换皮肤靠的就是提供同名主题覆盖官方那套。
public class ThemeInfo
{
    /// <summary>
    /// 主题标识, 也是设置里记的那个名字
    /// </summary>
    public string XName { get; }

    /// <summary>
    /// 主题显示名
    /// </summary>
    public string Name { get; }

    private string? transname;

    /// <summary>
    /// 名字 (翻译)
    /// </summary>
    public string TranslateName => transname ??= LocalizeCore.Translate(Name);

    /// <summary>
    /// 图片包目录名
    /// </summary>
    public string Image { get; }

    /// <summary>
    /// 配色
    /// </summary>
    public ILPS ThemeColor { get; }

    /// <summary>
    /// 主题自带的图片
    /// </summary>
    public ResourceIndexAdapter Images { get; } = new ResourceIndexAdapter();

    private ThemeInfo(string xName, string name, string image, ILPS themeColor)
    {
        XName = xName;
        Name = name;
        Image = image;
        ThemeColor = themeColor;
    }

    /// <summary>
    /// 从主题 lps 读出一套主题
    /// </summary>
    /// <returns>格式不对时返回 null</returns>
    public static ThemeInfo? Parse(LpsDocument lps)
    {
        var first = lps.First();
        if (first == null)
            return null;
        var image = first.Find("image")?.Info ?? string.Empty;
        var xName = first.Name;
        var name = first.Info;
        lps.RemoveAt(0);
        return new ThemeInfo(xName, name, image, lps);
    }

    /// <summary>
    /// 收录主题自带的图片包
    /// </summary>
    /// <param name="themeDirectory">主题 lps 所在目录</param>
    /// 与 Windows 版一致: 图片包目录下一层的 png 直接用文件名当键, 再下一层的
    /// 加上目录名前缀。
    public void LoadImages(DirectoryInfo themeDirectory)
    {
        if (string.IsNullOrEmpty(Image))
            return;
        var directory = new DirectoryInfo(Path.Combine(themeDirectory.FullName, Image));
        if (!directory.Exists)
            return;
        foreach (var file in directory.EnumerateFiles("*.png"))
            Images.Add(Path.GetFileNameWithoutExtension(file.Name), file.FullName);
        foreach (var sub in directory.EnumerateDirectories())
            foreach (var file in sub.EnumerateFiles("*.png"))
                Images.Add(sub.Name + "_" + Path.GetFileNameWithoutExtension(file.Name), file.FullName);
    }
}

/// <summary>
/// 名字到图片路径的小表
/// </summary>
/// 主题的图片包和 MOD 的图片索引是两回事(主题的会在切换主题时整套覆盖上去),
/// 所以单独存一份。
public class ResourceIndexAdapter
{
    private readonly Dictionary<string, string> sources
        = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>名字到路径</summary>
    public IReadOnlyDictionary<string, string> Sources => sources;

    /// <summary>登记一张图</summary>
    public void Add(string name, string path) => sources[name.ToLowerInvariant()] = path;
}
