using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using LinePutScript.Localization;
using System;
using System.Threading.Tasks;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 对话框
/// </summary>
/// 对应 Windows 版散在各处的 MessageBoxX / NoticeBox / PendingBox / InputBox.
/// 收成一处的理由不只是省代码: 那几个的线程要求各不相同, 分散着写就一定会有人
/// 从计时器线程上弹窗, 表现是整个界面卡死.
///
/// 所以这里每个方法都自己切到 UI 线程, 调用方从哪个线程来都行.
public static class DialogService
{
    /// <summary>
    /// 没有指定属主时挂到这个窗口上
    /// </summary>
    /// 由宿主在主窗口建好之后设一次. 没设也能用, 只是对话框不会跟着主窗口居中.
    public static Window? DefaultOwner { get; set; }

    /// <summary>
    /// 弹一条消息, 等玩家点掉
    /// </summary>
    public static Task ShowAsync(string text, string title = "", Window? owner = null)
        => RunOnUIAsync(() => ShowCore(title, text, owner,
            new[] { LocalizeCore.Translate("确定") }, 0).ContinueWith(_ => { }));

    /// <summary>
    /// 问一个是非题
    /// </summary>
    /// <returns>玩家点了"是"返回 true</returns>
    public static Task<bool> ConfirmAsync(string text, string title = "", Window? owner = null)
        => RunOnUIAsync(async () =>
        {
            var index = await ShowCore(title, text, owner,
                new[] { LocalizeCore.Translate("是"), LocalizeCore.Translate("否") }, 1);
            return index == 0;
        });

    /// <summary>
    /// 弹一个输入框
    /// </summary>
    /// <returns>玩家取消时返回 null</returns>
    public static Task<string?> InputAsync(string text, string defaultText = "",
        string title = "", bool allowMultiLine = false, Window? owner = null)
        => RunOnUIAsync(async () =>
        {
            var window = new VPetWindow { Title = string.IsNullOrEmpty(title) ? LocalizeCore.Translate("请输入") : title };
            var box = new TextBox
            {
                Text = defaultText,
                AcceptsReturn = allowMultiLine,
                MinWidth = 320,
                Height = allowMultiLine ? 120 : double.NaN,
                TextWrapping = allowMultiLine ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap,
            };
            string? result = null;
            var panel = new StackPanel { Spacing = 8 };
            if (!string.IsNullOrEmpty(text))
                panel.Children.Add(VPetWindow.BodyText(text));
            panel.Children.Add(box);
            panel.Children.Add(VPetWindow.ButtonRow(
                VPetWindow.SecondaryButton(LocalizeCore.Translate("取消"), () => window.Close()),
                VPetWindow.PrimaryButton(LocalizeCore.Translate("确定"), () =>
                {
                    result = box.Text ?? string.Empty;
                    window.Close();
                })));
            window.Body = panel;
            box.Focus();
            await ShowDialogOrWindow(window, owner);
            return result;
        });

    /// <summary>
    /// 弹一条不用玩家点的提示
    /// </summary>
    /// <param name="milliseconds">多久之后自动收起</param>
    /// 对应 Windows 版的 NoticeBox: 它是"说一声就走"的, 不该打断玩家手上的事
    public static void Notice(string text, string title = "", int milliseconds = 5000, Window? owner = null)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            var window = new VPetWindow { Title = string.IsNullOrEmpty(title) ? LocalizeCore.Translate("提示") : title };
            window.Body = VPetWindow.BodyText(text);
            var closed = false;
            window.Closed += (_, _) => closed = true;
            ShowNonModal(window, owner);
            await Task.Delay(milliseconds);
            if (!closed)
                window.Close();
        });
    }

    /// <summary>
    /// 做一件要花点时间的事, 期间挡住界面
    /// </summary>
    /// <param name="text">给玩家看的说明</param>
    /// <param name="work">真正要做的事, 在后台线程上跑</param>
    /// 对应 Windows 版的 PendingBox. work 刻意跑在后台: 在 UI 线程上做耗时的事
    /// 只会让"请稍候"这四个字也画不出来.
    public static async Task PendingAsync(string text, Func<Task> work, string title = "", Window? owner = null)
    {
        var window = await RunOnUI(() =>
        {
            var w = new VPetWindow { Title = string.IsNullOrEmpty(title) ? LocalizeCore.Translate("请稍候") : title };
            var panel = new StackPanel { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };
            panel.Children.Add(VPetWindow.BodyText(text));
            panel.Children.Add(new ProgressBar { IsIndeterminate = true, MinWidth = 280 });
            w.Body = panel;
            ShowNonModal(w, owner);
            return w;
        });
        try
        {
            await Task.Run(work);
        }
        finally
        {
            await RunOnUI(() => { window.Close(); return 0; });
        }
    }

    /// <summary>
    /// 对话框的公共部分
    /// </summary>
    /// <returns>玩家点了第几个按钮; 直接关窗算最后一个(通常是"否"/"取消")</returns>
    private static async Task<int> ShowCore(string title, string text, Window? owner,
        string[] buttons, int closeResult)
    {
        var window = new VPetWindow { Title = string.IsNullOrEmpty(title) ? LocalizeCore.Translate("提示") : title };
        int result = closeResult;
        var row = new Control[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            row[i] = i == 0
                ? VPetWindow.PrimaryButton(buttons[i], () => { result = index; window.Close(); })
                : VPetWindow.SecondaryButton(buttons[i], () => { result = index; window.Close(); });
        }
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(VPetWindow.BodyText(text));
        panel.Children.Add(VPetWindow.ButtonRow(row));
        window.Body = panel;
        await ShowDialogOrWindow(window, owner);
        return result;
    }

    /// <summary>
    /// 有属主就开模态, 没有就开普通窗口
    /// </summary>
    /// ShowDialog 没有属主会抛异常, 而桌宠启动早期(还没建主窗口)就可能要弹东西
    private static Task ShowDialogOrWindow(Window window, Window? owner)
    {
        var host = owner ?? DefaultOwner;
        if (host != null)
            return window.ShowDialog(host);

        var source = new TaskCompletionSource();
        window.Closed += (_, _) => source.TrySetResult();
        window.Show();
        return source.Task;
    }

    private static void ShowNonModal(Window window, Window? owner)
    {
        var host = owner ?? DefaultOwner;
        if (host != null)
            window.Show(host);
        else
            window.Show();
    }

    /// <summary>
    /// 切到 UI 线程执行
    /// </summary>
    /// 同步的活儿
    private static async Task<T> RunOnUI<T>(Func<T> func)
        => Dispatcher.UIThread.CheckAccess()
            ? func()
            : await Dispatcher.UIThread.InvokeAsync(func);

    /// 异步的活儿. 名字与上面区分开: Func<Task<T>> 也能匹配 Func<T>, 重载会撞车;
    /// Avalonia 的 InvokeAsync 自己认得 Func<Task<T>>, 所以这里只要一层 await
    private static async Task<T> RunOnUIAsync<T>(Func<Task<T>> func)
        => Dispatcher.UIThread.CheckAccess()
            ? await func()
            : await Dispatcher.UIThread.InvokeAsync(func);

    private static async Task RunOnUIAsync(Func<Task> func)
    {
        if (Dispatcher.UIThread.CheckAccess())
            await func();
        else
            await Dispatcher.UIThread.InvokeAsync(func);
    }
}
