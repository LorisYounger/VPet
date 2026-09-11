using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 字体
/// </summary>
/// Windows 版是这样用 MOD 字体的: `new FontFamily("file:///<目录>/#<字体名>")`,
/// WPF 认这种写法, 会去那个目录里找同名的 ttf.
///
/// **Avalonia 不认**. 它的 FontFamily 只接受 avares:// 这类程序集内资源, 磁盘上的
/// ttf 要自己实现 IFontCollection 才装得上. 那是一整套活儿, 而且装不上的后果只是
/// "字体不是 MOD 作者选的那个", 不影响任何玩法 —— 所以这里按 D-14 的默认走:
/// 先只认系统已装的字体, MOD 自带的 ttf 记一条提示告诉玩家怎么手动装.
///
/// 字体名照样从设置里读, 与 Windows 版同一个 font 键, 设置文件可以两边互拷.
public static class FontLoader
{
    /// <summary>
    /// 资源字典里字体的键名
    /// </summary>
    public const string ResourceKey = "VPetFont";

    /// <summary>
    /// 找不到指定字体时用的
    /// </summary>
    /// 空的 FontFamily 表示"系统默认", 在三个平台上各自是合理的中文字体
    public static FontFamily Fallback => FontFamily.Default;

    /// <summary>
    /// 按名字取一个系统字体
    /// </summary>
    /// <param name="name">字体名</param>
    /// <returns>系统里没有就返回默认字体</returns>
    public static FontFamily Resolve(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Fallback;
        var installed = FontManager.Current.SystemFonts
            .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        return installed ?? Fallback;
    }

    /// <summary>
    /// 把字体写进资源字典
    /// </summary>
    /// <param name="name">设置里记的字体名</param>
    /// <returns>真的用上了这个名字返回 true</returns>
    public static bool Apply(string? name, IResourceDictionary resources)
    {
        var font = Resolve(name);
        resources[ResourceKey] = font;
        return !string.IsNullOrWhiteSpace(name) && font != Fallback;
    }

    /// <summary>
    /// 找出 MOD 自带但装不上的字体
    /// </summary>
    /// <param name="themeDirectory">MOD 的 theme 目录</param>
    /// <returns>那些 ttf 的字体名</returns>
    /// 与 Windows 版一致: MOD 把 ttf 放在 theme/fonts 下
    public static List<string> FindModFonts(DirectoryInfo themeDirectory)
    {
        var result = new List<string>();
        var fonts = new DirectoryInfo(Path.Combine(themeDirectory.FullName, "fonts"));
        if (!fonts.Exists)
            return result;
        foreach (var file in fonts.EnumerateFiles("*.ttf"))
            result.Add(Path.GetFileNameWithoutExtension(file.Name));
        return result;
    }
}
