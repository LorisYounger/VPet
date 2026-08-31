using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Display;

public partial class MessageBar : UserControl, IMessageBarBase
{
    private readonly Border _rootBorder;
    private readonly TextBlock _nameText;
    private readonly TextBlock _messageText;
    private readonly ContentControl _messageContentHost;
    private readonly DispatcherTimer _closeTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private int _timeLeft;
    private string? _graphName;
    private SayInfoWithStreamBase? _activeStream;

    public MessageBar()
    {
        InitializeComponent();
        _rootBorder = this.FindControl<Border>("RootBorder") ?? throw new InvalidOperationException("RootBorder not found.");
        _nameText = this.FindControl<TextBlock>("NameText") ?? throw new InvalidOperationException("NameText not found.");
        _messageText = this.FindControl<TextBlock>("MessageText") ?? throw new InvalidOperationException("MessageText not found.");
        _messageContentHost = this.FindControl<ContentControl>("MessageContentHost") ?? throw new InvalidOperationException("MessageContentHost not found.");
        _closeTimer.Tick += (_, _) =>
        {
            if (--_timeLeft <= 0)
            {
                _closeTimer.Stop();
                IsVisible = false;
                EndAction?.Invoke();
            }
        };
        IsVisible = false;
    }

    public bool IsVisible
    {
        get => base.IsVisible;
        set => base.IsVisible = value;
    }

    public object View => this;

    public event Action? EndAction;

    event Action IMessageBarBase.EndAction
    {
        add => EndAction += value;
        remove => EndAction -= value;
    }

    public void Show(string name, string text, string? graphName = null, object? msgContent = null)
    {
        _graphName = graphName;
        _nameText.Text = name;
        _messageText.Text = text;
        if (msgContent is Control control)
        {
            _messageContentHost.Content = control;
            _messageContentHost.IsVisible = true;
        }
        else
        {
            _messageContentHost.Content = null;
            _messageContentHost.IsVisible = false;
        }
        _timeLeft = Math.Max(10, text.Length / 3);
        _closeTimer.Start();
        IsVisible = true;
    }

    public void Show(string name, SayInfoWithStreamBase sayInfoWithStream)
    {
        if (_activeStream != null)
        {
            _activeStream.Event_Update -= OnStreamUpdate;
            _activeStream.Event_Finish -= OnStreamFinish;
        }

        _activeStream = sayInfoWithStream;
        _nameText.Text = name;
        _messageText.Text = sayInfoWithStream.CurrentText.ToString();
        _graphName = sayInfoWithStream.GraphName;

        if (sayInfoWithStream.MsgContent is Control control)
        {
            _messageContentHost.Content = control;
            _messageContentHost.IsVisible = true;
        }
        else
        {
            _messageContentHost.Content = null;
            _messageContentHost.IsVisible = false;
        }

        sayInfoWithStream.Event_Update += OnStreamUpdate;
        if (sayInfoWithStream.IsFinishGen)
        {
            OnStreamFinish(sayInfoWithStream.CurrentText.ToString());
        }
        else
        {
            sayInfoWithStream.Event_Finish += OnStreamFinish;
        }

        IsVisible = true;
    }

    public void ForceClose()
    {
        _closeTimer.Stop();
        IsVisible = false;
        EndAction?.Invoke();
    }

    public void SetPlaceIN()
    {
        _rootBorder.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
    }

    public void SetPlaceOUT()
    {
        _rootBorder.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
    }

    public void Dispose()
    {
        _closeTimer.Stop();
        if (_activeStream != null)
        {
            _activeStream.Event_Update -= OnStreamUpdate;
            _activeStream.Event_Finish -= OnStreamFinish;
        }
    }

    private void OnStreamUpdate((string fullText, string changedText) data)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _messageText.Text = data.fullText;
            _timeLeft = Math.Max(10, data.fullText.Length / 3);
        });
    }

    private void OnStreamFinish(string fullText)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _messageText.Text = fullText;
            _timeLeft = Math.Max(10, fullText.Length / 3);
            _closeTimer.Start();
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
