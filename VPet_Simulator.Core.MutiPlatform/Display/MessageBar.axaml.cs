using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using VPet_Simulator.Unified.Services;
using Timer = System.Timers.Timer;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// MessageBar.xaml 的交互逻辑
/// </summary>
/// 跨平台: 原文复制自 VPet-Simulator.Core/Display/MessageBar.xaml.cs (IMassageBar 在 IMassageBar.cs 里). 差异:
/// Visibility→IsVisible, Panel.SetZIndex→ZIndex, Label.Content 照旧; 语音那段 Windows 版直接读 MediaElement 的时钟,
/// 这边问 Main.VoicePlayer (MOD 提供), "还剩两秒就能收"的判定在共享后端 VoiceRules 里; Show 里的调用要在 UI 线程,
/// Windows 版是调用方 Dispatcher.Invoke 包着, 这边 Say 跑在 Task.Run 里, 所以 Show 自己 Invoke 一次, 描述小字也在里面 new
/// (Avalonia 的控件只能在创建它的线程上用). 其余逐行相同
public partial class MessageBar : UserControl, IDisposable, IMassageBar
{
    public Control This => this;
    Main m;
    public MessageBar(Main m)
    {
        InitializeComponent();
        EndTimer.Elapsed += EndTimer_Elapsed;
        ShowTimer.Elapsed += ShowTimer_Elapsed;
        CloseTimer.Elapsed += CloseTimer_Elapsed;
        this.m = m;
    }

