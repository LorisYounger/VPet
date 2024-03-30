using System.Windows;
using System.Windows.Controls;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// GraphicsSettingsPage.xaml 的交互逻辑
/// </summary>
public partial class GraphicsSettingPage : Page
{
    public GraphicsSettingPageVM ViewModel => (GraphicsSettingPageVM)DataContext;

    public GraphicsSettingPage()
    {
        InitializeComponent();
        this.SetViewModel<GraphicsSettingPageVM>();
    }

    private void Button_StartPoint_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.GraphicsSetting.StartRecordPoint = new(
            SettingWindow.Instance.Left,
            SettingWindow.Instance.Top
        );
    }
}
