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
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; }

    #region GraphicsSetting
    private GraphicsSettingModel _graphicsSetting;
    public GraphicsSettingModel GraphicsSetting
    {
        get => _graphicsSetting;
        set => SetProperty(ref _graphicsSetting, value);
    }
    #endregion

    #region SystemSetting
    private SystemSettingModel _systemSetting;

    public SystemSettingModel SystemSetting
    {
        get => _systemSetting;
        set => SetProperty(ref _systemSetting, value);
    }
    #endregion

    #region InteractiveSetting
    private InteractiveSettingModel _interactiveSetting;
    public InteractiveSettingModel InteractiveSetting
    {
        get => _interactiveSetting;
        set => SetProperty(ref _interactiveSetting, value);
    }
    #endregion

    private static HashSet<string> _settingProperties =
        new(typeof(Setting).GetProperties().Select(p => p.Name));

    private Setting _setting;

    private ReflectionOptions _saveReflectionOptions = new() { CheckValueEquals = true };

    public SettingModel()
        : this(new("")) { }

    public SettingModel(Setting setting)
    {
        _setting = setting;
        GraphicsSetting = LoadSetting<GraphicsSettingModel>();
        InteractiveSetting = LoadSetting<InteractiveSettingModel>();
        SystemSetting = LoadSetting<SystemSettingModel>();
    }

    private T LoadSetting<T>()
        where T : new()
    {
        var setting = new T();
        ReflectionUtils.SetValue(_setting, setting);
        return setting;
    }

    public void Save()
    {
        SaveSetting(GraphicsSetting);
        SaveSetting(InteractiveSetting);
        SaveSetting(SystemSetting);
        File.WriteAllText(FilePath, _setting.ToString());
    }

    private void SaveSetting(object setting)
    {
        ReflectionUtils.SetValue(setting, _setting, _saveReflectionOptions);
    }
}
