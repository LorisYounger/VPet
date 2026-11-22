using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class InteractiveSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public SettingModel Setting { get; set; } = null!;

    public InteractiveSettingModel InteractiveSetting => Setting.InteractiveSetting;
}
