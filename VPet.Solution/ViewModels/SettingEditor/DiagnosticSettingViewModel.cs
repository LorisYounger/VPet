using HanumanInstitute.MvvmDialogs;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class DiagnosticSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public DiagnosticSettingViewModel(IDialogService dialogService)
        : base(dialogService) { }

    public SettingModel Setting { get; set; } = null!;

    public DiagnosticSettingModel DiagnosticSetting => Setting.DiagnosticSetting;
}
