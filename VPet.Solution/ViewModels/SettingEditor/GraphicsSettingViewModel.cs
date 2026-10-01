using HanumanInstitute.MvvmDialogs;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class GraphicsSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public SettingModel Setting { get; set; } = null!;

    public GraphicsSettingModel GraphicsSetting => Setting.GraphicsSetting;
}

public interface ISubSettingViewModel
{
    public SettingModel Setting { get; set; }
}
