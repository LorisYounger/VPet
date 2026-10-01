using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using LinePutScript.Localization.WPF;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class SystemSettingViewModel : ViewModelBase, ISubSettingViewModel
{
    public SettingModel Setting { get; set; }

    public SystemSettingModel SystemSetting => Setting.SystemSetting;

    [RelayCommand]
    private void CacheClean()
    {
        Setting.SystemSetting.LastCacheDate = DateTime.MinValue;
        DialogService.ShowMessageBox(
            this,
            "清理指令已下达,下次启动桌宠时生效".Translate(),
            "提示".Translate(),
            MessageBoxButton.Ok,
            MessageBoxImage.Information
        );
    }
}
