using VPet.Solution.MultiPlatform.Models.SettingEditor;

namespace VPet.Solution.MultiPlatform.ViewModels.SettingEditor;

public partial class InteractiveSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public SettingModel Setting { get; set; } = null!;

    public InteractiveSettingModel InteractiveSetting => Setting.InteractiveSetting;
}
