using FastMember;
using HKW.HKWUtils.Observable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using VPet.Solution.Properties;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.Models.SettingEditor;

public class SettingModel : ObservableClass<SettingModel>
{
    private string _name;

    /// <summary>
    /// 名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _filePath;

    /// <summary>
    /// 路径
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    private GraphicsSettingModel _graphicsSetting;
    public GraphicsSettingModel GraphicsSetting
    {
        get => _graphicsSetting;
        set => SetProperty(ref _graphicsSetting, value);
    }

    private SystemSettingModel _systemSetting;

    public SystemSettingModel SystemSetting
    {
        get => _systemSetting;
        set => SetProperty(ref _systemSetting, value);
    }

    private InteractiveSettingModel _interactiveSetting;
    public InteractiveSettingModel InteractiveSetting
    {
        get => _interactiveSetting;
        set => SetProperty(ref _interactiveSetting, value);
    }

    public SettingModel(Setting setting)
    {
        GraphicsSetting = LoadGraphicsSettings(setting);
    }

    private GraphicsSettingModel LoadGraphicsSettings(Setting setting)
    {
        var graphicsSettings = new GraphicsSettingModel();
        var sourceAccessor = ObjectAccessor.Create(setting);
        var targetAccessor = ObjectAccessor.Create(graphicsSettings);
        foreach (var property in typeof(GraphicsSettingModel).GetProperties())
        {
            if (sourceAccessor[property.Name] is Point point)
            {
                targetAccessor[property.Name] = new ObservablePoint(point);
            }
            else
            {
                targetAccessor[property.Name] = sourceAccessor[property.Name];
            }
        }
        return graphicsSettings;
    }
}
