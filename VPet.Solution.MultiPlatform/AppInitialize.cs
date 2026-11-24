using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.Avalonia;
using HanumanInstitute.MvvmDialogs.FileSystem;
using HKW.MVVM.SourceGenerator;
using LinePutScript.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VPet.Solution.MultiPlatform.ViewModels;
using VPet.Solution.MultiPlatform.Views;

namespace VPet.Solution.MultiPlatform;

internal partial class AppServices : DIConfigurationBase
{
    public ViewLocator ViewLocator { get; } = new();

    protected override void Configure(IServiceCollection services)
    {
        services.AddSingleton<IDialogService>(sp => new DialogService(
            new DialogManagerX(
                ViewLocator,
                dialogFactory: new DialogFactory().AddFluent(FluentMessageBoxType.ContentDialog)
            ),
            viewModelFactory: x => sp.GetService(x)
        ));

        RegisterWindow<MainViewModel, MainView, MainWindow>();

        RegisterWindow<SettingViewModel, SettingView, SettingWindow>();
        RegisterView<GraphicsSettingViewModel, GraphicsSettingView>();
        RegisterView<SystemSettingViewModel, SystemSettingView>();
        RegisterView<DiagnosticSettingViewModel, DiagnosticSettingView>();
        RegisterView<InteractiveSettingViewModel, InteractiveSettingView>();
        RegisterView<CustomizedSettingViewModel, CustomizedSettingView>();
        RegisterView<ModSettingViewModel, ModSettingView>();

        RegisterWindow<SaveViewModel, SaveView, SaveWindow>();
    }

    [DICustomServiceRegistrar(nameof(TViewModel))]
    public void RegisterView<TViewModel, TView>()
        where TViewModel : class, INotifyPropertyChanged
        where TView : UserControl, new()
    {
        ViewLocator.RegisterView<TViewModel, TView>();
    }

    [DICustomServiceRegistrar(nameof(TViewModel))]
    public void RegisterWindow<TViewModel, TView, TWindow>()
        where TViewModel : class, INotifyPropertyChanged
        where TView : UserControl, new()
        where TWindow : Window, new()
    {
        ViewLocator.Register<TViewModel, TView, TWindow>();
    }
}

public class ViewLocator : StrongViewLocator
{
    public void RegisterView<TViewModel, TView>()
        where TViewModel : class, INotifyPropertyChanged
        where TView : Control, new()
    {
        Register<TViewModel, TView>();
    }
}

public class DialogManagerX : DialogManager
{
    public DialogManagerX(
        IViewLocator? viewLocator = null,
        IDialogFactory? dialogFactory = null,
        ILogger<DialogManager>? logger = null,
        IDispatcher? dispatcher = null
    )
        : base(viewLocator, dialogFactory, logger, dispatcher) { }

    public override IView? FindViewByViewModel(INotifyPropertyChanged viewModel)
    {
        // 开始寻找窗口
        var view = base.FindViewByViewModel(viewModel);
        if (view is not null)
            return view;

        // 如果是单例视图, 直接返回主视图
        if (
            Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView
            && singleView.MainView is ContentControl mainView
        )
            return AsWrapper(mainView);

        // 寻找嵌套视图
        if (
            Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.Windows.FirstOrDefault(w =>
                w.IsActive
                && w.GetVisualDescendants().Any(v => ReferenceEquals(v.DataContext, viewModel))
            )
                is { } window
        )
            return AsWrapper(window);

        return null;
    }
}

public static class NativeUtils
{
    /// <summary>
    /// 解码像素宽度
    /// </summary>
    public const int DecodePixelWidth = 250;

    /// <summary>
    /// 解码像素高度
    /// </summary>
    public const int DecodePixelHeight = 250;

