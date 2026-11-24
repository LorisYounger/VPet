using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using HKW.MVVM.SourceGenerator;
using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class SystemSettingViewModel : DisposableViewModel, ISubSettingViewModel
{
    [ObservableProperty]
    public SettingModel Setting { get; set; } = null!;

    [NotifyPropertyChangeFrom(nameof(Setting))]
    public SystemSettingModel SystemSetting => Setting?.SystemSetting!;

    [RelayCommand]
    private async Task CacheClean()
    {
        Setting.SystemSetting.LastCacheDate = DateTime.MinValue;

        await DialogService.ShowMessageBoxAsync(
            this,
            "清理指令已下达,下次启动桌宠时生效".Translate(),
            "提示".Translate(),
            MessageBoxButton.Ok,
            MessageBoxImage.Information
        );
    }
}
