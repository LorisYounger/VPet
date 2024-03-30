using System.Windows.Controls;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// CustomizedSettingsPage.xaml 的交互逻辑
/// </summary>
public partial class CustomizedSettingPage : Page
{
    public CustomizedSettingPageVM ViewModel => (CustomizedSettingPageVM)DataContext;

    public CustomizedSettingPage()
    {
        InitializeComponent();
        this.SetViewModel<CustomizedSettingPageVM>();
    }
}