    private void CloseTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (Dispatcher.Invoke(() => Opacity) <= 0.05)
        {
            CloseTimer.Stop();
            Dispatcher.Invoke(() =>
            {
                Opacity = 1;
                this.IsVisible = false;
                MessageBoxContent.Children.Clear();
            });
            EndAction?.Invoke();
        }
        else
        {
            Dispatcher.Invoke(() => Opacity -= 0.02);
        }
    }

    List<char> outputtext = new List<char>();
    StringBuilder outputtextsample = new StringBuilder();
    private void ShowTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (outputtext.Count > 0)
        {
            // 处理2-3个字符，平衡效果和性能
            int batchSize = Math.Min(2, outputtext.Count);
            string textToAdd = string.Empty;

            for (int i = 0; i < batchSize; i++)
            {
                textToAdd += outputtext[0];
                outputtext.RemoveAt(0);
            }
            outputtextsample.Append(textToAdd);
            var snapshot = outputtextsample.ToString();
            Dispatcher.Post(() =>
            {
                TText.Text = snapshot;
            }, DispatcherPriority.Background);
        }
        else
        {
            //跨平台: 语音还剩两秒以上就先不收, 判定在共享后端里 (Windows 版直接读 MediaElement 的时钟)
            if (VoiceRules.ShouldHoldBubble(m.PlayingVoice, m.VoiceRemaining))
            {
                return;
            }
            ShowTimer.Stop();
            EndTimer.Start();
            if ((m.DisplayType.Name == graphName || m.DisplayType.Type == GraphInfo.GraphType.Say) && m.DisplayType.Animat != GraphInfo.AnimatType.C_End)
                m.DisplayCEndtoNomal(m.DisplayType.Name);
        }
    }
    /// <summary>
    /// 被关闭时事件
    /// </summary>
    public event Action? EndAction;
    private void EndTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (--timeleft <= 0)
        {
            EndTimer.Stop();
            CloseTimer.Start();
        }
    }

    public Timer EndTimer = new Timer() { Interval = 200 };
    public Timer ShowTimer = new Timer() { Interval = 150 };
    public Timer CloseTimer = new Timer() { Interval = 50 };
    int timeleft;
    string? graphName;
    /// <summary>
    /// 显示消息
    /// </summary>
    /// <param name="name">名字</param>
    /// <param name="text">内容</param>
    public void Show(string name, string text, string? graphName = null, Control? msgContent = null, string? desc = null)
    {
        Dispatcher.Invoke(() =>
        {
            if (m.UIGrid.Children.IndexOf(this) != m.UIGrid.Children.Count - 1)
            {
                ZIndex = m.UIGrid.Children.Count - 1;
            }
            MessageBoxContent.Children.Clear();
            TText.Text = "";
            outputtext = text.ToList();
            outputtextsample.Clear();
            LName.Content = name;
            timeleft = Function.ComCheck(text) * 10 + 20;
            ShowTimer.Start(); EndTimer.Stop(); CloseTimer.Stop();
            this.IsVisible = true;
            Opacity = .8;
            this.graphName = graphName;
            msgContent ??= BuildDescription(desc);
            if (msgContent != null)
            {
                MessageBoxContent.Children.Add(msgContent);
            }
        });
    }
    private SayInfoWithStream? oldsaystream;
    /// <summary>
    /// 流式传输模式 显示文字
    /// </summary>
    public void Show(string name, SayInfoWithStream sayInfoWithStream)
    {
        Dispatcher.Invoke(() =>
        {
            if (m.UIGrid.Children.IndexOf(this) != m.UIGrid.Children.Count - 1)
            {
                ZIndex = m.UIGrid.Children.Count - 1;
            }

            //解除之前说话绑定,取消之前的说话
            if (oldsaystream != null)
            {
                oldsaystream.Event_Update -= DealWithUpdate;
                oldsaystream.Event_Finish -= DealWithStreamFinish;
            }
            oldsaystream = sayInfoWithStream;

            MessageBoxContent.Children.Clear();
            TText.Text = "";
            LName.Content = name;
            ShowTimer.Stop();
            EndTimer.Stop();
            CloseTimer.Stop();
            this.IsVisible = true;
            Opacity = .8;
            graphName = sayInfoWithStream.GraphName;

            var msgcontent = sayInfoWithStream.MsgContent ?? BuildDescription(sayInfoWithStream.Desc);
            if (msgcontent != null)
            {
                MessageBoxContent.Children.Add(msgcontent);
            }



            TText.Text = sayInfoWithStream.CurrentText.ToString();
        });

        sayInfoWithStream.Event_Update += DealWithUpdate;
        if (sayInfoWithStream.IsFinishGen)
            DealWithStreamFinish(sayInfoWithStream.CurrentText.ToString());
        else
            sayInfoWithStream.Event_Finish += DealWithStreamFinish;
    }
    /// <summary>
    /// 把说话的描述做成右对齐的小字 (Windows 版是调用方现场 new 的那个 TextBlock)
    /// </summary>
    internal static Control? BuildDescription(string? desc)
    {
        if (string.IsNullOrWhiteSpace(desc))
            return null;
        var tb = new TextBlock()
        {
            Text = desc,
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        ToolTip.SetTip(tb, desc);
        return tb;
    }
    /// <summary>
    /// 流式传输用的阻断文字显示用的计时器
    /// </summary>
    DateTime nextshow = DateTime.Now;
    private readonly object nextshowLock = new();
    /// <summary>
    /// 增加显示新词
    /// </summary>
    /// <param name="data">更新内容</param>
    public void DealWithUpdate((string fullText, string changedText) data)
    {
        timeleft = data.fullText.Length;
        Task.Run(() =>
        {
            int sleeptime = 0;
            lock (nextshowLock)
            {
                var now = DateTime.Now;
                if (now < nextshow)
                {
                    sleeptime = (int)(nextshow - now).TotalMilliseconds;
                    nextshow = nextshow.AddMilliseconds(150);
                }
                else
                {
                    nextshow = now.AddMilliseconds(150);
                }
            }
            if (sleeptime > 0) //处理前等待
                Thread.Sleep(sleeptime);
            Dispatcher.Invoke(() => { TText.Text = data.fullText; });
        });

    }
    /// <summary>
    /// 处理流式传输结束
    /// </summary>
    public void DealWithStreamFinish(string fullText)
    {
        Task.Run(() =>
        {
            //跨平台: 等语音说到只剩两秒 (Windows 版读 MediaElement 的时钟, 这边问 Main.VoicePlayer)
            while (VoiceRules.ShouldHoldBubble(m.PlayingVoice, m.VoiceRemaining))
            {
                Thread.Sleep(100);
            }
            if (oldsaystream?.CurrentText.ToString() == fullText)
            {
                oldsaystream.Event_Update -= DealWithUpdate;
                oldsaystream.Event_Finish -= DealWithStreamFinish;
                oldsaystream = null;
            }

            timeleft = Function.ComCheck(fullText) * 5 + 10;
            EndTimer.Start();
            if ((m.DisplayType.Name == graphName || m.DisplayType.Type == GraphInfo.GraphType.Say) && m.DisplayType.Animat != GraphInfo.AnimatType.C_End)
                m.DisplayCEndtoNomal(m.DisplayType.Name);

        });
    }

    public void Border_MouseEnter(object? sender, PointerEventArgs? e)
    {
        EndTimer.Stop();
        CloseTimer.Stop();
        this.Opacity = .8;
    }

    public void Border_MouseLeave(object? sender, PointerEventArgs? e)
    {
        if (!ShowTimer.Enabled)
            EndTimer.Start();
    }

    private void UserControl_MouseDoubleClick(object? sender, TappedEventArgs e)
    {
        ForceClose();
    }
    /// <summary>
    /// 强制关闭
    /// </summary>
    public void ForceClose()
    {
        EndTimer.Stop(); ShowTimer.Stop(); CloseTimer.Close();
        Dispatcher.Invoke(() =>
        {
            this.IsVisible = false;
            MessageBoxContent.Children.Clear();
        });
        if ((m.DisplayType.Name == graphName || m.DisplayType.Type == GraphInfo.GraphType.Say) && m.DisplayType.Animat != GraphInfo.AnimatType.C_End)
            m.DisplayCEndtoNomal(m.DisplayType.Name);
        EndAction?.Invoke();
    }
    public void Dispose()
    {
        EndTimer.Dispose();
        ShowTimer.Dispose();
        CloseTimer.Dispose();
    }
    public void SetPlaceIN()
    {
        this.Height = 500;
        BorderMain.VerticalAlignment = VerticalAlignment.Bottom;
        Margin = new Thickness(0);
    }
    public void SetPlaceOUT()
    {
        this.Height = double.NaN;
        BorderMain.VerticalAlignment = VerticalAlignment.Top;
        Margin = new Thickness(0, 500, 0, 0);
    }

    private void MenuItemCopy_Click(object? sender, RoutedEventArgs e)
    {
        //跨平台: 剪贴板挂在窗口上, 是异步的
        _ = TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(TText.Text ?? "");
    }

    private void MenuItemClose_Click(object? sender, RoutedEventArgs e)
    {
        ForceClose();
    }

    private void ContextMenu_Opened(object? sender, RoutedEventArgs e) => Border_MouseEnter(null, null);

    private void ContextMenu_Closed(object? sender, RoutedEventArgs e) => Border_MouseLeave(null, null);

    private void TText_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        sv.ScrollToEnd();
    }
}
