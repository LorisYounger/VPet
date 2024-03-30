using System.Windows.Controls;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// ModSettingsPage.xaml 的交互逻辑
/// </summary>
public partial class ModSettingPage : Page
{
    public ModSettingPageVM ViewModel => (ModSettingPageVM)DataContext;

    public ModSettingPage()
    {
        InitializeComponent();
        this.SetViewModel<ModSettingPageVM>();
    }
}
