using CommunityToolkit.Mvvm.Input;
using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class DiagnosticSettingViewModel : DisposableViewModel, ISubSettingViewModel
{
    public SettingModel Setting { get; set; } = null!;

    public DiagnosticSettingModel DiagnosticSetting => Setting.DiagnosticSetting;

    [RelayCommand]
    public static void HyperMoreInfo()
    {
        NativeUtils.Launch("https://www.exlb.net/Diagnosis");
    }
}
