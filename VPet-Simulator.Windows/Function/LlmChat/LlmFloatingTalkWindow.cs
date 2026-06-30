using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VPet_Simulator.Core;
using Forms = System.Windows.Forms;

namespace VPet_Simulator.Windows;

internal class LlmFloatingTalkWindow : Window
{
    private const double BubbleWidth = 440;
    private const double BubbleMaxHeight = 270;
    private const double Gap = 12;

    private readonly MainWindow owner;
    private readonly TextBox inputBox;
    private readonly Button recordButton;
    private readonly Button replayButton;
    private readonly TextBlock textBlock;
    private readonly FrameworkElement outputPanel;
    private readonly FrameworkElement inputPanel;
    private readonly ScrollViewer scrollViewer;
    private readonly DispatcherTimer followTimer;
    private readonly DispatcherTimer outputTimer;
    private readonly DispatcherTimer thinkingTimer;
    private readonly Queue<char> pendingOutput = new();
    private CancellationTokenSource closeCts;
    private string fullText = "";
    private string visibleText = "";
    private bool isUserPositioned;
    private bool voiceInputEnabled;
    private bool voiceInputBusy;
    private bool thinkingActive;
    private int thinkingFrame;

    public LlmFloatingTalkWindow(MainWindow owner)
    {
        this.owner = owner;
        Owner = owner;
        ShowInTaskbar = false;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ShowActivated = false;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.Height;
        Topmost = owner.Topmost;
        Width = BubbleWidth;
        MinWidth = BubbleWidth;
        MaxHeight = BubbleMaxHeight + 130;

        textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
            FontSize = 18,
            LineHeight = 26,
            Padding = new Thickness(0, 0, 4, 0)
        };

        scrollViewer = new ScrollViewer
        {
            MaxHeight = BubbleMaxHeight,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = textBlock
        };

        var closeBubbleButton = CreateFlatCloseButton("关闭回复气泡".Translate());
        closeBubbleButton.Margin = new Thickness(8, 0, 0, 0);
        closeBubbleButton.Click += (_, _) => ClearOutput();

        replayButton = CreateFlatTextButton("重播".Translate(), "重新播放语音".Translate());
        replayButton.Margin = new Thickness(8, 6, 0, 0);
        replayButton.Visibility = Visibility.Collapsed;
        replayButton.Click += (_, _) => ReplayRequested?.Invoke();

