using System.Windows.Controls;
using VPet.Solution.ViewModels.SaveViewer;

namespace VPet.Solution.Views.SaveViewer;

/// <summary>
/// SaveStatisticPage.xaml 的交互逻辑
/// </summary>
public partial class SaveStatisticPage : Page
{
    public SaveStatisticPageVM ViewModel => (SaveStatisticPageVM)DataContext;

    public SaveStatisticPage()
    {
        InitializeComponent();
        this.SetViewModel<SaveStatisticPageVM>();
    }
}
