using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LinePutScript.Localization;
using System;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 消息框按钮组合, 对应 WPF 的 MessageBoxButton
/// </summary>
public enum MessageBoxButton
{
    OK,
    OKCancel,
    YesNoCancel,
    YesNo,
}

/// <summary>
/// 消息框返回值, 对应 WPF 的 MessageBoxResult
/// </summary>
public enum MessageBoxResult
{
    None,
    OK,
    Cancel,
    Yes,
    No,
}

/// <summary>
/// 消息框图标, 对应 Panuon 的 MessageBoxIcon
/// </summary>
public enum MessageBoxIcon
{
    None,
    Info,
    Warning,
    Error,
    Question,
    Success,
}

/// <summary>
/// 消息框
/// </summary>
/// 对应 Panuon 的 MessageBoxX: 同名同参数, Windows 版的 MessageBoxX.Show(...) 调用可以原样保留.
/// 与它一样是**同步**的 —— 弹完接着往下走, 靠 VPetWindow.ShowDialog() 里的 DispatcherFrame.
/// 从非 UI 线程调用时自动切到 UI 线程等结果.
public static class MessageBoxX
{
    public static MessageBoxResult Show(string message)
        => Show(null, message, string.Empty, MessageBoxButton.OK, MessageBoxIcon.None);

    public static MessageBoxResult Show(string message, string caption)
        => Show(null, message, caption, MessageBoxButton.OK, MessageBoxIcon.None);

    public static MessageBoxResult Show(string message, string caption, MessageBoxIcon icon)
        => Show(null, message, caption, MessageBoxButton.OK, icon);

    public static MessageBoxResult Show(string message, string caption, MessageBoxButton button)
        => Show(null, message, caption, button, MessageBoxIcon.None);

    public static MessageBoxResult Show(string message, string caption, MessageBoxButton button, MessageBoxIcon icon)
        => Show(null, message, caption, button, icon);

    public static MessageBoxResult Show(Window? owner, string message)
        => Show(owner, message, string.Empty, MessageBoxButton.OK, MessageBoxIcon.None);

    public static MessageBoxResult Show(Window? owner, string message, string caption)
        => Show(owner, message, caption, MessageBoxButton.OK, MessageBoxIcon.None);

    public static MessageBoxResult Show(Window? owner, string message, string caption, MessageBoxButton button)
        => Show(owner, message, caption, button, MessageBoxIcon.None);

    public static MessageBoxResult Show(Window? owner, string message, string caption, MessageBoxButton button, MessageBoxIcon icon)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.Invoke(() => Show(owner, message, caption, button, icon));

        var window = new VPetWindow
        {
            Title = caption,
            Topmost = true,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = (owner ?? DialogService.DefaultOwner) != null
                ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            MinWidth = 300,
        };
        var result = MessageBoxResult.None;
        var text = new TextBlock
        {
            //Windows 版的文案里有用 \r 换行的, Avalonia 的 TextBlock 不认单独的回车
            Text = Localization.StrExtension.Normalize(message),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 460,
            Margin = new Thickness(20, 15, 20, 15),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var content = new DockPanel { Margin = new Thickness(10) };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 5,
            Margin = new Thickness(0, 5, 0, 0),
        };
        DockPanel.SetDock(buttons, Dock.Bottom);
        foreach (var (label, value) in Buttons(button))
        {
            var b = new Button { Content = label, MinWidth = 75, Height = 30, FontSize = 12 };
            b.Bind(StyledElement.ThemeProperty, b.GetResourceObservable("ThemedButtonStyle"));
            var captured = value;
            b.Click += (_, _) => { result = captured; window.Close(); };
            buttons.Children.Add(b);
        }
        content.Children.Add(buttons);
        var body = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (IconText(icon) is { } glyph)
            body.Children.Add(new TextBlock
            {
                Text = glyph,
                FontSize = 28,
                Margin = new Thickness(15, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
        body.Children.Add(text);
        content.Children.Add(body);
        window.Content = content;
        window.ShowDialog(owner);
        //直接关窗: 有"取消"就算取消, 否则算最后一个按钮 (与 Panuon 一致)
        if (result == MessageBoxResult.None)
            result = button switch
            {
                MessageBoxButton.OK => MessageBoxResult.OK,
                MessageBoxButton.YesNo => MessageBoxResult.No,
                _ => MessageBoxResult.Cancel,
            };
        return result;
    }

    private static (string, MessageBoxResult)[] Buttons(MessageBoxButton button) => button switch
    {
        MessageBoxButton.OKCancel => new[] { ("确定".Translate(), MessageBoxResult.OK), ("取消".Translate(), MessageBoxResult.Cancel) },
        MessageBoxButton.YesNo => new[] { ("是".Translate(), MessageBoxResult.Yes), ("否".Translate(), MessageBoxResult.No) },
        MessageBoxButton.YesNoCancel => new[] { ("是".Translate(), MessageBoxResult.Yes), ("否".Translate(), MessageBoxResult.No), ("取消".Translate(), MessageBoxResult.Cancel) },
        _ => new[] { ("确定".Translate(), MessageBoxResult.OK) },
    };

    /// <summary>
    /// 图标用字符画: Panuon 用的是 remixicon 字体里的图标, 这里不引字体, 用 Unicode 符号
    /// </summary>
    private static string? IconText(MessageBoxIcon icon) => icon switch
    {
        MessageBoxIcon.Info => "ℹ",
        MessageBoxIcon.Warning => "⚠",
        MessageBoxIcon.Error => "✖",
        MessageBoxIcon.Question => "?",
        _ => null,
    };
}

/// <summary>
/// 不打断玩家的提示框
/// </summary>
/// 对应 Panuon 的 NoticeBox: 弹在角落, 几秒后自己走
public static class NoticeBox
{
    public static void Show(string message, string caption = "", int duration = 5000)
        => DialogService.Notice(message, caption, duration);
}

/// <summary>
/// "请稍候"框
/// </summary>
/// 对应 Panuon 的 PendingBox: Show 返回句柄, 干完活调 Close
public static class PendingBox
{
    public sealed class Handle
    {
        private readonly VPetWindow window;
        internal Handle(VPetWindow window) => this.window = window;
        public void Close() => Dispatcher.UIThread.Post(window.Close);
    }

    public static Handle Show(string message, string caption = "")
    {
        return Dispatcher.UIThread.Invoke(() =>
        {
            var window = new VPetWindow
            {
                Title = caption,
                Topmost = true,
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                CaptionButtons = VPetWindow.CaptionButtonsType.None,
            };
            var panel = new StackPanel { Spacing = 12, Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Center };
            panel.Children.Add(new TextBlock { Text = Localization.StrExtension.Normalize(message), TextWrapping = TextWrapping.Wrap, MaxWidth = 400 });
            panel.Children.Add(new ProgressBar { IsIndeterminate = true, MinWidth = 280 });
            window.Content = panel;
            if (DialogService.DefaultOwner != null)
                window.Show(DialogService.DefaultOwner);
            else
                window.Show();
            return new Handle(window);
        });
    }
}