    /// <summary>
    /// 载入图片到内存位图
    /// </summary>
    /// <param name="imagePath">图片路径</param>
    /// <returns>位图对象</returns>
    public static Bitmap? LoadImageToStream(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || File.Exists(imagePath) is false)
            return null;
        using var stream = File.OpenRead(imagePath);
        return new Bitmap(stream);
    }

    /// <summary>
    /// 从存储文件异步载入图片到内存位图
    /// </summary>
    /// <param name="file">图片存储文件</param>
    /// <returns>位图对象</returns>
    public static async Task<Bitmap?> LoadImageToStreamAsync(IDialogStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return new Bitmap(memoryStream);
    }

    /// <summary>
    /// 载入图片至内存流并按宽度解码
    /// </summary>
    /// <param name="imagePath">图片路径</param>
    /// <returns>位图对象</returns>
    public static Bitmap LoadImageToMemoryStream(string imagePath)
    {
        var bytes = File.ReadAllBytes(imagePath);
        using var stream = new MemoryStream(bytes);
        return Bitmap.DecodeToWidth(stream, DecodePixelWidth);
    }

    /// <summary>
    /// 载入图片至内存流并按宽度解码
    /// </summary>
    /// <param name="imageStream">图片流</param>
    /// <returns>位图对象</returns>
    public static Bitmap LoadImageToMemoryStream(Stream imageStream)
    {
        return Bitmap.DecodeToWidth(imageStream, DecodePixelWidth);
    }

    /// <summary>
    /// 获取当前应用的顶层容器
    /// </summary>
    /// <returns>顶层容器或 null</returns>
    public static TopLevel? GetTopLevel()
    {
        return Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => desktop.Windows.FirstOrDefault(w =>
                w.IsActive
            ) ?? desktop.MainWindow,
            ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),
            _ => null,
        };
    }

    /// <summary>
    /// 跨平台打开网页链接或本地路径
    /// </summary>
    /// <param name="target">网页链接或本地文件/目录路径</param>
    public static void Launch(string? target)
    {
        _ = LaunchAsync(target);
    }

    /// <summary>
    /// 跨平台异步打开网页链接或本地路径
    /// </summary>
    /// <param name="target">网页链接或本地文件/目录路径</param>
    /// <returns>是否成功发起打开操作</returns>
    public static async Task<bool> LaunchAsync(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return false;

        var launcher = GetTopLevel()?.Launcher;
        if (launcher is not null)
        {
            if (File.Exists(target))
            {
                if (await launcher.LaunchFileInfoAsync(new FileInfo(target)))
                    return true;
            }
            else if (Directory.Exists(target))
            {
                if (await launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(target)))
                    return true;
            }
            else if (Uri.TryCreate(target, UriKind.Absolute, out var uri))
            {
                if (await launcher.LaunchUriAsync(uri))
                    return true;
            }
        }

        if (OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
            return false;

        return LaunchWithProcess(target);
    }

    /// <summary>
    /// 跨平台异步打开存储文件或文件夹 (在 Web 端对文件会在新标签页打开文本预览)
    /// </summary>
    /// <param name="item">存储文件或文件夹</param>
    /// <returns>是否成功发起打开操作</returns>
    public static async Task<bool> LaunchAsync(IDialogStorageItem? item)
    {
        if (item is null)
            return false;

        var launcher = GetTopLevel()?.Launcher;
        if (launcher is not null)
        {
            if (item is AvaloniaDialogStorageFile avaloniaFile)
            {
                if (await launcher.LaunchFileAsync(avaloniaFile.Source))
                    return true;
            }
            else if (item is AvaloniaDialogStorageFolder avaloniaFolder)
            {
                if (await launcher.LaunchFileAsync(avaloniaFolder.Source))
                    return true;
            }
        }

        if (item.TryGetLocalPath() is { Length: > 0 } localPath)
            return await LaunchAsync(localPath);

        if (
            OperatingSystem.IsBrowser()
            && launcher is not null
            && item is IDialogStorageFile storageFile
        )
        {
            try
            {
                var text = await storageFile.ReadAllTextAsync();
                var dataUri = new Uri(
                    $"data:text/plain;charset=utf-8,{Uri.EscapeDataString(text)}",
                    UriKind.Absolute
                );
                return await launcher.LaunchUriAsync(dataUri);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// 跨平台在系统文件管理器中定位文件或打开所在目录
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static void RevealInFileExplorer(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        if (OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
            return;

        try
        {
            var fullPath = Path.GetFullPath(filePath);
            if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", $"/select,\"{fullPath}\"")?.Dispose();
                return;
            }
            if (OperatingSystem.IsMacOS())
            {
                using var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "open",
                        UseShellExecute = false,
                        ArgumentList = { "-R", fullPath },
                    }
                );
                return;
            }
            var dir = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(dir) is false)
                LaunchWithProcess(dir);
        }
        catch
        {
            // 忽略文件管理器启动失败
        }
    }

    /// <summary>
    /// 桌面端通过系统进程打开链接或本地路径的兜底方法
    /// </summary>
    /// <param name="target">网页链接或本地路径</param>
    /// <returns>是否成功启动</returns>
    private static bool LaunchWithProcess(string target)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var process = Process.Start(
                    new ProcessStartInfo { FileName = target, UseShellExecute = true }
                );
                return true;
            }
            if (OperatingSystem.IsMacOS())
            {
                using var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "open",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        ArgumentList = { target },
                    }
                );
                return true;
            }
            if (OperatingSystem.IsLinux())
            {
                using var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        ArgumentList = { target },
                    }
                );
                return true;
            }
        }
        catch
        {
            // 忽略外部程序启动失败
        }
        return false;
    }
}

