using System.Windows.Controls;
using Panuon.WPF.UI;
using VPet.Solution.ViewModels.SaveViewer;

namespace VPet.Solution.Views.SaveViewer;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class SaveWindow : WindowX
{
    public SaveWindow()
    {
        InitializeComponent();
    }

    //private readonly Dictionary<SubSettingModelType, UserControl> _settingViewByType = new()
    //{
    //    [SubSettingModelType.Graphics] = new GraphicsSettingView(),
    //    [SubSettingModelType.System] = new SystemSettingView(),
    //    [SubSettingModelType.Interactive] = new InteractiveSettingView(),
    //    [SubSettingModelType.Customized] = new CustomizedSettingView(),
    //    [SubSettingModelType.Diagnostic] = new DiagnosticSettingView(),
    //    [SubSettingModelType.Mod] = new ModSettingView(),
    //};
}
