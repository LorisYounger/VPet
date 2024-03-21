using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using HKW.HKWUtils;
using Panuon.WPF.UI;
using VPet.Solution.ViewModels.SaveViewer;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SaveViewer;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class SaveWindow : WindowX
{
    public static SaveWindow Instance { get; private set; }
    public SaveWindowVM ViewModel => (SaveWindowVM)DataContext;

    public SaveWindow()
    {
        InitializeComponent();
        this.SetViewModel<SaveWindowVM>();
        this.SetCloseState(WindowCloseState.Hidden);

        ListBoxItem_SaveData.Tag = new SaveDataPage();
        ListBoxItem_SaveStatistic.Tag = new SaveStatisticPage();
        ListBox_Pages.SelectedIndex = 0;
        Instance = this;
    }

    private void Frame_Main_ContentRendered(object? sender, EventArgs e)
    {
        if (sender is not Frame frame)
            return;
        // 清理过时页面
        while (frame.CanGoBack)
            frame.RemoveBackEntry();
        GC.Collect();
    }
}
