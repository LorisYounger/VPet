using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using VPet.Solution.MultiPlatform.Models;
using VPet.Solution.MultiPlatform.ViewModels;

namespace VPet.Solution.MultiPlatform.Views;

public partial class SettingView : UserControl
{
    public SettingView()
    {
        InitializeComponent();
        //DataContextChanged += SettingWindow_DataContextChanged;
        //Closing += SettingWindow_Closing;
        //Closed += SettingWindow_Closed;
    }

    public SettingViewModel? ViewModel
    {
        get => DataContext as SettingViewModel;
        set => DataContext = value;
    }

    private void SettingWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (ViewModel?.CurrentSetting?.IsChanged is not true)
            return;
        // TODO: DialogService 关闭设置窗口前提示是否保存未保存的修改
        // var result = ViewModel.DialogService.ShowMessageBox(
        //     ViewModel,
        //     "当前设置未保存, 是否保存".Translate(),
        //     "",
        //     HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton.YesNoCancel
        // );
        // if (result is true)
        // {
        //     ViewModel.CurrentSetting.Save();
        // }
        // else if (result is false)
        // {
        //     ViewModel.CurrentSetting.Reload();
        // }
        // else
        // {
        //     e.Cancel = true;
        // }
    }

    private void LastSubSettingType_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
            return;
        var index = (int)ViewModel.CurrentSubSettingType.Value;
        if (index > 0)
        {
            SubSettingTypes.SelectedIndex--;
            SubSettingTypes.ScrollIntoView(SubSettingTypes.SelectedItem!);
        }
    }

    private void NextSubSettingType_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
            return;
        var index = (int)ViewModel.CurrentSubSettingType.Value;
        if (index < ViewModel.CurrentSubSettingType.InfoDictionary.Count - 1)
        {
            SubSettingTypes.SelectedIndex++;
            SubSettingTypes.ScrollIntoView(SubSettingTypes.SelectedItem!);
        }
    }

    private void SettingWindow_Closed(object? sender, EventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _subscribedViewModel = null;
        }
        _lastSettingModel = null;
        foreach (var view in _settingViewByType.Values)
        {
            if (view.DataContext is ISubSettingViewModel vm)
            {
                vm.Setting = null!;
            }
            view.DataContext = null;
        }
    }

    private readonly Dictionary<SubSettingModelType, UserControl> _settingViewByType = new()
    {
        [SubSettingModelType.Graphics] = new GraphicsSettingView(),
        [SubSettingModelType.System] = new SystemSettingView(),
        [SubSettingModelType.Interactive] = new InteractiveSettingView(),
        [SubSettingModelType.Customized] = new CustomizedSettingView(),
        [SubSettingModelType.Diagnostic] = new DiagnosticSettingView(),
        [SubSettingModelType.Mod] = new ModSettingView(),
    };

    private SettingViewModel? _subscribedViewModel;

    private void SettingWindow_DataContextChanged(object? sender, EventArgs e)
    {
        //if (_subscribedViewModel is not null)
        //{
        //    _subscribedViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        //    _subscribedViewModel = null;
        //}
        //if (ViewModel is null)
        //    return;
        //_subscribedViewModel = ViewModel;
        //ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        //SettingView.Content = GetSettingView(
        //    ViewModel.CurrentSubSettingViewModel,
        //    ViewModel.CurrentSubSettingType
        //);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        //if (ViewModel is null)
        //    return;
        //if (e.PropertyName == nameof(ViewModel.CurrentSubSettingViewModel))
        //{
        //    SettingView.Content = GetSettingView(
        //        ViewModel.CurrentSubSettingViewModel,
        //        ViewModel.CurrentSubSettingType
        //    );
        //}
    }

    private UserControl? GetSettingView(ISubSettingViewModel? viewModel, SubSettingModelType type)
    {
        if (viewModel is null)
            return null;
        var view = _settingViewByType[type];
        view.DataContext = null;
        view.DataContext = viewModel;
        return view;
    }

    private SettingModel? _lastSettingModel;

    private void Settings_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null)
            return;
        if (_lastSettingModel == ViewModel.CurrentSetting)
        {
            Settings.SelectedItem = ViewModel.CurrentSetting;
        }
        else
        {
            _lastSettingModel = ViewModel.CurrentSetting;
        }
    }
}
