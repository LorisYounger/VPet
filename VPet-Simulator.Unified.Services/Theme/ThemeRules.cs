using LinePutScript;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 主题配色的解析与派生
/// </summary>
/// 主题 lps 里只写基础色, 界面上真正用到的还有一批半透明变体, 是按固定的透明度
/// 派生出来的. 两端各派生一遍必然会漏 —— 漏一个键的表现是某个控件底色变成透明,
/// 而不是报错。
///
/// 只出数值, 不出画刷: WPF 的 SolidColorBrush 和 Avalonia 的是两个类型, 各自宿主
/// 拿这里给的 ARGB 自己造。
public static class ThemeRules
{
    /// <summary>
    /// 阴影颜色的键名
    /// </summary>
    /// 它是唯一一个要当颜色而不是画刷用的
    public const string ShadowColorKey = "ShadowColor";

    /// <summary>
    /// 要派生半透明变体的基础色
    /// </summary>
    public static readonly IReadOnlyList<string> DerivedBases = new[]
    {
        "Primary", "Secondary", "DARKPrimary",
    };

    /// <summary>
    /// 派生出来的后缀与对应的透明度
    /// </summary>
    /// 数值取自 Windows 版 MainWindow.LoadTheme, 一个都不能改 —— MOD 主题是按
    /// 这批值调出来的
    public static readonly IReadOnlyList<(string Suffix, byte Alpha)> DerivedAlphas = new[]
    {
        ("Trans", (byte)204),
        ("Trans4", (byte)44),
        ("TransA", (byte)170),
        ("TransE", (byte)238),
    };

    /// <summary>
    /// 一条要塞进资源字典的配色
    /// </summary>
    public readonly struct ThemeColor
    {
        /// <summary>资源键名</summary>
        public string Key { get; }
        public byte A { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        /// <summary>是不是"当颜色用"而不是"当画刷用"</summary>
        public bool IsRawColor { get; }

        public ThemeColor(string key, byte a, byte r, byte g, byte b, bool isRawColor)
        {
            Key = key;
            A = a;
            R = r;
            G = g;
            B = b;
            IsRawColor = isRawColor;
        }
    }

    /// <summary>
    /// 把主题 lps 展开成一串要塞进资源字典的配色
    /// </summary>
    /// <param name="themeColor">主题的配色部分(已经去掉了首行)</param>
    /// 顺序与 Windows 版一致: 先阴影色, 再所有名字里不含 "Color" 的基础色,
    /// 最后是派生出来的半透明变体。
    public static IEnumerable<ThemeColor> Expand(ILPS themeColor)
    {
        var shadow = themeColor.FindLine(ShadowColorKey)?.Info
            ?? themeColor.FindSub(ShadowColorKey)?.Info;
        if (shadow != null && TryParse(shadow, out var sc))
            yield return new ThemeColor(ShadowColorKey, sc.A, sc.R, sc.G, sc.B, true);

        foreach (var line in themeColor)
        {
            // 名字里带 Color 的是"当颜色用"的, 上面单独处理过了
            if (line.Name.Contains("Color"))
                continue;
            if (!TryParse(line.Info, out var c))
                continue;
            yield return new ThemeColor(line.Name, c.A, c.R, c.G, c.B, false);
        }

        foreach (var name in DerivedBases)
        {
            var baseColor = themeColor.FindLine(name)?.Info;
            if (baseColor == null || !TryParse(baseColor, out var b))
                continue;
            foreach (var (suffix, alpha) in DerivedAlphas)
                yield return new ThemeColor(name + suffix, alpha, b.R, b.G, b.B, false);
        }
    }

    /// <summary>
    /// 解析主题里写的颜色
    /// </summary>
    /// <param name="text">六位或八位十六进制, 可带也可不带 #</param>
    /// 主题 lps 里写的是不带 # 的六位十六进制, Windows 版是拼上 # 之后交给
    /// ColorConverter 的. 这里自己解析, 免得依赖某个平台的转换器。
    public static bool TryParse(string? text, out (byte A, byte R, byte G, byte B) color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var hex = text.Trim().TrimStart('#');
        if (hex.Length == 6)
        {
            if (!TryByte(hex, 0, out var r) || !TryByte(hex, 2, out var g) || !TryByte(hex, 4, out var b))
                return false;
            color = (255, r, g, b);
            return true;
        }
        if (hex.Length == 8)
        {
            if (!TryByte(hex, 0, out var a) || !TryByte(hex, 2, out var r)
                || !TryByte(hex, 4, out var g) || !TryByte(hex, 6, out var b))
                return false;
            color = (a, r, g, b);
            return true;
        }
        return false;
    }

    private static bool TryByte(string hex, int index, out byte value)
        => byte.TryParse(hex.AsSpan(index, 2), System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out value);
}
