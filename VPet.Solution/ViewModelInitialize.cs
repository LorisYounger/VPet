using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.Wpf;
using HKW.WPF.Extensions;
using Microsoft.Extensions.Logging;
using Panuon.WPF.UI;
using ReactiveUI.Builder;
using Splat;
using VPet.Solution.ViewModels;
using VPet.Solution.ViewModels.SettingEditor;
using VPet.Solution.Views;
using VPet.Solution.Views.SettingEditor;

namespace VPet.Solution;

internal sealed class ViewModelInitializer : IDisposable
{
    public static ViewModelInitializer Instance { get; } = new();

    public ViewModelInitializer()
    {
        Resolver = new ModernDependencyResolver();
        try
        {
            RxAppBuilder.EnsureInitialized();
        }
        catch
        {
            RxAppBuilder.CreateReactiveUIBuilder().WithWpf().BuildApp();
            ModeDetector.OverrideModeDetector(Splat.ModeDetection.Mode.Run);
        }
        ViewLocator = new();
    }

    private ViewLocator ViewLocator { get; }

    private ModernDependencyResolver Resolver { get; }

    public IReadonlyDependencyResolver Initialize()
    {
        //Directory.CreateDirectory(NativeData.ProgramBaseDirectory);
        //NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(
        //    NativeResources.CreateFileWhenNotExists(NativeResources.NLogConfig)
        //);
        Resolver.RegisterLazySingleton<IDialogService>(() =>
            new DialogService(
                new DialogManagerX(viewLocator: ViewLocator, dialogFactory: new DialogFactory()),
                viewModelFactory: x => Resolver.GetService(x)
            )
        );

        ViewLocator.Register<MainViewModel, MainWindow>();
        SplatRegistrations.Register<MainViewModel>();

        ViewLocator.Register<SettingViewModel, SettingWindow>();
        SplatRegistrations.Register<SettingViewModel>();
        ViewLocator.Register<GraphicsSettingViewModel, GraphicsSettingView>();
        SplatRegistrations.Register<GraphicsSettingViewModel>();
        ViewLocator.Register<SystemSettingViewModel, SystemSettingView>();
        SplatRegistrations.Register<SystemSettingViewModel>();
        ViewLocator.Register<DiagnosticSettingViewModel, DiagnosticSettingView>();
        SplatRegistrations.Register<DiagnosticSettingViewModel>();
        ViewLocator.Register<InteractiveSettingViewModel, InteractiveSettingView>();
        SplatRegistrations.Register<InteractiveSettingViewModel>();
        ViewLocator.Register<CustomizedSettingViewModel, CustomizedSettingView>();
        SplatRegistrations.Register<CustomizedSettingViewModel>();
        ViewLocator.Register<ModSettingViewModel, ModSettingView>();
        SplatRegistrations.Register<ModSettingViewModel>();

        SplatRegistrations.SetupIOC(Resolver);
        return Resolver;
    }

    private bool _isDisposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;
        if (disposing)
        {
            Resolver.Dispose();
        }
        _isDisposed = true;
    }
}

public class DialogManagerX : DialogManager
{
    /// <inheritdoc cref="DialogManager.DialogManager(IViewLocator?, IDialogFactory?, ILogger{DialogManager}?, Dispatcher?)"/>
    public DialogManagerX(
        IViewLocator? viewLocator = null,
        IDialogFactory? dialogFactory = null,
        ILogger<DialogManager>? logger = null,
        Dispatcher? dispatcher = null
    )
        : base(viewLocator, dialogFactory, logger, dispatcher) { }

    private static IEnumerable<Window> Windows => Application.Current.Windows.Cast<Window>();

    public override IView? FindViewByViewModel(INotifyPropertyChanged viewModel)
    {
        var view = base.FindViewByViewModel(viewModel);
        if (view is not null)
            return view;
        var window = Windows.First(w => w.IsActive);
        var c = window.FindView(viewModel);
        if (c is not null)
            return window.AsWrapper();
        throw new NotImplementedException();
    }
}

public class ViewLocator : StrongViewLocator { }

