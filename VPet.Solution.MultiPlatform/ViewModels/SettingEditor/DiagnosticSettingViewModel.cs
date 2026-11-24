using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.MVVM.SourceGenerator;
using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class DiagnosticSettingViewModel : DisposableViewModel, ISubSettingViewModel
{
    [ObservableProperty]
    public SettingModel Setting { get; set; } = null!;

    [NotifyPropertyChangeFrom(nameof(Setting))]
    public DiagnosticSettingModel DiagnosticSetting => Setting?.DiagnosticSetting!;

    [RelayCommand]
    public static void HyperMoreInfo()
    {
        NativeUtils.Launch("https://www.exlb.net/Diagnosis");
    }
}
