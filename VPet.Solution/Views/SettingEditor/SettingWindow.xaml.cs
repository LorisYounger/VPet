using HKW.HKWUtils;
using Panuon.WPF.UI;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingWindow : WindowX
{
    public SettingWindowVM ViewModel => (SettingWindowVM)DataContext;

    public SettingWindow()
    {
        InitializeComponent();
        this.SetViewModel<SettingWindowVM>();
        this.SetCloseState(WindowCloseState.Hidden);

        ListBoxItem_GraphicsSettings.Tag = new GraphicsSettingPage();
        ListBoxItem_SystemSettings.Tag = new SystemSettingPage();
        ListBoxItem_InteractiveSettings.Tag = new InteractiveSettingPage();
        ListBoxItem_CustomizedSettings.Tag = new CustomizedSettingPage();
        ListBoxItem_DiagnosticSettings.Tag = new DiagnosticSettingPage();
        ListBoxItem_ModSettings.Tag = new ModSettingPage();
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
