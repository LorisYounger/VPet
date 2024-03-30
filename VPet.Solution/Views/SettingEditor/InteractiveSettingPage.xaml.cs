using System.Windows.Controls;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// InteractiveSettingsPage.xaml 的交互逻辑
/// </summary>
public partial class InteractiveSettingPage : Page
{
    public InteractiveSettingPageVM ViewModel => (InteractiveSettingPageVM)DataContext;

    public InteractiveSettingPage()
    {
        InitializeComponent();
        this.SetViewModel<InteractiveSettingPageVM>();
    }
}
