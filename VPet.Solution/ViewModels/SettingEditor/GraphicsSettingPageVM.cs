using HKW.HKWUtils.Observable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet.Solution.Models;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public class GraphicsSettingPageVM : ObservableClass<GraphicsSettingPageVM>
{
    private GraphicsSettingModel _graphicsSetting;
    public GraphicsSettingModel GraphicsSetting
    {
        get => _graphicsSetting;
        set => SetProperty(ref _graphicsSetting, value);
    }

    public GraphicsSettingPageVM()
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
            GraphicsSetting = sender.CurrentSetting.GraphicsSetting;
        }
    }
}
