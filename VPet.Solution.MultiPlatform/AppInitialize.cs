using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.Avalonia;
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
        //RegisterView<SettingViewModel, SettingWindow>();
        RegisterView<GraphicsSettingViewModel, GraphicsSettingView>();
        RegisterView<SystemSettingViewModel, SystemSettingView>();
        RegisterView<DiagnosticSettingViewModel, DiagnosticSettingView>();
        RegisterView<InteractiveSettingViewModel, InteractiveSettingView>();
        RegisterView<CustomizedSettingViewModel, CustomizedSettingView>();
        RegisterView<ModSettingViewModel, ModSettingView>();

        //RegisterView<SaveViewModel, SaveWindow>();
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
        // 否则开始寻找窗口
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
            && desktop.Windows.FirstOrDefault(window =>
                window.GetVisualDescendants().Any(v => ReferenceEquals(v.DataContext, viewModel))
            )
                is { } window
        )
            return AsWrapper(window);

        return null;
    }
}

public static class MVVMDialogExtensions
{
    private static readonly Dictionary<Type, INotifyPropertyChanged> _displayedViewModelByType = [];

    extension(IDialogService dialogService)
    {
        /// <summary>
        /// 显示单例窗口
        /// </summary>
        /// <typeparam name="TViewModel">视图模型类型</typeparam>
        /// <param name="ownerViewModel">主视图模型</param>
        /// <param name="viewModel">视图模型</param>
        public void ShowInstance<TViewModel>(
            INotifyPropertyChanged? ownerViewModel,
            TViewModel? viewModel = null
        )
            where TViewModel : class, INotifyPropertyChanged
        {
            // TODO: 接入跨平台 DialogService 单例窗口显示逻辑
            if (_displayedViewModelByType.TryGetValue(typeof(TViewModel), out var value))
            {
                var view = dialogService.DialogManager.FindViewByViewModel(value);
                if (view?.RefObj is Window window)
                {
                    if (window.WindowState == WindowState.Minimized)
                        window.WindowState = WindowState.Normal;
                    window.Activate();
                }
                return;
            }
            _displayedViewModelByType[typeof(TViewModel)] = (
                viewModel ??= dialogService.CreateViewModel<TViewModel>()
            );
            dialogService.Show(ownerViewModel, viewModel);
            var v = dialogService.DialogManager.FindViewByViewModel(viewModel);
            if (v?.RefObj is Window w)
                w.Closed += W_Closed;

            void W_Closed(object? sender, EventArgs e)
            {
                _displayedViewModelByType.Remove(typeof(TViewModel));
                w.Closed -= W_Closed;
            }
        }

        /// <summary>
        /// 关闭单例窗口
        /// </summary>
        /// <typeparam name="TViewModel">视图模型类型</typeparam>
        public void CloseInstance<TViewModel>()
            where TViewModel : INotifyPropertyChanged
        {
            if (_displayedViewModelByType.Remove(typeof(TViewModel), out var viewModel) is false)
                return;
            dialogService.Close(viewModel);
        }

        public TViewModel CreateViewModel<TViewModel>(Action<TViewModel> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            var vm = dialogService.CreateViewModel<TViewModel>();
            action(vm);
            return vm;
        }
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
    /// 打开链接或文件
    /// </summary>
    /// <param name="filePath">文件或链接路径</param>
    public static void OpenLink(string filePath)
    {
        // TODO: 跨平台实现 OpenLink
    }

    /// <summary>
    /// 从文件管理器定位并打开文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static void OpenFileFromExplorer(string filePath)
    {
        // TODO: 跨平台实现 OpenFileFromExplorer
    }
}

/// <summary>
/// 面板按索引控制子元素可见性的附加属性辅助类
/// </summary>
public static class PanelHelper
{
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
