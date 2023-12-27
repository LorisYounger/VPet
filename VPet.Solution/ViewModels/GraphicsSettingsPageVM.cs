using HKW.HKWUtils.Observable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet.Solution.Models;

namespace VPet.Solution.ViewModels;

public class GraphicsSettingsPageVM : ObservableClass<GraphicsSettingsPageVM>
{
    private GraphicsSettingsModel _graphicsSettings;
    public GraphicsSettingsModel GraphicsSettings
    {
        get => _graphicsSettings;
        set => SetProperty(ref _graphicsSettings, value);
    }
}
