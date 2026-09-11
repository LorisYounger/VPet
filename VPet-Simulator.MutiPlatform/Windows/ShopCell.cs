using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;

namespace VPet_Simulator.MutiPlatform.Windows;

/// <summary>
/// 商店和背包里那个格子
/// </summary>
/// 单独抽出来是为了让渲染探针能画**真的**格子: 探针里另写一份样子差不多的,
/// 那它就只是在检查探针自己, 改了真格子也看不出来。
///
/// 所以这里只收纯数据, 不碰存档也不碰宿主。
internal static class ShopCell
{
    /// <summary>
    /// 格子的尺寸
    /// </summary>
    /// 等高是有意的: 不定高的话每张卡片按内容各长各的, 一排按钮参差不齐
    public const double CellWidth = 150;
    public const double CellHeight = 210;

    /// <summary>
    /// 造一个格子
    /// </summary>
    /// <param name="title">第一行(名字, 背包里还带数量)</param>
    /// <param name="subtitle">第二行(价格), 不需要就传 null</param>
    /// <param name="warning">警告行(超模), 不需要就传 null</param>
    /// <param name="detail">底下那段小字</param>
    /// <param name="image">图, 没有就传 null</param>
    /// <param name="actionText">按钮文字, null 表示不要按钮</param>
    /// <param name="action">按钮点下去干什么</param>
    public static Border Build(string title, string? subtitle, string? warning,
        string detail, Bitmap? image, string? actionText, Action? action)
    {
        var cell = new Border
        {
            Width = CellWidth,
            Height = CellHeight,
            Margin = new Thickness(4),
        };
        cell.Classes.Add("vpet-card");

        var panel = new DockPanel { LastChildFill = true };
        var top = new StackPanel { Spacing = 4 };
        DockPanel.SetDock(top, Dock.Top);
        panel.Children.Add(top);

        if (image != null)
            top.Children.Add(new Image { Source = image, Height = 56, Stretch = Stretch.Uniform });

        top.Children.Add(new TextBlock
        {
            Text = title,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        if (subtitle != null)
            top.Children.Add(new TextBlock
            {
                Text = subtitle,
                HorizontalAlignment = HorizontalAlignment.Center,
            });

        if (warning != null)
            top.Children.Add(new TextBlock
            {
                Text = warning,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.OrangeRed,
                FontSize = 12,
            });

        var hint = new TextBlock { Text = detail, FontSize = 12, TextWrapping = TextWrapping.Wrap };
        hint.Classes.Add("vpet-hint");
        top.Children.Add(hint);

        if (actionText != null && action != null)
        {
            var button = VPetWindow.PrimaryButton(actionText, action);
            button.VerticalAlignment = VerticalAlignment.Bottom;
            panel.Children.Add(button);
        }

        cell.Child = panel;
        return cell;
    }

    /// <summary>
    /// 把一串数值拼成给玩家看的小字
    /// </summary>
    /// 只列不为零的项 —— 一份饮料把"饱腹度"也列出来只是噪音, 与 Windows 版一致
    public static string Describe(IEnumerable<(string Name, double Value)> values)
    {
        var parts = new List<string>();
        foreach (var (name, value) in values)
        {
            if (value != 0)
                parts.Add($"{name} {(value > 0 ? "+" : "")}{value:f0}");
        }
        return string.Join("  ", parts);
    }
}
