using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 消息栏
/// </summary>
/// 对应 Windows 版的 VPet-Simulator.Core/Display/MessageBar.xaml.cs.
/// 三个计时器的分工与 Windows 版一致:
///   ShowTimer  逐字吐字(每 150 毫秒吐两个字)
///   EndTimer   吐完之后按文本长度停留一段时间
///   CloseTimer 停留结束后逐步降低不透明度直到隐藏
///
/// 与 Windows 版的差异: 语音相关的等待逻辑没有移植 —— 跨平台侧还没有语音播放器.
/// 那部分在 Windows 版里是"说完话要等语音播完再收起消息框", 等语音接上再补.
public partial class MessageBar : UserControl, IMassageBar
{
    private readonly PetMain m;

    /// <summary>
    /// 无参构造仅供 Avalonia 设计器使用
    /// </summary>
    public MessageBar() : this(null!)
    {
    }

    /// <summary>
    /// 该消息框的Control
    /// </summary>
    public Control This => this;

    public MessageBar(PetMain m)
    {
        InitializeComponent();
        EndTimer.Elapsed += EndTimer_Elapsed;
        ShowTimer.Elapsed += ShowTimer_Elapsed;
        CloseTimer.Elapsed += CloseTimer_Elapsed;
        this.m = m;
        IsVisible = false;
    }

    public Timer EndTimer = new Timer() { Interval = 200 };
    public Timer ShowTimer = new Timer() { Interval = 150 };
    public Timer CloseTimer = new Timer() { Interval = 50 };

    private int timeleft;
    private string? graphName;

    /// <summary>
    /// 待吐出的字
    /// </summary>
    private List<char> outputtext = new List<char>();
    private readonly StringBuilder outputtextsample = new StringBuilder();

    /// <summary>
    /// 被关闭时事件
    /// </summary>
    public event Action? EndAction;

