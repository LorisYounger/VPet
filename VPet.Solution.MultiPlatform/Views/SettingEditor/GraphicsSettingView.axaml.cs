using Avalonia.Controls;
using Avalonia.Interactivity;
using HKW.HKWUtils.Drawing;
using VPet.Solution.MultiPlatform.ViewModels.SettingEditor;

namespace VPet.Solution.MultiPlatform.Views.SettingEditor;

/// <summary>
/// GraphicsSettingView.axaml 的交互逻辑
/// </summary>
public partial class GraphicsSettingView : UserControl
{
    public GraphicsSettingViewModel? ViewModel
    {
        get => DataContext as GraphicsSettingViewModel;
        set => DataContext = value;
    }

    public GraphicsSettingView()
    {
        InitializeComponent();
    }

    private void SetCurrentPointToStartPoint_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            var scaling = window.RenderScaling > 0 ? window.RenderScaling : 1;
            ViewModel?.GraphicsSetting.StartRecordPoint.SetValue(
                new Point<double>(window.Position.X / scaling, window.Position.Y / scaling)
            );
        }
    }
}
