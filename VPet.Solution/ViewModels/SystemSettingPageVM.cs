using HKW.HKWUtils.Observable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet.Solution.Models;

namespace VPet.Solution.ViewModels;

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
        MainWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
    }

    private void Current_PropertyChangedX(MainWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowVM.CurrentSetting))
        {
            SystemSetting = sender.CurrentSetting.SystemSetting;
        }
    }
}
