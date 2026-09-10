using System.Windows;
using System.Windows.Controls;
using HKW.HKWUtils.Extensions;
using LinePutScript.Localization.WPF;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// ModSettingsView.xaml 的交互逻辑
/// </summary>
public partial class ModSettingView : UserControl
{
    public ModSettingViewModel ViewModel
    {
        get => (ModSettingViewModel)DataContext;
        set => DataContext = value;
    }

    public ModSettingView()
    {
        InitializeComponent();
    }

    private void ModIsPass_Checked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
            return;
        var mod = ViewModel.CurrentMod;
        var result = ViewModel.DialogService.ShowMessageBox(
            ViewModel,
            "是否启用 {0} 的代码插件?\n一经启用,该插件将会允许访问该系统(包括外部系统)的所有数据\n如果您不确定,请先使用杀毒软件查杀检查".Translate(
                mod.Name
            ),
            "启用 {0} 的代码插件?".Translate(mod.Name),
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxButton.YesNo,
            HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxImage.Warning
        );
        if (result is true)
        {
            mod.IsEnabled = true;
            mod.IsPass = true;
        }
    }
}
