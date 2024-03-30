using System.Windows.Controls;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// DiagnosticSettingsPage.xaml 的交互逻辑
/// </summary>
public partial class DiagnosticSettingPage : Page
{
    public DiagnosticSettingPageVM ViewModel => (DiagnosticSettingPageVM)DataContext;

    public DiagnosticSettingPage()
    {
        InitializeComponent();
        this.SetViewModel<DiagnosticSettingPageVM>();
    }
}
