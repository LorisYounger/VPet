using Avalonia.Media;
using Avalonia.Media.Fonts;
using System;
using System.Collections.Generic;
using System.IO;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// MOD 自带的字体
/// </summary>
/// 跨平台: Windows 版是 `new FontFamily("<目录>\#<字体名>")`, WPF 会去那个目录找同名的 ttf.
/// Avalonia 的 FontFamily 只认登记过的字体集 (avares:// 是内置的一套), 磁盘上的 ttf 要自己登记:
/// 这里把 MOD 的 ttf 收进一个键为 fonts:vpetmod 的字体集, 每个文件按"文件名(不带扩展名)"登记一遍,
/// 与 Windows 版 IFont.Name 的取法一致, 于是 `new FontFamily("fonts:vpetmod#<字体名>")` 就能用.
public sealed class FontLoader : FontCollectionBase
{
    /// <summary>
    /// 字体集的键, FontFamily 写成 fonts:vpetmod#字体名
    /// </summary>
    public static readonly Uri CollectionKey = new Uri("fonts:vpetmod");

    private static FontLoader? instance;
    private static readonly object locker = new();
    private readonly HashSet<string> loaded = new(StringComparer.OrdinalIgnoreCase);

    public override Uri Key => CollectionKey;

    /// <summary>
    /// 登记一个 ttf, 之后可以按文件名当字体名用
    /// </summary>
    /// <param name="file">ttf 文件</param>
    /// <returns>登记上了返回 true; 文件坏了或不是字体返回 false</returns>
    public static bool Register(FileInfo file)
    {
        lock (locker)
        {
            if (instance == null)
            {
                instance = new FontLoader();
                FontManager.Current.AddFontCollection(instance);
            }
            var name = Path.GetFileNameWithoutExtension(file.Name);
            if (instance.loaded.Contains(name))
                return true;
            try
            {
                using var stream = file.OpenRead();
                if (!instance.TryAddGlyphTypeface(stream, out var glyphTypeface))
                    return false;
                //文件名与字体内部的族名往往不一样 (OPPOSans R.ttf 里叫 OPPOSans), 再按文件名登记一遍
                var key = new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);
                instance.TryAddGlyphTypeface(name, key, glyphTypeface);
                instance.loaded.Add(name);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 这个名字登记上了没有
    /// </summary>
    public static bool IsRegistered(string name)
    {
        lock (locker)
            return instance != null && instance.loaded.Contains(name);
    }

    /// <summary>
    /// 按字体名取 FontFamily (要先 Register)
    /// </summary>
    public static FontFamily Family(string name) => new FontFamily(CollectionKey + "#" + name);
}
