using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using HKW.HKWUtils;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.ViewModels;
using VPet.Solution.Views.SaveViewer;
using VPet.Solution.Views.SettingEditor;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : WindowX
{
    public MainWindowVM ViewModel => (MainWindowVM)DataContext;

    public SettingWindow SettingWindow { get; } = new();
    public SaveWindow SaveWindow { get; } = new();

    public MainWindow()
    {        
        InitializeComponent();
        this.SetViewModel<MainWindowVM>();
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        foreach (var mod in ModSettingModel.LocalMods)
            mod.Value.Image?.CloseStream();
        SettingWindow.CloseX();
        SaveWindow.CloseX();
    }

    private void Button_OpenSettingEditor_Click(object? sender, RoutedEventArgs e)
    {
        SettingWindow.ShowOrActivate();
    }

    private void Button_OpenSaveViewer_Click(object? sender, RoutedEventArgs e)
    {
        SaveWindow.ShowOrActivate();
    }
}
