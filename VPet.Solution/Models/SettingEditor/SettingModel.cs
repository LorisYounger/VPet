using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.MVVM;
using LinePutScript;
using LinePutScript.Localization.WPF;

namespace VPet.Solution.Models.SettingEditor;

public partial class SettingModel : ObservableObjectEx
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// 已更改
    /// </summary>
    [ObservableProperty]
    public bool IsChanged { get; set; }

    public GraphicsSettingModel GraphicsSetting { get; } = new();

    public SystemSettingModel SystemSetting { get; } = new();

    public InteractiveSettingModel InteractiveSetting { get; } = new();

    public CustomizedSettingModel CustomizedSetting { get; } = new();

    public DiagnosticSettingModel DiagnosticSetting { get; } = new();

    public ModSettingModel ModSetting { get; } = new();

    private readonly Setting _setting;
    internal MultipleDisposable Disposables { get; } = [];

    public SettingModel()
        : this(new("Setting#VPET:|\n")) { }

    public SettingModel(Setting setting)
    {
        _setting = setting;

        Reload();
        GraphicsSetting.Changed.Subscribe(_ => IsChanged = true).DisposeWith(Disposables);
        SystemSetting.Changed.Subscribe(_ => IsChanged = true).DisposeWith(Disposables);
        DiagnosticSetting.Changed.Subscribe(_ => IsChanged = true).DisposeWith(Disposables);
        InteractiveSetting.Changed.Subscribe(_ => IsChanged = true).DisposeWith(Disposables);
        CustomizedSetting.Changed.Subscribe(_ => IsChanged = true).DisposeWith(Disposables);
        ModSetting.Changed.Subscribe(_ => IsChanged = true);
    }

    /// <summary>
    /// 恢复初始设置
    /// </summary>
    public void Reload()
    {
        GraphicsSetting.Load(_setting);
        SystemSetting.Load(_setting);
        DiagnosticSetting.Load(_setting);
        InteractiveSetting.Load(_setting);
        CustomizedSetting.Load(_setting);
        ModSetting.Load(_setting);

        IsChanged = false;
    }

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    public void Reset()
    {
        GraphicsSetting.Load(Setting.Default);
        SystemSetting.Load(Setting.Default);
        DiagnosticSetting.Load(Setting.Default);
        InteractiveSetting.Load(Setting.Default);
        CustomizedSetting.Load(Setting.Default);
        ModSetting.Load(Setting.Default);

        IsChanged = false;
    }

    public ISubSettingModel GetSubSetting(SubSettingModelType modelType)
    {
        return modelType switch
        {
            SubSettingModelType.Graphics => GraphicsSetting,
            SubSettingModelType.System => SystemSetting,
            SubSettingModelType.Interactive => InteractiveSetting,
            SubSettingModelType.Customized => CustomizedSetting,
            SubSettingModelType.Diagnostic => DiagnosticSetting,
            SubSettingModelType.Mod => ModSetting,
            _ => throw new NotImplementedException(),
        };
    }

    public void Save()
    {
        GraphicsSetting.Save(_setting);
        SystemSetting.Save(_setting);
        DiagnosticSetting.Save(_setting);
        InteractiveSetting.Save(_setting);
        CustomizedSetting.Save(_setting);
        ModSetting.Save(_setting);

        File.WriteAllText(FilePath, _setting.ToString());
        IsChanged = false;
    }
}

public interface ISubSettingModel
{
    public SubSettingModelType ModelType { get; }
}

public enum SubSettingModelType
{
    Graphics,
    System,
    Interactive,
    Customized,
    Diagnostic,
    Mod,
}
