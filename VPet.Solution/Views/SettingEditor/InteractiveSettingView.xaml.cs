using System.Windows;
using System.Windows.Controls;
using HKW.HKWUtils.Extensions;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// InteractiveSettingsView.xaml 的交互逻辑
/// </summary>
public partial class InteractiveSettingView : UserControl
{
    public InteractiveSettingViewModel? ViewModel
    {
        get => (InteractiveSettingViewModel)DataContext;
        set => DataContext = value;
    }

    public InteractiveSettingView()
    {
        InitializeComponent();
    }
}