        var outputButtons = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Top
        };
        outputButtons.Children.Add(closeBubbleButton);
        outputButtons.Children.Add(replayButton);

        var outputGrid = new Grid();
        outputGrid.ColumnDefinitions.Add(new ColumnDefinition());
        outputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        outputGrid.Children.Add(scrollViewer);
        Grid.SetColumn(outputButtons, 1);
        outputGrid.Children.Add(outputButtons);

        var outputBorder = CreateBubbleBorder(outputGrid);
        outputBorder.Visibility = Visibility.Collapsed;
        outputBorder.MouseLeftButtonDown += DragArea_MouseLeftButtonDown;
        outputPanel = outputBorder;

        inputBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            MinHeight = 48,
            MaxHeight = 92,
            FontSize = 16,
            Padding = new Thickness(2, 4, 2, 4),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.Transparent,
            Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText)
        };
        inputBox.PreviewKeyDown += InputBox_PreviewKeyDown;
        inputBox.TextChanged += InputBox_TextChanged;

        recordButton = new Button
        {
            Content = "按住说".Translate(),
            Width = 68,
            Height = 34,
            Margin = new Thickness(10, 7, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Collapsed,
            Background = Function.ResourcesBrush(Function.BrushType.SecondaryLight),
            BorderBrush = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker),
            Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
            ToolTip = "按住录音, 松开识别".Translate()
        };
        recordButton.PreviewMouseLeftButtonDown += RecordButton_PreviewMouseLeftButtonDown;
        recordButton.PreviewMouseLeftButtonUp += RecordButton_PreviewMouseLeftButtonUp;

        var sendButton = new Button
        {
            Content = "发送".Translate(),
            Width = 74,
            Height = 34,
            Margin = new Thickness(10, 7, 8, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Background = Function.ResourcesBrush(Function.BrushType.SecondaryLight),
            BorderBrush = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker),
            Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText)
        };
        sendButton.Click += (_, _) => SendInput();

        var closeInputButton = CreateFlatCloseButton("关闭输入框".Translate());
        closeInputButton.Margin = new Thickness(0, 10, 0, 0);
        closeInputButton.Click += (_, _) => Close();

        var inputRow = new Grid();
        inputRow.ColumnDefinitions.Add(new ColumnDefinition());
        inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputRow.Children.Add(inputBox);
        Grid.SetColumn(recordButton, 1);
        inputRow.Children.Add(recordButton);
        Grid.SetColumn(sendButton, 2);
        inputRow.Children.Add(sendButton);
        Grid.SetColumn(closeInputButton, 3);
        inputRow.Children.Add(closeInputButton);

        var inputBorder = CreateBubbleBorder(inputRow);
        inputBorder.Margin = new Thickness(0, 8, 0, 0);
        inputBorder.MouseLeftButtonDown += DragArea_MouseLeftButtonDown;
        inputPanel = inputBorder;

        var stack = new StackPanel
        {
            Width = BubbleWidth
        };
        stack.Children.Add(outputPanel);
        stack.Children.Add(inputBorder);

        Content = stack;
        Loaded += (_, _) => Reposition();
        SizeChanged += (_, _) => Reposition();
        owner.LocationChanged += OwnerChanged;
        owner.SizeChanged += OwnerChanged;
        owner.StateChanged += OwnerChanged;
        owner.Closed += OwnerClosed;

        followTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(350)
        };
        followTimer.Tick += (_, _) =>
        {
            Topmost = owner.Topmost;
            Reposition();
        };
        followTimer.Start();

        outputTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(35)
        };
        outputTimer.Tick += (_, _) => FlushOutputTick();

        thinkingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(320)
        };
        thinkingTimer.Tick += (_, _) => FlushThinkingTick();
    }

    public event Action<string> InputSubmitted;
    public event Action VoiceInputStarted;
    public event Action VoiceInputStopped;
    public event Action ReplayRequested;

    public bool IsClosed { get; private set; }
    public bool HasInputText => !string.IsNullOrWhiteSpace(inputBox.Text);
    public string OutputText => fullText;

    public void SetInputPanelVisible(bool visible)
    {
        if (IsClosed)
            return;
        inputPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        Reposition();
    }

    public void FocusInput()
    {
        if (IsClosed)
            return;
        CancelAutoClose();
        SetInputPanelVisible(true);
        if (!IsVisible)
            Show();
        Activate();
        FocusManager.SetFocusedElement(this, inputBox);
        inputBox.Focus();
        Keyboard.Focus(inputBox);
    }

    public void SetInputEnabled(bool enabled)
    {
        if (IsClosed)
            return;
        inputBox.IsEnabled = enabled;
        recordButton.IsEnabled = enabled && voiceInputEnabled && !voiceInputBusy;
    }

    public void SetInputText(string text, bool append = false)
    {
        if (IsClosed)
            return;
        SetInputPanelVisible(true);
        if (!IsVisible)
            Show();

        if (append && !string.IsNullOrWhiteSpace(inputBox.Text))
            inputBox.Text = inputBox.Text.TrimEnd() + Environment.NewLine + (text ?? "");
        else
            inputBox.Text = text ?? "";
        inputBox.CaretIndex = inputBox.Text.Length;
        FocusInput();
    }

    public void SetVoiceInputEnabled(bool enabled)
    {
        if (IsClosed)
            return;
        voiceInputEnabled = enabled;
        recordButton.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        recordButton.IsEnabled = enabled && inputBox.IsEnabled && !voiceInputBusy;
        if (!enabled)
            recordButton.Content = "按住说".Translate();
    }

    public void SetVoiceInputState(string text, bool busy)
    {
        if (IsClosed)
            return;
        voiceInputBusy = busy;
        recordButton.Content = string.IsNullOrWhiteSpace(text) ? "按住说".Translate() : text.Translate();
        recordButton.IsEnabled = voiceInputEnabled && inputBox.IsEnabled && !busy;
    }

    public void BeginResponse(bool showThinking = true)
    {
        if (IsClosed)
            return;
        CancelAutoClose();
        ClearOutput();
        SetReplayEnabled(false);
        if (showThinking)
            SetThinking();
    }

    public void ShowPlainText(string text)
    {
        if (IsClosed)
            return;
        if (!IsVisible)
            Show();
        ReplaceText(text ?? "");
    }

    public void ShowOutputOnly()
    {
        if (IsClosed)
            return;
        SetInputPanelVisible(false);
        if (!IsVisible)
            Show();
    }

    public void SetThinking()
    {
        if (IsClosed)
            return;
        if (!IsVisible)
            Show();
        pendingOutput.Clear();
        outputTimer.Stop();
        thinkingFrame = 0;
        thinkingActive = true;
        outputPanel.Visibility = Visibility.Visible;
        RenderThinkingText();
        thinkingTimer.Start();
        Reposition();
    }

    public void AppendText(string text)
    {
        if (IsClosed)
            return;
        if (!IsVisible)
            Show();
        QueueText(text);
    }

    public void ReplaceText(string text)
    {
        if (IsClosed)
            return;
        if (!IsVisible)
            Show();
        StopThinking(true);
        pendingOutput.Clear();
        outputTimer.Stop();
        fullText = "";
        visibleText = "";
        RenderVisibleText();
        QueueText(text ?? "");
    }

    public void FlushPendingOutput()
    {
        if (IsClosed)
            return;
        while (pendingOutput.Count > 0)
            visibleText += pendingOutput.Dequeue();
        fullText = visibleText;
        RenderVisibleText();
        scrollViewer.ScrollToEnd();
        Reposition();
    }

    public async Task WaitForOutputCompleteAsync(CancellationToken cancellationToken)
    {
        while (!IsClosed && !cancellationToken.IsCancellationRequested)
        {
            bool hasPending = await Dispatcher.InvokeAsync(() => pendingOutput.Count > 0);
            if (!hasPending)
                return;
            await Task.Delay(50, cancellationToken);
        }
    }

    public async Task WaitThenCloseAsync(CancellationToken cancellationToken)
    {
        int ms = Math.Min(Math.Max((fullText?.Length ?? 0) * 70 + 2500, 3500), 16000);
        await WaitThenCloseAsync(ms, cancellationToken);
    }

    public async Task WaitThenCloseAsync(int milliseconds, CancellationToken cancellationToken)
    {
        if (IsClosed)
            return;
        closeCts?.Cancel();
        closeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            await Task.Delay(Math.Max(1000, milliseconds), closeCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        await Dispatcher.InvokeAsync(() =>
        {
            if (!HasInputText)
                Close();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        IsClosed = true;
        closeCts?.Cancel();
        followTimer.Stop();
        outputTimer.Stop();
        thinkingTimer.Stop();
        owner.LocationChanged -= OwnerChanged;
        owner.SizeChanged -= OwnerChanged;
        owner.StateChanged -= OwnerChanged;
        owner.Closed -= OwnerClosed;
        inputBox.PreviewKeyDown -= InputBox_PreviewKeyDown;
        inputBox.TextChanged -= InputBox_TextChanged;
        recordButton.PreviewMouseLeftButtonDown -= RecordButton_PreviewMouseLeftButtonDown;
        recordButton.PreviewMouseLeftButtonUp -= RecordButton_PreviewMouseLeftButtonUp;
        base.OnClosed(e);
    }

    private static Border CreateBubbleBorder(UIElement child)
    {
        return new Border
        {
            Width = BubbleWidth,
            MinWidth = BubbleWidth,
            Background = Function.ResourcesBrush(Function.BrushType.Primary),
            BorderBrush = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDark),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12),
            Child = child
        };
    }

    private static Button CreateFlatCloseButton(string toolTip)
    {
        return new Button
        {
            Content = "×",
            Width = 24,
            Height = 24,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
            ToolTip = toolTip
        };
    }

    private static Button CreateFlatTextButton(string text, string toolTip)
    {
        return new Button
        {
            Content = text,
            Width = 36,
            Height = 24,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
            FontSize = 12,
            ToolTip = toolTip
        };
    }

    private void ClearOutput()
    {
        CancelAutoClose();
        StopThinking(true);
        pendingOutput.Clear();
        outputTimer.Stop();
        fullText = "";
        visibleText = "";
        RenderVisibleText();
        outputPanel.Visibility = Visibility.Collapsed;
        SetReplayEnabled(false);
        Reposition();
    }

    private void CancelAutoClose()
    {
        closeCts?.Cancel();
    }

    public void SetReplayEnabled(bool enabled)
    {
        if (IsClosed)
            return;
        replayButton.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        replayButton.IsEnabled = enabled;
    }

    private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed || IsInteractiveSource(e.OriginalSource as DependencyObject))
            return;
        try
        {
            isUserPositioned = true;
            DragMove();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static bool IsInteractiveSource(DependencyObject source)
    {
        while (source != null)
        {
            if (source is TextBox or ButtonBase or ScrollBar)
                return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }

    private void QueueText(string text)
    {
        if (IsClosed || string.IsNullOrEmpty(text))
            return;
        if (thinkingActive)
            StopThinking(true);
        outputPanel.Visibility = Visibility.Visible;
        foreach (char c in text)
            pendingOutput.Enqueue(c);
        fullText += text;
        if (!outputTimer.IsEnabled)
            outputTimer.Start();
    }

    private void FlushOutputTick()
    {
        if (pendingOutput.Count == 0)
        {
            outputTimer.Stop();
            return;
        }

        int count = Math.Min(3, pendingOutput.Count);
        for (int i = 0; i < count; i++)
            visibleText += pendingOutput.Dequeue();
        RenderVisibleText();
        scrollViewer.ScrollToEnd();
        Reposition();
    }

    private void FlushThinkingTick()
    {
        if (!thinkingActive)
            return;
        thinkingFrame = (thinkingFrame + 1) % 4;
        RenderThinkingText();
        Reposition();
    }

    private void RenderThinkingText()
    {
        visibleText = "思考中".Translate() + new string('.', thinkingFrame);
        textBlock.Inlines.Clear();
        textBlock.Inlines.Add(new Run(visibleText));
        scrollViewer.ScrollToEnd();
    }

    private void StopThinking(bool clearText)
    {
        if (!thinkingActive && !thinkingTimer.IsEnabled)
            return;
        thinkingActive = false;
        thinkingTimer.Stop();
        if (!clearText)
            return;
        fullText = "";
        visibleText = "";
        textBlock.Inlines.Clear();
    }

    private void RenderVisibleText()
    {
        textBlock.Inlines.Clear();
        if (string.IsNullOrEmpty(visibleText))
            return;

        int index = 0;
        while (index < visibleText.Length)
        {
            if (visibleText[index] == '\r')
            {
                index++;
                continue;
            }
            if (visibleText[index] == '\n')
            {
                textBlock.Inlines.Add(new LineBreak());
                index++;
                continue;
            }
            if (TryAddToken("**", index, FontWeights.Bold, null, out int nextIndex))
            {
                index = nextIndex;
                continue;
            }
            if (visibleText[index] == '`' && TryAddInlineCode(index, out nextIndex))
            {
                index = nextIndex;
                continue;
            }
            if (visibleText[index] == '*' && TryAddToken("*", index, null, FontStyles.Italic, out nextIndex))
            {
                index = nextIndex;
                continue;
            }

            int plainEnd = FindNextMarkdownToken(index + 1);
            AddPlainRun(visibleText[index..plainEnd]);
            index = plainEnd;
        }
    }

    private bool TryAddToken(string marker, int index, FontWeight? weight, FontStyle? style, out int nextIndex)
    {
        nextIndex = index;
        if (!visibleText.AsSpan(index).StartsWith(marker, StringComparison.Ordinal))
            return false;

        int start = index + marker.Length;
        int end = visibleText.IndexOf(marker, start, StringComparison.Ordinal);
        if (end <= start)
            return false;

        var run = new Run(visibleText[start..end]);
        if (weight.HasValue)
            run.FontWeight = weight.Value;
        if (style.HasValue)
            run.FontStyle = style.Value;
        textBlock.Inlines.Add(run);
        nextIndex = end + marker.Length;
        return true;
    }

    private bool TryAddInlineCode(int index, out int nextIndex)
    {
        nextIndex = index;
        int start = index + 1;
        int end = visibleText.IndexOf('`', start);
        if (end <= start)
            return false;

        textBlock.Inlines.Add(new Run(visibleText[start..end])
        {
            FontFamily = new FontFamily("Consolas"),
            Background = new SolidColorBrush(Color.FromArgb(32, 0, 0, 0))
        });
        nextIndex = end + 1;
        return true;
    }

    private int FindNextMarkdownToken(int start)
    {
        for (int i = start; i < visibleText.Length; i++)
        {
            char c = visibleText[i];
            if (c is '\r' or '\n' or '*' or '`')
                return i;
        }
        return visibleText.Length;
    }

    private void AddPlainRun(string text)
    {
        if (!string.IsNullOrEmpty(text))
            textBlock.Inlines.Add(new Run(text));
    }

    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            return;

        e.Handled = true;
        SendInput();
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (HasInputText)
            CancelAutoClose();
    }

    private void RecordButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!voiceInputEnabled || voiceInputBusy)
            return;
        recordButton.CaptureMouse();
        e.Handled = true;
        VoiceInputStarted?.Invoke();
    }

    private void RecordButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!recordButton.IsMouseCaptured)
            return;
        recordButton.ReleaseMouseCapture();
        e.Handled = true;
        VoiceInputStopped?.Invoke();
    }

    private void SendInput()
    {
        string text = inputBox.Text;
        if (string.IsNullOrWhiteSpace(text))
            return;

        inputBox.Text = "";
        InputSubmitted?.Invoke(text);
    }

    private void OwnerChanged(object sender, EventArgs e)
    {
        Reposition();
    }

    private void OwnerClosed(object sender, EventArgs e)
    {
        Close();
    }

    private void Reposition()
    {
        if (!IsVisible || owner.Dispatcher.HasShutdownStarted || owner.Dispatcher.HasShutdownFinished)
            return;
        if (isUserPositioned)
            return;

        var screen = Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(owner).Handle);
        var work = screen.WorkingArea;
        double scaleX = 1;
        double scaleY = 1;
        var source = PresentationSource.FromVisual(owner);
        if (source?.CompositionTarget != null)
        {
            scaleX = source.CompositionTarget.TransformFromDevice.M11;
            scaleY = source.CompositionTarget.TransformFromDevice.M22;
        }

        Rect workArea = new Rect(work.X * scaleX, work.Y * scaleY, work.Width * scaleX, work.Height * scaleY);
        Rect pet = new Rect(owner.Left, owner.Top, owner.ActualWidth, owner.ActualHeight);
        double width = ActualWidth > 1 ? ActualWidth : BubbleWidth;
        double height = ActualHeight > 1 ? ActualHeight : Math.Min(BubbleMaxHeight, 180);

        double rightSpace = workArea.Right - pet.Right - Gap;
        double leftSpace = pet.Left - workArea.Left - Gap;
        double upSpace = pet.Top - workArea.Top - Gap;
        double downSpace = workArea.Bottom - pet.Bottom - Gap;

        Point target;
        if (rightSpace >= width || rightSpace >= leftSpace && rightSpace >= upSpace && rightSpace >= downSpace)
        {
            target = new Point(pet.Right + Gap, pet.Top + (pet.Height - height) / 2);
        }
        else if (leftSpace >= width || leftSpace >= upSpace && leftSpace >= downSpace)
        {
            target = new Point(pet.Left - width - Gap, pet.Top + (pet.Height - height) / 2);
        }
        else if (upSpace >= height || upSpace >= downSpace)
        {
            target = new Point(pet.Left + (pet.Width - width) / 2, pet.Top - height - Gap);
        }
        else
        {
            target = new Point(pet.Left + (pet.Width - width) / 2, pet.Bottom + Gap);
        }

        Left = Clamp(target.X, workArea.Left + Gap, workArea.Right - width - Gap);
        Top = Clamp(target.Y, workArea.Top + Gap, workArea.Bottom - height - Gap);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (max < min)
            return min;
        return Math.Min(Math.Max(value, min), max);
    }
}
