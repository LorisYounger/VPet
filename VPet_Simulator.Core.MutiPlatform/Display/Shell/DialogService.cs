using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 对话框的公共部分
/// </summary>
/// MessageBoxX / NoticeBox / PendingBox 各自对应 Panuon 的同名类 (见 MessageBoxX.cs), 这里只放它们
/// 共用的两样: 默认属主, 和"说一声就走"的提示框本体.
public static class DialogService
{
    /// <summary>
    /// 没有指定属主时挂到这个窗口上
    /// </summary>
    /// 由宿主在主窗口建好之后设一次. 没设也能用, 只是对话框不会跟着主窗口居中.
    public static Window? DefaultOwner { get; set; }

    /// <summary>
    /// 弹一条不用玩家点的提示
    /// </summary>
    /// <param name="milliseconds">多久之后自动收起</param>
    /// 对应 Panuon 的 NoticeBox: 它是"说一声就走"的, 不该打断玩家手上的事
    public static void Notice(string text, string title = "", int milliseconds = 5000, Window? owner = null)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            var window = new VPetWindow
            {
                Title = title,
                Topmost = true,
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                CaptionButtons = VPetWindow.CaptionButtonsType.Close,
            };
            window.Content = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 420,
                Margin = new Thickness(20, 15),
            };
            var closed = false;
            window.Closed += (_, _) => closed = true;
            var host = owner ?? DefaultOwner;
            if (host != null)
                window.Show(host);
            else
                window.Show();
            await Task.Delay(milliseconds);
            if (!closed)
                window.Close();
        });
    }
}
