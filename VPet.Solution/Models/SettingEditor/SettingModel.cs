using FastMember;
using HKW.HKWUtils.Observable;
using LinePutScript;
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
    /// 文件路径
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

    private static HashSet<string> _settingProperties =
        new(typeof(Setting).GetProperties().Select(p => p.Name));

    private Setting _setting;

    public SettingModel(Setting setting)
    {
        _setting = setting;
        GraphicsSetting = LoadGraphicsSettings();
    }

    private GraphicsSettingModel LoadGraphicsSettings()
    {
        var graphicsSettings = new GraphicsSettingModel();
        var sourceAccessor = ObjectAccessor.Create(_setting);
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

    public void Save()
    {
        SaveGraphicsSettings();
        File.WriteAllText(FilePath, _setting.ToString());
    }

    private void SaveGraphicsSettings()
    {
        var sourceAccessor = ObjectAccessor.Create(GraphicsSetting);
        var targetAccessor = ObjectAccessor.Create(_setting);
        foreach (var property in typeof(GraphicsSettingModel).GetProperties())
        {
            //if (_settingProperties.Contains(property.Name) is false)
            //    continue;
            var sourceValue = sourceAccessor[property.Name];
            var targetValue = targetAccessor[property.Name];
            if (sourceValue.Equals(targetValue))
                continue;
            if (sourceValue is ObservablePoint point)
            {
                targetAccessor[property.Name] = point.ToPoint();
            }
            else
            {
                targetAccessor[property.Name] = sourceValue;
            }
        }
    }
}
