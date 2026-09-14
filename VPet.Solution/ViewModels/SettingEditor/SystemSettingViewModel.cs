using HanumanInstitute.MvvmDialogs;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class SystemSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public SystemSettingViewModel(IDialogService dialogService)
        : base(dialogService) { }

    public SettingModel Setting { get; set; }

    public SystemSettingModel SystemSetting => Setting.SystemSetting;
}
