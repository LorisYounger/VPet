using HKW.HKWUtils.Observable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet.Solution.Models;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public class SystemSettingPageVM : ObservableClass<SystemSettingPageVM>
{
    private SystemSettingModel _systemSetting;
    public SystemSettingModel SystemSetting
    {
        get => _systemSetting;
        set => SetProperty(ref _systemSetting, value);
    }

    public SystemSettingPageVM()
    {
        SettingWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
    }

    private void Current_PropertyChangedX(SettingWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (e.PropertyName == nameof(SettingWindowVM.CurrentSetting))
        {
            SystemSetting = sender.CurrentSetting.SystemSetting;
        }
    }
}
