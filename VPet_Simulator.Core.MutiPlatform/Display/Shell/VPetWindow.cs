using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 桌宠各功能窗口的外壳
/// </summary>
/// 对应 Windows 版那些继承 Panuon 的 WindowX 的窗口. 统一在这里做的事只有三样:
/// 套上主题样式、给一个标题栏、关掉时通知宿主.
///
/// 刻意做得很薄: 每个功能窗口的内容差别很大, 外壳管多了反而处处要开口子.
public class VPetWindow : Window
{
    private readonly DockPanel root = new DockPanel();
    private readonly TextBlock titleText = new TextBlock();
    private readonly ContentControl body = new ContentControl();

    /// <summary>
    /// 窗口内容
    /// </summary>
    public object? Body
    {
        get => body.Content;
        set => body.Content = value;
    }

    public VPetWindow()
    {
        Classes.Add("vpet");
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;

        var titleBar = new Border { Padding = new Thickness(12, 8) };
        titleBar.Classes.Add("vpet-title");
        titleBar.Child = titleText;
        DockPanel.SetDock(titleBar, Dock.Top);

        body.Margin = new Thickness(12);
        root.Children.Add(titleBar);
        root.Children.Add(body);
        Content = root;

        // 标题栏跟着窗口标题走, 免得两处各设一遍
        titleText.Text = Title;
        PropertyChanged += (_, e) =>
        {
            if (e.Property == TitleProperty)
                titleText.Text = Title;
        };
    }

    /// <summary>
    /// 造一排靠右的按钮
    /// </summary>
    /// 对话框和功能窗口的底部按钮排法是一样的, 放这儿省得每处抄一遍
    public static StackPanel ButtonRow(params Control[] buttons)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 12, 0, 0),
        };
        foreach (var button in buttons)
            panel.Children.Add(button);
        return panel;
    }

    /// <summary>
    /// 造一个主按钮
    /// </summary>
    public static Button PrimaryButton(string text, Action click)
    {
        var button = new Button { Content = text };
        button.Classes.Add("vpet");
        button.Click += (_, _) => click();
        return button;
    }

    /// <summary>
    /// 造一个次要按钮
    /// </summary>
    public static Button SecondaryButton(string text, Action click)
    {
        var button = new Button { Content = text };
        button.Classes.Add("vpet-secondary");
        button.Click += (_, _) => click();
        return button;
    }

    /// <summary>
    /// 造一段正文
    /// </summary>
    public static TextBlock BodyText(string text)
    {
        var block = new TextBlock { Text = text, MaxWidth = 420 };
        block.Classes.Add("vpet-body");
        return block;
    }
}
