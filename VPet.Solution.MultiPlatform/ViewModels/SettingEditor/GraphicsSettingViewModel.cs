using CommunityToolkit.Mvvm.ComponentModel;
using HKW.MVVM.SourceGenerator;
using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class GraphicsSettingViewModel : DisposableViewModel, ISubSettingViewModel
{
    [ObservableProperty]
    public SettingModel Setting { get; set; } = null!;

    [NotifyPropertyChangeFrom(nameof(Setting))]
    public GraphicsSettingModel GraphicsSetting => Setting?.GraphicsSetting!;
}

public interface ISubSettingViewModel
{
    public SettingModel Setting { get; set; }
}
