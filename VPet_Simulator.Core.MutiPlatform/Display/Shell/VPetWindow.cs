using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 桌宠各功能窗口的外壳
/// </summary>
/// 对应 Windows 版那些继承 Panuon 的 WindowX 的窗口. WindowX 靠 WindowXCaption.* 附加属性
/// 定义标题栏, 这里把那几项做成本类的属性, XAML 里 pu:WindowXCaption.Background 机械替换成
/// CaptionBackground 即可; 标题栏本身画在 ResourceStyle.axaml 的 ControlTheme 里.
///
/// 与 WindowX 一样自己画标题栏 (SystemDecorations=None), 三个平台看起来才一样.
public partial class VPetWindow : Window
{
    /// <summary>
    /// 标题栏按钮组合, 对应 WindowXCaption.Buttons
    /// </summary>
    public enum CaptionButtonsType
    {
        None,
        Close,
        MinimizeClose,
        MaximizeClose,
        All,
    }

    public static readonly StyledProperty<IBrush?> CaptionBackgroundProperty =
        AvaloniaProperty.Register<VPetWindow, IBrush?>(nameof(CaptionBackground));
    public static readonly StyledProperty<IBrush?> CaptionForegroundProperty =
        AvaloniaProperty.Register<VPetWindow, IBrush?>(nameof(CaptionForeground));
    public static readonly StyledProperty<double> CaptionHeightProperty =
        AvaloniaProperty.Register<VPetWindow, double>(nameof(CaptionHeight), 30);
    public static readonly StyledProperty<CaptionButtonsType> CaptionButtonsProperty =
        AvaloniaProperty.Register<VPetWindow, CaptionButtonsType>(nameof(CaptionButtons), CaptionButtonsType.All);
    public static readonly StyledProperty<IDataTemplate?> CaptionHeaderTemplateProperty =
        AvaloniaProperty.Register<VPetWindow, IDataTemplate?>(nameof(CaptionHeaderTemplate));
    public static readonly StyledProperty<Color> CaptionShadowColorProperty =
        AvaloniaProperty.Register<VPetWindow, Color>(nameof(CaptionShadowColor), Colors.Transparent);
    public static readonly StyledProperty<object?> OverlayerProperty =
        AvaloniaProperty.Register<VPetWindow, object?>(nameof(Overlayer));
    public static readonly StyledProperty<bool> IsOverlayerVisibleProperty =
        AvaloniaProperty.Register<VPetWindow, bool>(nameof(IsOverlayerVisible));
    public static readonly StyledProperty<bool> IsMaskVisibleProperty =
        AvaloniaProperty.Register<VPetWindow, bool>(nameof(IsMaskVisible));
    public static readonly StyledProperty<IBrush?> MaskBrushProperty =
        AvaloniaProperty.Register<VPetWindow, IBrush?>(nameof(MaskBrush));
    public static readonly StyledProperty<ControlTheme?> CaptionCloseButtonThemeProperty =
        AvaloniaProperty.Register<VPetWindow, ControlTheme?>(nameof(CaptionCloseButtonTheme));
    public static readonly StyledProperty<ControlTheme?> CaptionMinimizeButtonThemeProperty =
        AvaloniaProperty.Register<VPetWindow, ControlTheme?>(nameof(CaptionMinimizeButtonTheme));
    public static readonly StyledProperty<ControlTheme?> CaptionMaximizeButtonThemeProperty =
        AvaloniaProperty.Register<VPetWindow, ControlTheme?>(nameof(CaptionMaximizeButtonTheme));

    /// <summary>
    /// 标题栏背景, 对应 WindowXCaption.Background
    /// </summary>
    public IBrush? CaptionBackground
    {
        get => GetValue(CaptionBackgroundProperty);
        set => SetValue(CaptionBackgroundProperty, value);
    }

    /// <summary>
    /// 标题栏前景, 对应 WindowXCaption.Foreground
    /// </summary>
    public IBrush? CaptionForeground
    {
        get => GetValue(CaptionForegroundProperty);
        set => SetValue(CaptionForegroundProperty, value);
    }

