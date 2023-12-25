using HKW.HKWUtils;
using Panuon.WPF.UI;
using System.Windows.Controls;
using VPet.Solution.ViewModels;

namespace VPet.Solution.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : WindowX
{
    public MainWindowVM ViewModel => this.SetViewModel<MainWindowVM>().Value;

    public MainWindow()
    {
        if (App.IsDone)
        {
            Close();
            return;
        }
        InitializeComponent();
        ListBoxItem_GraphicsSettings.Tag = new GraphicsSettingsPage();
        ListBoxItem_SystemSettings.Tag = new SystemSettingsPage();
        ListBoxItem_InteractiveSettings.Tag = new InteractiveSettingsPage();
        ListBoxItem_CustomizedSettings.Tag = new CustomizedSettingsPage();
        ListBoxItem_DiagnosticSettings.Tag = new DiagnosticSettingsPage();
        ListBoxItem_ModSettings.Tag = new ModSettingsPage();
    }

    private void Frame_Main_ContentRendered(object sender, EventArgs e)
    {
        if (sender is not Frame frame)
            return;
        // 清理过时页面
        while (frame.CanGoBack)
            frame.RemoveBackEntry();
        GC.Collect();
    }
}
