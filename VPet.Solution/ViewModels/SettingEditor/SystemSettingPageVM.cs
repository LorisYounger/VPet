using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public class SystemSettingPageVM : ObservableClass<SystemSettingPageVM>
{
    #region SystemSetting

    private SystemSettingModel _systemSetting;
    public SystemSettingModel SystemSetting
    {
        get => _systemSetting;
        set => SetProperty(ref _systemSetting, value);
    }
    #endregion

    public SystemSettingPageVM()
    {
        SettingWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
    }

    private void Current_PropertyChangedX(SettingWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (
            e.PropertyName == nameof(SettingWindowVM.CurrentSetting)
            && sender.CurrentSetting is not null
        )
        {
            SystemSetting = sender.CurrentSetting.SystemSetting;
        }
    }
}
