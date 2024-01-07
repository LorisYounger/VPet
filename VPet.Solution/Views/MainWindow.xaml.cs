using HKW.HKWUtils;
using Panuon.WPF.UI;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using VPet.Solution.ViewModels;
using VPet.Solution.Views.SettingEditor;

namespace VPet.Solution.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : WindowX
{
    public MainWindowVM ViewModel => (MainWindowVM)DataContext;

    public SettingWindow SettingWindow { get; set; } = new();

    public MainWindow()
    {
        if (App.IsDone)
        {
            Close();
            return;
        }
        InitializeComponent();
        this.SetViewModel<MainWindowVM>();

        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
        SettingWindow.CloseX();
    }

    private void Button_OpenSettingEditor_Click(object sender, RoutedEventArgs e)
    {
        if (SettingWindow.IsVisible is false)
            SettingWindow.Show();
        SettingWindow.Activate();
    }
}