/// <summary>
/// 面板辅助类
/// </summary>
public static class PanelHelper
{
    /// <summary>
    /// 按索引控制子元素可见性的附加属性
    /// </summary>
    public static readonly AttachedProperty<int> ShowChildByIndexProperty =
        AvaloniaProperty.RegisterAttached<Panel, int>("ShowChildByIndex", typeof(PanelHelper));

    static PanelHelper()
    {
        ShowChildByIndexProperty.Changed.AddClassHandler<Panel>(ShowChildByIndexPropertyChanged);
    }

    public static int GetShowChildByIndex(Panel control)
    {
        return control.GetValue(ShowChildByIndexProperty);
    }

    public static void SetShowChildByIndex(Panel control, int value)
    {
        control.SetValue(ShowChildByIndexProperty, value);
    }

    private static void ShowChildByIndexPropertyChanged(
        Panel panel,
        AvaloniaPropertyChangedEventArgs e
    )
    {
        var index = GetShowChildByIndex(panel);
        for (var i = 0; i < panel.Children.Count; i++)
        {
            panel.Children[i].IsVisible = i == index;
        }
    }
}

/// <summary>
/// LPS 翻译转换器
/// </summary>
public class LPSTranslateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value?.ToString() is string str)
            return str.Translate();
        return null;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        throw new NotSupportedException("LPSTranslateConverter does not support ConvertBack.");
    }
}

/// <summary>
/// 将 NumericUpDown 的 decimal? 与 double 双向转换
/// </summary>
public class DoubleToDecimalConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            double d => System.Convert.ToDecimal(d),
            int i => System.Convert.ToDecimal(i),
            decimal m => m,
            _ => null,
        };
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is decimal m)
        {
            if (targetType == typeof(int) || targetType == typeof(int?))
                return System.Convert.ToInt32(m);
            return System.Convert.ToDouble(m);
        }
        return value;
    }
}

/// <summary>
/// 将 NumericUpDown 的 decimal? 与 int 双向转换
/// </summary>
public class IntToDecimalConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => System.Convert.ToDecimal(i),
            double d => System.Convert.ToDecimal(d),
            decimal m => m,
            _ => null,
        };
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is decimal m)
            return System.Convert.ToInt32(m);
        return value;
    }
}

public static class VPetSolution
{
    private static IDialogStorageFolder? _baseFolder;

    /// <summary>
    /// 当前根目录文件夹
    /// </summary>
    public static IDialogStorageFolder? BaseFolder
    {
        get => _baseFolder;
        set
        {
            if (ReferenceEquals(_baseFolder, value))
                return;
            _baseFolder?.Dispose();
            _baseFolder = value;
            BaseDirectory = value?.GetDisplayPath() ?? string.Empty;
        }
    }

