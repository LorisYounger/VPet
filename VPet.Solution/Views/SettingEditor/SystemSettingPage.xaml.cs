using System.Windows.Controls;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// SystemSettingsPage.xaml 的交互逻辑
/// </summary>
public partial class SystemSettingPage : Page
{
    public SystemSettingPageVM ViewModel => (SystemSettingPageVM)DataContext;

    public SystemSettingPage()
    {
        InitializeComponent();
        this.SetViewModel<SystemSettingPageVM>();
    }
}