    /// <summary>
    /// 标题栏高度, 对应 WindowXCaption.Height
    /// </summary>
    public double CaptionHeight
    {
        get => GetValue(CaptionHeightProperty);
        set => SetValue(CaptionHeightProperty, value);
    }

    /// <summary>
    /// 标题栏按钮, 对应 WindowXCaption.Buttons
    /// </summary>
    public CaptionButtonsType CaptionButtons
    {
        get => GetValue(CaptionButtonsProperty);
        set => SetValue(CaptionButtonsProperty, value);
    }

    /// <summary>
    /// 标题栏内容模板, 对应 WindowXCaption.HeaderTemplate; 为空时显示 Title
    /// </summary>
    public IDataTemplate? CaptionHeaderTemplate
    {
        get => GetValue(CaptionHeaderTemplateProperty);
        set => SetValue(CaptionHeaderTemplateProperty, value);
    }

    /// <summary>
    /// 标题栏阴影色, 对应 WindowXCaption.ShadowColor
    /// </summary>
    public Color CaptionShadowColor
    {
        get => GetValue(CaptionShadowColorProperty);
        set => SetValue(CaptionShadowColorProperty, value);
    }

    /// <summary>
    /// 盖在内容上面的一层, 对应 WindowX.Overlayer
    /// </summary>
    public object? Overlayer
    {
        get => GetValue(OverlayerProperty);
        set => SetValue(OverlayerProperty, value);
    }

    /// <summary>
    /// 覆盖层是否显示, 对应 WindowX.IsOverlayerVisible
    /// </summary>
    public bool IsOverlayerVisible
    {
        get => GetValue(IsOverlayerVisibleProperty);
        set => SetValue(IsOverlayerVisibleProperty, value);
    }

    /// <summary>
    /// 遮罩是否显示, 对应 WindowX.IsMaskVisible (盖在内容上、覆盖层下的半透明一层)
    /// </summary>
    public bool IsMaskVisible
    {
        get => GetValue(IsMaskVisibleProperty);
        set => SetValue(IsMaskVisibleProperty, value);
    }

    /// <summary>
    /// 遮罩的颜色, 对应 WindowX.MaskBrush
    /// </summary>
    public IBrush? MaskBrush
    {
        get => GetValue(MaskBrushProperty);
        set => SetValue(MaskBrushProperty, value);
    }

    /// <summary>
    /// 关闭按钮的样式, 对应 WindowXCaption.CloseButtonStyle
    /// </summary>
    public ControlTheme? CaptionCloseButtonTheme
    {
        get => GetValue(CaptionCloseButtonThemeProperty);
        set => SetValue(CaptionCloseButtonThemeProperty, value);
    }

    /// <summary>
    /// 最小化按钮的样式, 对应 WindowXCaption.MinimizeButtonStyle
    /// </summary>
    public ControlTheme? CaptionMinimizeButtonTheme
    {
        get => GetValue(CaptionMinimizeButtonThemeProperty);
        set => SetValue(CaptionMinimizeButtonThemeProperty, value);
    }

    /// <summary>
    /// 最大化按钮的样式, 对应 WindowXCaption.MaximizeButtonStyle
    /// </summary>
    public ControlTheme? CaptionMaximizeButtonTheme
    {
        get => GetValue(CaptionMaximizeButtonThemeProperty);
        set => SetValue(CaptionMaximizeButtonThemeProperty, value);
    }

    /// <summary>
    /// 窗口是否已经关掉, 对应 WindowX.IsClosed
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// 按住这个元素可以拖动窗口, 对应 WindowX.IsDragMoveArea 附加属性
    /// </summary>
    public static readonly AttachedProperty<bool> IsDragMoveAreaProperty =
        AvaloniaProperty.RegisterAttached<VPetWindow, Control, bool>("IsDragMoveArea");

    public static bool GetIsDragMoveArea(Control control) => control.GetValue(IsDragMoveAreaProperty);
    public static void SetIsDragMoveArea(Control control, bool value) => control.SetValue(IsDragMoveAreaProperty, value);

    static VPetWindow()
    {
        IsDragMoveAreaProperty.Changed.AddClassHandler<Control>((control, e) =>
        {
            if (e.NewValue is true)
                control.PointerPressed += DragMoveArea_PointerPressed;
            else
                control.PointerPressed -= DragMoveArea_PointerPressed;
        });
    }

