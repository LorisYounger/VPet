using HKW.HKWUtils;
using LinePutScript.Localization.WPF;
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
    public static SettingWindow Instance { get; private set; }
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
        ListBox_Pages.SelectedIndex = 0;
        Instance = this;
    }

    private void SettingWindow_Closing(object sender, CancelEventArgs e)
    {
        if (ViewModel?.CurrentSetting?.IsChanged is true)
        {
            if (ViewModel?.CurrentSetting?.IsChanged is true)
            {
                this.SetCloseState(WindowCloseState.Hidden | WindowCloseState.SkipNext);
                var result = MessageBox.Show(
                    "当前设置未保存 确定要保存吗".Translate(),
                    "",
                    MessageBoxButton.YesNoCancel
                );
                if (result is MessageBoxResult.Yes)
                {
                    ViewModel.CurrentSetting.Save();
                    Close();
                }
                else if (result is MessageBoxResult.No)
                {
                    ViewModel.CurrentSetting.IsChanged = false;
                }
                else if (result is MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
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
