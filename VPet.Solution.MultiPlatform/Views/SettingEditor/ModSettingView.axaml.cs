using Avalonia.Controls;
using Avalonia.Interactivity;
using VPet.Solution.MultiPlatform.ViewModels;

namespace VPet.Solution.MultiPlatform.Views;

/// <summary>
/// ModSettingView.axaml 的交互逻辑
/// </summary>
public partial class ModSettingView : UserControl
{
    public ModSettingViewModel? ViewModel
    {
        get => DataContext as ModSettingViewModel;
        set => DataContext = value;
    }

    public ModSettingView()
    {
        InitializeComponent();
    }

    private void ModIsPass_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null || ViewModel.CurrentMod is null)
            return;
        if (ModIsPass.IsChecked is not true)
            return;
        var mod = ViewModel.CurrentMod;
        // TODO: DialogService 提示启用代码插件的安全风险确认框
        // var result = ViewModel.DialogService.ShowMessageBox(
        //     ViewModel,
        //     "是否启用 {0} 的代码插件?\n一经启用,该插件将会允许访问该系统(包括外部系统)的所有数据\n如果您不确定,请先使用杀毒软件查杀检查".Translate(
        //         mod.Name
        //     ),
        //     "启用 {0} 的代码插件?".Translate(mod.Name),
        //     HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton.YesNo,
        //     HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxImage.Warning
        // );
        // if (result is true)
        // {
        //     mod.IsEnabled = true;
        //     mod.IsPass = true;
        // }
        mod.IsEnabled = true;
        mod.IsPass = true;
    }
}
