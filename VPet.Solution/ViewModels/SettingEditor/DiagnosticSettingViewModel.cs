using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using VPet.Solution.Models.SettingEditor;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class DiagnosticSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public DiagnosticSettingViewModel(IDialogService dialogService)
        : base(dialogService) { }

    public SettingModel Setting { get; set; } = null!;

    public DiagnosticSettingModel DiagnosticSetting => Setting.DiagnosticSetting;

    [RelayCommand]
    public void hyper_moreInfo()
    {
        ExtensionFunction.StartURL("https://www.exlb.net/Diagnosis");
    }
}
