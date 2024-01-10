using FastMember;
using HKW.HKWUtils.Observable;
using LinePutScript;
using System.ComponentModel;
using System.Reflection;
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

    private ReflectionOptions _saveReflectionOptions = new() { CheckValueEquals = true };

    public SettingModel(Setting setting)
    {
        _setting = setting;
        GraphicsSetting = LoadGraphicsSettings();
    }

    private GraphicsSettingModel LoadGraphicsSettings()
    {
        var graphicsSetting = new GraphicsSettingModel();
        ReflectionUtils.SetValue(_setting, graphicsSetting);
        return graphicsSetting;
    }

    public void Save()
    {
        SaveGraphicsSettings();
        File.WriteAllText(FilePath, _setting.ToString());
    }

    private void SaveGraphicsSettings()
    {
        ReflectionUtils.SetValue(GraphicsSetting, _setting, _saveReflectionOptions);
    }
}