    /// <summary>
    /// 当前目录显示文本
    /// </summary>
    public static string BaseDirectory { get; private set; } = string.Empty;

    /// <summary>
    /// 通过本地路径设置根目录
    /// </summary>
    /// <param name="directoryPath">本地目录路径</param>
    public static void SetLocalBaseDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || Directory.Exists(directoryPath) is false)
            return;
        BaseFolder = new DesktopDialogStorageFolder(directoryPath);
    }
}

/// <summary>
/// 跨平台对话框存储扩展方法
/// </summary>
public static class DialogStorageExtensions
{
    /// <summary>
    /// 获取存储项的本地路径, 不支持本地路径的平台返回 null
    /// </summary>
    /// <param name="item">存储项</param>
    /// <returns>本地路径或 null</returns>
    public static string? TryGetLocalPath(this IDialogStorageItem item)
    {
        return item switch
        {
            AvaloniaDialogStorageFolder folder => folder.Source.TryGetLocalPath(),
            AvaloniaDialogStorageFile file => file.Source.TryGetLocalPath(),
            DesktopDialogStorageItem desktopItem => desktopItem.LocalPath,
            _ => item.Path.IsAbsoluteUri && item.Path.IsFile ? item.Path.LocalPath : null,
        };
    }

    /// <summary>
    /// 获取存储项用于展示的路径或名称
    /// </summary>
    /// <param name="item">存储项</param>
    /// <returns>展示文本</returns>
    public static string GetDisplayPath(this IDialogStorageItem item)
    {
        if (item.TryGetLocalPath() is { Length: > 0 } localPath)
            return localPath;
        return item.Name;
    }

    /// <summary>
    /// 获取指定名称的子文件夹
    /// </summary>
    /// <param name="folder">父文件夹</param>
    /// <param name="name">子文件夹名称</param>
    /// <returns>子文件夹或 null</returns>
    public static async Task<IDialogStorageFolder?> TryGetFolderAsync(
        this IDialogStorageFolder folder,
        string name
    )
    {
        if (folder is AvaloniaDialogStorageFolder avaloniaFolder)
        {
            var subFolder = await avaloniaFolder.Source.GetFolderAsync(name);
            if (subFolder is not null)
                return subFolder.ToDialog();
        }
        else if (folder is DesktopDialogStorageFolder desktopFolder)
        {
            var subPath = Path.Combine(desktopFolder.LocalPath, name);
            if (Directory.Exists(subPath))
                return new DesktopDialogStorageFolder(subPath);
            return null;
        }

        await foreach (var item in folder.GetItemsAsync())
        {
            if (
                item is IDialogStorageFolder sub
                && string.Equals(sub.Name, name, StringComparison.OrdinalIgnoreCase)
            )
            {
                return sub;
            }
            item.Dispose();
        }
        return null;
    }

    /// <summary>
    /// 获取指定名称的子文件
    /// </summary>
    /// <param name="folder">父文件夹</param>
    /// <param name="name">文件名</param>
    /// <returns>子文件或 null</returns>
    public static async Task<IDialogStorageFile?> TryGetFileAsync(
        this IDialogStorageFolder folder,
        string name
    )
    {
        if (folder is AvaloniaDialogStorageFolder avaloniaFolder)
        {
            var subFile = await avaloniaFolder.Source.GetFileAsync(name);
            if (subFile is not null)
                return subFile.ToDialog();
        }
        else if (folder is DesktopDialogStorageFolder desktopFolder)
        {
            var subPath = Path.Combine(desktopFolder.LocalPath, name);
            if (File.Exists(subPath))
                return new DesktopDialogStorageFile(subPath);
            return null;
        }

        await foreach (var item in folder.GetItemsAsync())
        {
            if (
                item is IDialogStorageFile file
                && string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase)
            )
            {
                return file;
            }
            item.Dispose();
        }
        return null;
    }

    /// <summary>
    /// 异步读取文件的全部文本内容
    /// </summary>
    /// <param name="file">存储文件</param>
    /// <returns>文本内容</returns>
    public static async Task<string> ReadAllTextAsync(this IDialogStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true
        );
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// 异步将文本内容覆写到文件
    /// </summary>
    /// <param name="file">存储文件</param>
    /// <param name="content">文本内容</param>
    public static async Task WriteAllTextAsync(this IDialogStorageFile file, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        await using var stream = await file.OpenWriteAsync();
        if (stream.CanSeek)
        {
            stream.Position = 0;
            stream.SetLength(0);
        }
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
    }
}

