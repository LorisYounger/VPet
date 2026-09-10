using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using HanumanInstitute.MvvmDialogs;
using HKW.HKWUtils;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using ReactiveUI;
using ReactiveUI.Primitives;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingWindow : WindowX, IViewFor<SettingViewModel>
{
    public SettingViewModel? ViewModel
    {
        get => (SettingViewModel)DataContext!;
        set => DataContext = value;
    }
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (SettingViewModel)value!;
    }

    public SettingWindow()
    {
        InitializeComponent();
        DataContextChanged += SettingWindow_DataContextChanged;
        Closing += SettingWindow_Closing;
        Closed += SettingWindow_Closed;
    }

    private void SettingWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (ViewModel?.CurrentSetting?.IsChanged is not true)
            return;
        var result = ViewModel.DialogService.ShowMessageBox(
            ViewModel,
            "当前设置未保存, 是否保存".Translate(),
            "",
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton.YesNoCancel
        );
        if (result is true)
        {
            ViewModel.CurrentSetting.Save();
        }
        else if (result is false)
        {
            ViewModel.CurrentSetting.Reload();
        }
        else
        {
            e.Cancel = true;
        }
    }

    private void LastSubSettingType_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
            return;
        var index = (int)ViewModel.CurrentSubSettingType.Value;
        if (index > 0)
        {
            SubSettingTypes.SelectedIndex--;
            SubSettingTypes.ScrollIntoView(SubSettingTypes.SelectedItem);
        }
    }

    private void NextSubSettingType_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
            return;
        var index = (int)ViewModel.CurrentSubSettingType.Value;
        if (index < ViewModel.CurrentSubSettingType.InfoDictionary.Count - 1)
        {
            SubSettingTypes.SelectedIndex++;
            SubSettingTypes.ScrollIntoView(SubSettingTypes.SelectedItem);
        }
    }

    private void SettingWindow_Closed(object? sender, EventArgs e)
    {
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

    private void SettingWindow_DataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (ViewModel is null)
            return;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        SettingView.Content = GetSettingView(
            ViewModel.CurrentSubSettingViewModel,
            ViewModel.CurrentSubSettingType
        );
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ViewModel is null)
            return;
        if (e.PropertyName == nameof(ViewModel.CurrentSubSettingViewModel))
        {
            SettingView.Content = GetSettingView(
                ViewModel.CurrentSubSettingViewModel,
                ViewModel.CurrentSubSettingType
            );
        }
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

    SettingModel? _lastSettingModel;

    private void Settings_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
