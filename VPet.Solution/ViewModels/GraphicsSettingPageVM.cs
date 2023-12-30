using HKW.HKWUtils.Observable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet.Solution.Models;

namespace VPet.Solution.ViewModels;

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
        MainWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
    }

    private void Current_PropertyChangedX(MainWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowVM.CurrentSetting))
        {
            GraphicsSetting = sender.CurrentSetting.GraphicsSetting;
        }
    }
}
