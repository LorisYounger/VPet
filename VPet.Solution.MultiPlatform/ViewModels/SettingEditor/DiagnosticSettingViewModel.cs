using CommunityToolkit.Mvvm.Input;
using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class DiagnosticSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public SettingModel Setting { get; set; } = null!;

    public DiagnosticSettingModel DiagnosticSetting => Setting.DiagnosticSetting;

    [RelayCommand]
    public static void HyperMoreInfo()
    {
        // TODO: OpenLink 打开诊断说明文档
        // NativeUtils.OpenLink("https://www.exlb.net/Diagnosis");
    }
}