public static class MVVMDialogExtensions
{
    private static readonly Dictionary<Type, INotifyPropertyChanged> _displayedViewModelByType = [];
    extension(IDialogService dialogService)
    {
        /// <summary>
        /// 显示单例
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
            if (_displayedViewModelByType.TryGetValue(typeof(TViewModel), out var value))
            {
                var view = dialogService.DialogManager.FindViewByViewModel(value);
                if (view?.RefObj is Window window)
                {
                    window.RestoredAndAlert();
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
        /// 关闭单例
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

        /// <summary>
        /// 显示消息框
        /// </summary>
        /// <param name="dialogService">对话框服务器</param>
        /// <param name="ownerViewModel">所有者视图模型</param>
        /// <param name="text">消息</param>
        /// <param name="title">标题</param>
        /// <param name="button">按钮</param>
        /// <param name="icon">图标</param>
        /// <param name="defaultResult">默认结果</param>
        /// <returns>结果</returns>
        public bool? ShowMessageBox(
            INotifyPropertyChanged ownerViewModel,
            string text,
            string title = "",
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton button =
                HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton.Ok,
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxImage icon =
                HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxImage.None
        )
        {
            var owner = dialogService.DialogManager.FindViewByViewModel(ownerViewModel)!;
            return MessageBoxX
                .Show((Window)owner.RefObj, text, title, button.ToWPFButton(), icon.ToPUIIcon())
                .ToResult();
        }
    }

    /// <summary>
    /// 转换为结果
    /// </summary>
    /// <param name="result">提示框结果</param>
    /// <param name="defaultResult">默认结果</param>
    /// <returns>结果</returns>
    public static bool? ToResult(this MessageBoxResult result)
    {
        return result switch
        {
            MessageBoxResult.OK => true,
            MessageBoxResult.Yes => true,
            MessageBoxResult.No => false,
            MessageBoxResult.Cancel => null,
            _ => null,
        };
    }

    /// <summary>
    /// 转换为PUI图标
    /// </summary>
    /// <param name="icon">图标</param>
    /// <returns>PUI图标</returns>
    public static MessageBoxIcon ToPUIIcon(
        this HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxImage icon
    )
    {
        return icon switch
        {
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxImage.None =>
                MessageBoxIcon.None,
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxImage.Information =>
                MessageBoxIcon.Info,
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxImage.Warning =>
                MessageBoxIcon.Warning,
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxImage.Error =>
                MessageBoxIcon.Error,
            _ => MessageBoxIcon.None,
        };
    }

    /// <summary>
    /// 转换为WPF按钮
    /// </summary>
    /// <param name="button">MVVM窗口按钮</param>
    /// <returns>WPF按钮</returns>
    public static MessageBoxButton ToWPFButton(
        this HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton button
    )
    {
        return button switch
        {
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton.Ok =>
                MessageBoxButton.OK,
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton.OkCancel =>
                MessageBoxButton.OKCancel,
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton.YesNo =>
                MessageBoxButton.YesNo,
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton.YesNoCancel =>
                MessageBoxButton.YesNoCancel,
            _ => MessageBoxButton.OK,
        };
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
    /// 载入图片到流
    /// </summary>
    /// <param name="imagePath">图片路径</param>
    /// <returns>图片</returns>
    public static BitmapImage LoadImageToStream(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || File.Exists(imagePath) is false)
            return null!;
        BitmapImage bitmapImage = new();
        bitmapImage.BeginInit();
        try
        {
            bitmapImage.StreamSource = new StreamReader(imagePath).BaseStream;
        }
        finally
        {
            bitmapImage.EndInit();
        }
        return bitmapImage;
    }

    /// <summary>
    /// 载入图片至内存流
    /// </summary>
    /// <param name="imagePath">图片路径</param>
    /// <returns></returns>
    public static BitmapImage LoadImageToMemoryStream(string imagePath)
    {
        BitmapImage bitmapImage = new();
        bitmapImage.BeginInit();
        try
        {
            var bytes = File.ReadAllBytes(imagePath);
            bitmapImage.StreamSource = new MemoryStream(bytes);
            bitmapImage.DecodePixelWidth = DecodePixelWidth;
        }
        finally
        {
            bitmapImage.EndInit();
        }
        return bitmapImage;
    }

    /// <summary>
    /// 载入图片至内存流
    /// </summary>
    /// <param name="imageStream">图片流</param>
    /// <returns></returns>
    public static BitmapImage LoadImageToMemoryStream(Stream imageStream)
    {
        BitmapImage bitmapImage = new();
        bitmapImage.BeginInit();
        try
        {
            bitmapImage.StreamSource = imageStream;
            bitmapImage.DecodePixelWidth = DecodePixelWidth;
        }
        finally
        {
            bitmapImage.EndInit();
        }
        return bitmapImage;
    }

    /// <summary>
    /// 打开文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static void OpenLink(string filePath)
    {
        System
            .Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true }
            )
            ?.Close();
    }

    /// <summary>
    /// 从资源管理器打开文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static void OpenFileFromExplorer(string filePath)
    {
        System
            .Diagnostics.Process.Start("Explorer", $"/select,{Path.GetFullPath(filePath)}")
            ?.Close();
    }
}
