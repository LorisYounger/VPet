using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet.Solution.Models;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

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
        SettingWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
    }

    private void Current_PropertyChangedX(SettingWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (
            e.PropertyName == nameof(SettingWindowVM.CurrentSetting)
            && sender.CurrentSetting is not null
        )
        {
            InteractiveSetting = sender.CurrentSetting.InteractiveSetting;
        }
    }
}
