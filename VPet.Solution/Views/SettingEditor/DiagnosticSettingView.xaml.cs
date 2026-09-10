using System.Windows.Controls;
using HKW.HKWUtils.Extensions;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.Views.SettingEditor;

/// <summary>
/// DiagnosticSettingsView.xaml 的交互逻辑
/// </summary>
public partial class DiagnosticSettingView : UserControl
{
    public DiagnosticSettingViewModel? ViewModel
    {
        get => (DiagnosticSettingViewModel)DataContext;
        set => DataContext = value;
    }

    public DiagnosticSettingView()
    {
        InitializeComponent();
    }
}
