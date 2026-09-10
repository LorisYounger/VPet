using System.Collections.ObjectModel;
using System.ComponentModel;
using HKW.HKWMapper;
using HKW.HKWReactiveUI;
using ReactiveUI;

namespace VPet.Solution.Models.SettingEditor;

public partial class DiagnosticSettingModel : ReactiveObject, ISubSettingModel
{
    [MapIgnoreProperty]
    public SubSettingModelType ModelType => SubSettingModelType.Diagnostic;

    [ReactiveProperty]
    /// <summary>
    /// 自动修复超模
    /// </summary>
    public bool AutoCal { get; set; }

    [ReactiveProperty]
    /// <summary>
    /// 是否启用数据收集
    /// </summary>
    public bool Diagnosis { get; set; }

    /// <summary>
    /// 数据收集频率
    /// </summary>
    [ReactiveProperty]
    [DefaultValue(500)]
    public int DiagnosisInterval { get; set; }
    public static ObservableCollection<int> DiagnosisIntervals { get; } =
        new() { 200, 500, 1000, 2000, 5000, 10000, 20000 };

    public void Load(Setting setting)
    {
        GetAutoCal(setting);
    }

    public void Save(Setting setting)
    {
        SetAutoCal(setting);
    }

    private void GetAutoCal(Setting setting)
    {
        AutoCal = setting["gameconfig"].GetBool("noAutoCal") is false;
    }

    private void SetAutoCal(Setting setting)
    {
        setting["gameconfig"].SetBool("noAutoCal", AutoCal is false);
    }
}
