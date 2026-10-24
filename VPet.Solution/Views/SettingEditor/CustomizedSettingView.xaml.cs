using System.Windows;
using System.Windows.Controls;
using HKW.HKWUtils.Extensions;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// CustomizedSettingsView.xaml 的交互逻辑
/// </summary>
public partial class CustomizedSettingView : UserControl
{
    public CustomizedSettingViewModel? ViewModel
    {
        get => (CustomizedSettingViewModel)DataContext;
        set => DataContext = value;
    }

    public CustomizedSettingView()
    {
        InitializeComponent();
    }
}
