using System.Windows;
using System.Windows.Controls;
using HKW.HKWUtils.Drawing;
using HKW.HKWUtils.Extensions;
using HKW.WPF.Extensions;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// GraphicsSettingsView.xaml 的交互逻辑
/// </summary>
public partial class GraphicsSettingView : UserControl
{
    public GraphicsSettingViewModel? ViewModel
    {
        get => (GraphicsSettingViewModel)DataContext!;
        set => DataContext = value;
    }

    public GraphicsSettingView()
    {
        InitializeComponent();
    }

    private void SetCurrentPointToStartPoint_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.FindVisuaParent<Window>();
        ViewModel?.GraphicsSetting.StartRecordPoint.SetValue(window.Left, window.Top);
    }
}
