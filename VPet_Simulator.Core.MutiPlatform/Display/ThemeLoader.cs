using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 把主题配色塞进 Avalonia 的资源字典
/// </summary>
/// 对应 Windows 版 MainWindow.LoadTheme 的后半段. 配色怎么展开(哪些当颜色、
/// 哪些当画刷、四种半透明变体各多少)在共享后端里, 这边只负责造 Avalonia 的类型。
public static class ThemeLoader
{
    /// <summary>
    /// 应用一套主题
    /// </summary>
    /// <param name="theme">主题</param>
    /// <param name="resources">要写进去的资源字典, 一般是 Application.Current.Resources</param>
    /// <returns>实际写进去了几个键</returns>
    /// 必须在 UI 线程上调用: 资源字典的变更会立刻触发重新绑定。
    public static int Apply(ThemeInfo theme, IResourceDictionary resources)
    {
        int count = 0;
        foreach (var color in ThemeRules.Expand(theme.ThemeColor))
        {
            var c = Color.FromArgb(color.A, color.R, color.G, color.B);
            resources[color.Key] = color.IsRawColor ? c : (object)new SolidColorBrush(c);
            count++;
        }
        return count;
    }

    /// <summary>
    /// 应用当前应用的资源字典
    /// </summary>
    public static int Apply(ThemeInfo theme)
    {
        var app = Application.Current;
        if (app == null)
            return 0;
        return Apply(theme, app.Resources);
    }

    /// <summary>
    /// 主题里应当提供的键
    /// </summary>
    /// 用来在启动时核对一遍: 缺键的表现是某个控件底色透明, 而不是报错, 很难查。
    /// 名单取自 Windows 版 App.xaml 里定义的那批。
    public static readonly IReadOnlyList<string> ExpectedKeys = new[]
    {
        "Primary", "PrimaryTrans", "PrimaryTrans4", "PrimaryTransA", "PrimaryTransE",
        "PrimaryText", "PrimaryDark", "PrimaryLight",
        "Secondary", "SecondaryTrans", "SecondaryTrans4", "SecondaryTransA", "SecondaryTransE",
        "SecondaryText", "SecondaryDark", "SecondaryLight",
        "DARKPrimary", "DARKPrimaryTrans", "DARKPrimaryTrans4", "DARKPrimaryTransA", "DARKPrimaryTransE",
        "DARKPrimaryText",
        "ShadowColor",
    };

    /// <summary>
    /// 找出主题没提供的键
    /// </summary>
    public static List<string> MissingKeys(ThemeInfo theme)
    {
        var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var color in ThemeRules.Expand(theme.ThemeColor))
            have.Add(color.Key);
        var missing = new List<string>();
        foreach (var key in ExpectedKeys)
            if (!have.Contains(key))
                missing.Add(key);
        return missing;
    }
}