/// <summary>
/// 响应式布局附加属性辅助类
/// </summary>
public static class ResponsiveHelper
{
    /// <summary>
    /// 默认窄屏断点宽度
    /// </summary>
    public const double DefaultBreakpoint = 720.0;

    /// <summary>
    /// 是否启用响应式监听
    /// </summary>
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "Enable",
            typeof(ResponsiveHelper),
            defaultValue: false
        );

    /// <summary>
    /// 窄屏断点宽度阈值
    /// </summary>
    public static readonly AttachedProperty<double> BreakpointProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "Breakpoint",
            typeof(ResponsiveHelper),
            defaultValue: DefaultBreakpoint
        );

    /// <summary>
    /// 当前是否处于紧凑 (窄屏) 模式
    /// </summary>
    public static readonly AttachedProperty<bool> IsCompactProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "IsCompact",
            typeof(ResponsiveHelper),
            defaultValue: false,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
        );

    static ResponsiveHelper()
    {
        EnableProperty.Changed.AddClassHandler<Control>(OnEnableChanged);
        BreakpointProperty.Changed.AddClassHandler<Control>(OnBreakpointChanged);
    }

    public static bool GetEnable(Control element) => element.GetValue(EnableProperty);

    public static void SetEnable(Control element, bool value) =>
        element.SetValue(EnableProperty, value);

    public static double GetBreakpoint(Control element) => element.GetValue(BreakpointProperty);

    public static void SetBreakpoint(Control element, double value) =>
        element.SetValue(BreakpointProperty, value);

    public static bool GetIsCompact(Control element) => element.GetValue(IsCompactProperty);

    public static void SetIsCompact(Control element, bool value) =>
        element.SetValue(IsCompactProperty, value);

    private static void OnEnableChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            control.SizeChanged += OnControlSizeChanged;
            control.Loaded += OnControlLoaded;
            UpdateState(control, control.Bounds.Width);
        }
        else
        {
            control.SizeChanged -= OnControlSizeChanged;
            control.Loaded -= OnControlLoaded;
        }
    }

    private static void OnBreakpointChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (GetEnable(control))
            UpdateState(control, control.Bounds.Width);
    }

    private static void OnControlLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Control control)
            UpdateState(control, control.Bounds.Width);
    }

    private static void OnControlSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is Control control)
            UpdateState(control, e.NewSize.Width);
    }

    private static void UpdateState(Control control, double width)
    {
        if (width <= 0)
            return;

        var isCompact = width < GetBreakpoint(control);
        SetIsCompact(control, isCompact);
        control.Classes.Set("compact", isCompact);
        control.Classes.Set("narrow", isCompact);
        control.Classes.Set("expanded", isCompact is false);
        control.Classes.Set("wide", isCompact is false);
    }
}

/// <summary>
/// 无滚动条自动往返滚动附加属性辅助类
/// </summary>
public static class AutoScrollHelper
{
    private sealed class AutoScrollState
    {
        public DispatcherTimer? Timer;
        public double PauseSeconds;
        public int Direction = 1;
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        ScrollViewer,
        AutoScrollState
    > States = new();

    /// <summary>
    /// 是否启用无滚动条自动往返滚动
    /// </summary>
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, bool>(
            "Enable",
            typeof(AutoScrollHelper),
            defaultValue: false
        );

    /// <summary>
    /// 自动滚动速度 (像素/秒)
    /// </summary>
    public static readonly AttachedProperty<double> SpeedProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, double>(
            "Speed",
            typeof(AutoScrollHelper),
            defaultValue: 45.0
        );