    private static void DragMoveArea_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && e.GetCurrentPoint(control).Properties.IsLeftButtonPressed
            && TopLevel.GetTopLevel(control) is Window window)
            window.BeginMoveDrag(e);
    }

    /// <summary>
    /// 窗口左边缘的屏幕坐标 (逻辑像素), 对应 WPF 的 Window.Left
    /// </summary>
    public double Left
    {
        get => Position.X / RenderScaling;
        set => Position = new PixelPoint((int)Math.Round(value * RenderScaling), Position.Y);
    }

    /// <summary>
    /// 窗口上边缘的屏幕坐标 (逻辑像素), 对应 WPF 的 Window.Top
    /// </summary>
    public double Top
    {
        get => Position.Y / RenderScaling;
        set => Position = new PixelPoint(Position.X, (int)Math.Round(value * RenderScaling));
    }

    public VPetWindow()
    {
        WindowDecorations = WindowDecorations.None;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Closed += (_, _) => IsClosed = true;
    }

    protected override Type StyleKeyOverride => typeof(VPetWindow);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        //标题栏: 按住拖动, 三个按钮
        if (e.NameScope.Find<Control>("PART_Caption") is { } caption)
            caption.PointerPressed += (s, args) =>
            {
                if (args.GetCurrentPoint(caption).Properties.IsLeftButtonPressed && !(args.Source is Button))
                    BeginMoveDrag(args);
            };
        if (e.NameScope.Find<Button>("PART_MinimizeButton") is { } minimize)
        {
            minimize.Click += (_, _) => WindowState = WindowState.Minimized;
            if (CaptionMinimizeButtonTheme != null)
                minimize.Theme = CaptionMinimizeButtonTheme;
        }
        if (e.NameScope.Find<Button>("PART_MaximizeButton") is { } maximize)
        {
            maximize.Click += (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            if (CaptionMaximizeButtonTheme != null)
                maximize.Theme = CaptionMaximizeButtonTheme;
        }
        if (e.NameScope.Find<Button>("PART_CloseButton") is { } close)
        {
            close.Click += (_, _) => Close();
            if (CaptionCloseButtonTheme != null)
                close.Theme = CaptionCloseButtonTheme;
        }
    }

    /// <summary>
    /// 模态显示并等到关闭, 对应 WPF 的 Window.ShowDialog()
    /// </summary>
    /// Avalonia 的 ShowDialog 是异步的; Windows 版的代码都是同步写法 (弹完接着往下走),
    /// 这里用 DispatcherFrame 把消息循环推进去等, 调用方的代码才能原样保留.
    public void ShowDialog() => ShowDialog(null);

    /// <summary>
    /// 模态显示并等到关闭, 指定属主
    /// </summary>
    public void ShowDialog(Window? owner)
    {
        owner ??= Owner as Window ?? DialogService.DefaultOwner;
        var frame = new DispatcherFrame();
        Closed += (_, _) => frame.Continue = false;
        if (owner != null && owner.IsVisible)
            _ = base.ShowDialog(owner);
        else
            Show();
        Dispatcher.UIThread.PushFrame(frame);
    }

    /// <summary>
    /// 在窗口底部飘一条提示, 对应 WindowX.Toast
    /// </summary>
    public void Toast(string message, int duration = 3000, MessageBoxIcon icon = MessageBoxIcon.None)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            var label = new Border
            {
                Background = this.FindResource("DARKPrimaryText") as IBrush,
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(25, 15),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 40),
                Child = new TextBlock { Text = message, Foreground = this.FindResource("DARKPrimary") as IBrush },
            };
            var previous = Overlayer;
            var previousVisible = IsOverlayerVisible;
            Overlayer = label;
            IsOverlayerVisible = true;
            await System.Threading.Tasks.Task.Delay(duration);
            if (ReferenceEquals(Overlayer, label))
            {
                Overlayer = previous;
                IsOverlayerVisible = previousVisible;
            }
        });
    }

    /// <summary>
    /// 激活并置前, 对应 WPF 的 Window.Focus()
    /// </summary>
    public new bool Focus()
    {
        Activate();
        return base.Focus();
    }
}
