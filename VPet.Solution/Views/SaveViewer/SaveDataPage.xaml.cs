using System.Windows.Controls;
using VPet.Solution.ViewModels.SaveViewer;

namespace VPet.Solution.Views.SaveViewer;

/// <summary>
/// SaveDataPage.xaml 的交互逻辑
/// </summary>
public partial class SaveDataPage : Page
{
    public SaveDataPageVM ViewModel => (SaveDataPageVM)DataContext;

    public SaveDataPage()
    {
        InitializeComponent();
        this.SetViewModel<SaveDataPageVM>();
    }
}