    /// <summary>
    /// 首尾停顿时间 (秒)
    /// </summary>
    public static readonly AttachedProperty<double> PauseSecondsProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, double>(
            "PauseSeconds",
            typeof(AutoScrollHelper),
            defaultValue: 1.5
        );

    static AutoScrollHelper()
    {
        EnableProperty.Changed.AddClassHandler<ScrollViewer>(OnEnableChanged);
    }

    public static bool GetEnable(ScrollViewer element) => element.GetValue(EnableProperty);

    public static void SetEnable(ScrollViewer element, bool value) =>
        element.SetValue(EnableProperty, value);

    public static double GetSpeed(ScrollViewer element) => element.GetValue(SpeedProperty);

    public static void SetSpeed(ScrollViewer element, double value) =>
        element.SetValue(SpeedProperty, value);

    public static double GetPauseSeconds(ScrollViewer element) =>
        element.GetValue(PauseSecondsProperty);

    public static void SetPauseSeconds(ScrollViewer element, double value) =>
        element.SetValue(PauseSecondsProperty, value);

    private static void OnEnableChanged(
        ScrollViewer scrollViewer,
        AvaloniaPropertyChangedEventArgs e
    )
    {
        if (e.NewValue is true)
        {
            scrollViewer.HorizontalScrollBarVisibility = Avalonia
                .Controls
                .Primitives
                .ScrollBarVisibility
                .Hidden;
            scrollViewer.VerticalScrollBarVisibility = Avalonia
                .Controls
                .Primitives
                .ScrollBarVisibility
                .Disabled;
            scrollViewer.AttachedToVisualTree += OnAttachedToVisualTree;
            scrollViewer.DetachedFromVisualTree += OnDetachedFromVisualTree;
            if (TopLevel.GetTopLevel(scrollViewer) is not null)
                Start(scrollViewer);
        }
        else
        {
            scrollViewer.AttachedToVisualTree -= OnAttachedToVisualTree;
            scrollViewer.DetachedFromVisualTree -= OnDetachedFromVisualTree;
            Stop(scrollViewer);
        }
    }

    private static void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
            Start(scrollViewer);
    }

    private static void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
            Stop(scrollViewer);
    }

    private static void Start(ScrollViewer scrollViewer)
    {
        var state = States.GetOrCreateValue(scrollViewer);
        if (state.Timer is not null)
            return;

        state.Direction = 1;
        state.PauseSeconds = GetPauseSeconds(scrollViewer);
        state.Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        state.Timer.Tick += (_, _) => OnTick(scrollViewer, state);
        state.Timer.Start();
    }

    private static void Stop(ScrollViewer scrollViewer)
    {
        if (States.TryGetValue(scrollViewer, out var state) is false || state.Timer is null)
            return;

        state.Timer.Stop();
        state.Timer = null;
    }

    private static void OnTick(ScrollViewer scrollViewer, AutoScrollState state)
    {
        var maxOffset = scrollViewer.Extent.Width - scrollViewer.Viewport.Width;
        if (maxOffset <= 1)
        {
            if (scrollViewer.Offset.X != 0)
                scrollViewer.Offset = new Vector(0, 0);
            state.Direction = 1;
            state.PauseSeconds = GetPauseSeconds(scrollViewer);
            return;
        }

        if (scrollViewer.IsPointerOver)
            return;

        const double dt = 0.03;
        if (state.PauseSeconds > 0)
        {
            state.PauseSeconds -= dt;
            return;
        }

        var speed = GetSpeed(scrollViewer);
        var nextX = scrollViewer.Offset.X + (state.Direction * speed * dt);
        if (nextX >= maxOffset)
        {
            nextX = maxOffset;
            state.Direction = -1;
            state.PauseSeconds = GetPauseSeconds(scrollViewer);
        }
        else if (nextX <= 0)
        {
            nextX = 0;
            state.Direction = 1;
            state.PauseSeconds = GetPauseSeconds(scrollViewer);
        }

        scrollViewer.Offset = new Vector(nextX, 0);
    }
}
