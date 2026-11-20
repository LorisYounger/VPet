using CommunityToolkit.Mvvm.Input;
using LinePutScript.Localization;
using VPet.Solution.MultiPlatform.Models.SettingEditor;

namespace VPet.Solution.MultiPlatform.ViewModels.SettingEditor;

public partial class SystemSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public SettingModel Setting { get; set; } = null!;

    public SystemSettingModel SystemSetting => Setting.SystemSetting;

    [RelayCommand]
    private void CacheClean()
    {
        Setting.SystemSetting.LastCacheDate = DateTime.MinValue;
        // TODO: DialogService 提示清理缓存指令已下达
        // DialogService.ShowMessageBox(
        //     this,
        //     "清理指令已下达,下次启动桌宠时生效".Translate(),
        //     "提示".Translate(),
        //     MessageBoxButton.Ok,
        //     MessageBoxImage.Information
        // );
    }
}
