using System.Windows;
using Panuon.WPF.UI;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using VPet.Solution.ViewModels;

namespace VPet.Solution.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : WindowX, IViewFor<MainViewModel>
{
    public MainViewModel? ViewModel
    {
        get => (MainViewModel)DataContext!;
        set => DataContext = value;
    }
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (MainViewModel)value!;
    }

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Languages.ItemsSource = MainViewModel.AvailableCultures;
        this.Bind(ViewModel, vm => vm.CurrentCulture, v => v.Languages.SelectedItem)
            .DisposeWith(ViewModel!.Disposables);
        this.BindCommand(ViewModel, vm => vm.OpenLocalTextCommand, v => v.OpenLocalText)
            .DisposeWith(ViewModel.Disposables);
        this.BindCommand(ViewModel, vm => vm.FirstStartFailedCommand, v => v.FirstStartFailed)
            .DisposeWith(ViewModel.Disposables);
        this.BindCommand(ViewModel, vm => vm.OpenSettingCommand, v => v.OpenSettingEditor)
            .DisposeWith(ViewModel.Disposables);
    }
}
