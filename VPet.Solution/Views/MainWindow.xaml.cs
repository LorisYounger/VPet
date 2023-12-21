using Panuon.WPF.UI;

namespace VPet.Solution;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : WindowX
{
    public MainWindow()
    {
        if (App.IsDone)
        {
            Close();
            return;
        }
        InitializeComponent();
    }
}
