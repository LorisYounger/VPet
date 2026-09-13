using System.Windows.Controls;
using HKW.HKWUtils.Extensions;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// SystemSettingsView.xaml 的交互逻辑
/// </summary>
public partial class SystemSettingView : UserControl
{
    public SystemSettingViewModel ViewModel
    {
        get => (SystemSettingViewModel)DataContext!;
        set => DataContext = value;
    }

    public SystemSettingView()
    {
        InitializeComponent();
    }
}