    /// <summary>
    /// 显示消息
    /// </summary>
    /// <param name="name">名字</param>
    /// <param name="text">内容</param>
    /// <param name="graphName">图像名</param>
    /// <param name="msgContent">消息框内容</param>
    /// <param name="desc">描述, 没有 msgContent 时用它生成一段右对齐的小字</param>
    /// desc 是字符串而不是现成的控件, 是有原因的: Avalonia 的控件有线程亲和性,
    /// 谁 new 出来的就只能在谁的线程上用, 而 Say 整个是在 Task.Run 里跑的.
    /// 描述控件必须在这个 Invoke 里面 new, 在外面 new 好传进来的话, 下一次排版
    /// 就会在 UI 线程上抛"调用线程无法访问此对象".
    public void Show(string name, string text, string? graphName = null, Control? msgContent = null,
        string? desc = null)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            MessageBoxContent.Children.Clear();
            TText.Text = "";
            outputtext = text.ToList();
            outputtextsample.Clear();
            LName.Text = name;
            timeleft = Function.ComCheck(text) * 10 + 20;
            ShowTimer.Start(); EndTimer.Stop(); CloseTimer.Stop();
            IsVisible = true;
            Opacity = .8;
            this.graphName = graphName;
            var content = msgContent ?? BuildDescription(desc);
            if (content != null)
            {
                MessageBoxContent.Children.Add(content);
            }
        });
    }

    private SayInfoWithStream? oldsaystream;

    /// <summary>
    /// 流式传输模式 显示文字
    /// </summary>
    public void Show(string name, SayInfoWithStream sayInfoWithStream)
    {
        //解除之前说话绑定,取消之前的说话
        if (oldsaystream != null)
        {
            oldsaystream.Event_Update -= DealWithUpdate;
            oldsaystream.Event_Finish -= DealWithStreamFinish;
        }
        oldsaystream = sayInfoWithStream;

        Dispatcher.UIThread.Invoke(() =>
        {
            MessageBoxContent.Children.Clear();
            TText.Text = "";
            LName.Text = name;
            ShowTimer.Stop();
            EndTimer.Stop();
            CloseTimer.Stop();
            IsVisible = true;
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
    /// 把说话的描述做成右对齐的小字
    /// </summary>
    internal static Control? BuildDescription(string? desc)
    {
        if (string.IsNullOrWhiteSpace(desc))
            return null;
        return new TextBlock
        {
            Text = desc,
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
    }

    private void DealWithUpdate((string fullText, string changedText) value)
    {
        Dispatcher.UIThread.Post(() => TText.Text = value.fullText);
    }

    private void DealWithStreamFinish(string text)
    {
        Dispatcher.UIThread.Post(() => TText.Text = text);
        timeleft = Function.ComCheck(text) * 10 + 20;
        EndTimer.Start();
    }

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
            Dispatcher.UIThread.Post(() => TText.Text = snapshot, DispatcherPriority.Background);
        }
        else
        {
            //字打完了但语音还没说完, 气泡再挂一会儿 —— 与 Windows 版同一个两秒阈值
            if (VoiceRules.ShouldHoldBubble(m.PlayingVoice, m.VoiceRemaining))
                return;
            ShowTimer.Stop();
            EndTimer.Start();
            if ((m.DisplayType?.Name == graphName || m.DisplayType?.Type == GraphInfo.GraphType.Say)
                && m.DisplayType?.Animat != GraphInfo.AnimatType.C_End)
                m.DisplayCEndtoNomal(m.DisplayType?.Name);
        }
    }

    private void EndTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        //语音还没说完就先不倒计时: 每次心跳现问一遍, 这样语音比预计长也不会被截断
        //(Windows 版是在那儿 while + Sleep 死等, 那会占住线程)
        if (VoiceRules.ShouldHoldBubble(m.PlayingVoice, m.VoiceRemaining))
            return;
        if (--timeleft <= 0)
        {
            EndTimer.Stop();
            CloseTimer.Start();
        }
    }

    private void CloseTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (Dispatcher.UIThread.Invoke(() => Opacity) <= 0.05)
        {
            CloseTimer.Stop();
            Dispatcher.UIThread.Invoke(() =>
            {
                Opacity = 1;
                IsVisible = false;
                MessageBoxContent.Children.Clear();
            });
            EndAction?.Invoke();
        }
        else
        {
            Dispatcher.UIThread.Invoke(() => Opacity -= 0.02);
        }
    }

    /// <summary>
    /// 强制关闭
    /// </summary>
    /// <summary>
    /// 鼠标移进来时把淡出计时停掉, 让人有时间读完
    /// </summary>
    /// 对应 Windows 版 MessageBar.Border_MouseEnter/Leave. 少了这个, 气泡会在
    /// 鼠标底下自己淡掉.
    private void BorderMain_PointerEntered(object? sender, PointerEventArgs e)
    {
        EndTimer.Stop();
        CloseTimer.Stop();
        Opacity = .8;
    }

    private void BorderMain_PointerExited(object? sender, PointerEventArgs e)
    {
        if (!ShowTimer.Enabled)
            EndTimer.Start();
    }

    /// <summary>
    /// 设置位置在桌宠内
    /// </summary>
    public void SetPlaceIN()
    {
        Height = 500;
        BorderMain.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
        Margin = new Thickness(0);
    }

    /// <summary>
    /// 设置位置在桌宠外
    /// </summary>
    public void SetPlaceOUT()
    {
        Height = double.NaN;
        BorderMain.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        Margin = new Thickness(0, 500, 0, 0);
    }

    public void ForceClose()
    {
        ShowTimer.Stop();
        EndTimer.Stop();
        CloseTimer.Stop();
        Dispatcher.UIThread.Invoke(() =>
        {
            Opacity = 1;
            IsVisible = false;
            MessageBoxContent.Children.Clear();
        });
        EndAction?.Invoke();
    }

    public void Dispose()
    {
        ShowTimer.Dispose();
        EndTimer.Dispose();
        CloseTimer.Dispose();
    }
}
