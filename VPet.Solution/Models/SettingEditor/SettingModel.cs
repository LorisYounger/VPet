using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using FastMember;
using HKW.HKWUtils.Observable;
using LinePutScript;
using LinePutScript.Localization.WPF;
using VPet.Solution.Properties;
using VPet_Simulator.Windows;

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

    #region IsChanged
    private bool _isChanged;

    /// <summary>
    /// 已更改
    /// </summary>
    public bool IsChanged
    {
        get => _isChanged;
        set => SetProperty(ref _isChanged, value);
    }
    #endregion

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

    #region CustomizedSetting
    private CustomizedSettingModel _CustomizedSetting;
    public CustomizedSettingModel CustomizedSetting
    {
        get => _CustomizedSetting;
        set => SetProperty(ref _CustomizedSetting, value);
    }
    #endregion

    #region DiagnosticSetting
    private DiagnosticSettingModel _diagnosticSetting;
    public DiagnosticSettingModel DiagnosticSetting
    {
        get => _diagnosticSetting;
        set => SetProperty(ref _diagnosticSetting, value);
    }
    #endregion

    #region ModSetting
    private ModSettingModel _modSetting;
    public ModSettingModel ModSetting
    {
        get => _modSetting;
        set => SetProperty(ref _modSetting, value);
    }
    #endregion


    private readonly Setting _setting;

    private readonly ReflectionOptions _saveReflectionOptions = new() { CheckValueEquals = true };

    public SettingModel()
        : this(new("Setting#VPET:|\n")) { }

    public SettingModel(Setting setting)
    {
        _setting = setting;

        GraphicsSetting = LoadSetting<GraphicsSettingModel>();
        if (string.IsNullOrWhiteSpace(GraphicsSetting.Language))
            GraphicsSetting.Language = LocalizeCore.CurrentCulture;

        InteractiveSetting = LoadSetting<InteractiveSettingModel>();

        SystemSetting = LoadSetting<SystemSettingModel>();

        CustomizedSetting = LoadCustomizedSetting(setting);

        DiagnosticSetting = LoadSetting<DiagnosticSettingModel>();
        DiagnosticSetting.GetAutoCalFromSetting(setting);

        ModSetting = LoadModSetting(setting);
        MergePropertyChangedNotify();
    }

    private void MergePropertyChangedNotify()
    {
        var accessor = ObjectAccessor.Create(this);
        foreach (var property in typeof(SettingModel).GetProperties())
        {
            var value = accessor[property.Name];
            if (value is INotifyPropertyChanged model)
                model.PropertyChanged += Notify_PropertyChanged;
        }
    }

    private void Notify_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsChanged = true;
    }

    private ModSettingModel LoadModSetting(Setting setting)
    {
        var settingModel = new ModSettingModel(setting);
        return settingModel;
    }

    private CustomizedSettingModel LoadCustomizedSetting(Setting setting)
    {
        var model = new CustomizedSettingModel();
        if (setting[CustomizedSettingModel.TargetName] is ILine line && line.Count > 0)
        {
            foreach (var sub in line)
                model.Links.Add(new(sub.Name, sub.Info));
        }
        else
        {
            setting.Remove(CustomizedSettingModel.TargetName);
        }
        return model;
    }

    private T LoadSetting<T>()
        where T : new()
    {
        var settingModel = new T();
        ReflectionUtils.SetValue(_setting, settingModel);
        return settingModel;
    }

    public void Save()
    {
        SaveSetting(GraphicsSetting);
        SaveSetting(InteractiveSetting);
        SaveSetting(SystemSetting);
        SaveSetting(DiagnosticSetting);
        DiagnosticSetting.SetAutoCalToSetting(_setting);
        foreach (var link in CustomizedSetting.Links)
            _setting[CustomizedSettingModel.TargetName].Add(new Sub(link.Name, link.Link));
        ModSetting.Save(_setting);
        File.WriteAllText(FilePath, _setting.ToString());
        IsChanged = false;
    }

    private void SaveSetting(object settingModel)
    {
        ReflectionUtils.SetValue(settingModel, _setting, _saveReflectionOptions);
    }
}
