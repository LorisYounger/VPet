using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet.Solution.Models;

namespace VPet.Solution.ViewModels;

public class InteractiveSettingPageVM : ObservableClass<InteractiveSettingPageVM>
{
    private InteractiveSettingModel _systemSetting;
    public InteractiveSettingModel InteractiveSetting
    {
        get => _systemSetting;
        set => SetProperty(ref _systemSetting, value);
    }

    public InteractiveSettingPageVM()
    {
        MainWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
    }

    private void Current_PropertyChangedX(MainWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowVM.CurrentSetting))
        {
            InteractiveSetting = sender.CurrentSetting.InteractiveSetting;
        }
    }
}
