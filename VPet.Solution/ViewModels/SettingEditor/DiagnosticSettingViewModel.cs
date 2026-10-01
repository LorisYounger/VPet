using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using VPet_Simulator.Windows.Interface;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class DiagnosticSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public SettingModel Setting { get; set; } = null!;

    public DiagnosticSettingModel DiagnosticSetting => Setting.DiagnosticSetting;

    [RelayCommand]
    public static void HyperMoreInfo()
    {
        ExtensionFunction.StartURL("https://www.exlb.net/Diagnosis");
    }
}
