using HanumanInstitute.MvvmDialogs;
using HKW.HKWReactiveUI;
using ReactiveUI;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class InteractiveSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public InteractiveSettingViewModel(IDialogService dialogService)
        : base(dialogService) { }

    public SettingModel Setting { get; set; } = null!;

    public InteractiveSettingModel InteractiveSetting => Setting.InteractiveSetting;
}
