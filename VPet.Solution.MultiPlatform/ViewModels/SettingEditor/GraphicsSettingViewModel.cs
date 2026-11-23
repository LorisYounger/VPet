using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class GraphicsSettingViewModel : DisposableViewModel, ISubSettingViewModel
{
    public SettingModel Setting { get; set; } = null!;

    public GraphicsSettingModel GraphicsSetting => Setting.GraphicsSetting;
}

public interface ISubSettingViewModel
{
    public SettingModel Setting { get; set; }
}
