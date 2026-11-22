using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.Mapper;
using HKW.MVVM;

namespace VPet.Solution.MultiPlatform.Models;

public partial class DiagnosticSettingModel : ObservableObjectEx, ISubSettingModel
{
    [MapIgnoreProperty]
    public SubSettingModelType ModelType => SubSettingModelType.Diagnostic;

    /// <summary>
    /// 自动修复超模
    /// </summary>
    [ObservableProperty]
    public bool AutoCal { get; set; }

    /// <summary>
    /// 是否启用数据收集
    /// </summary>
    [ObservableProperty]
    public bool Diagnosis { get; set; }

    /// <summary>
    /// 数据收集频率
    /// </summary>
    [ObservableProperty]
    [DefaultValue(500)]
    public int DiagnosisInterval { get; set; }

    public static ObservableCollection<int> DiagnosisIntervals { get; } =
    [200, 500, 1000, 2000, 5000, 10000, 20000];

    public void Load(Setting setting)
    {
        GetAutoCal(setting);
        Diagnosis = setting.Diagnosis;
        DiagnosisInterval = setting.DiagnosisInterval;
    }

    public void Save(Setting setting)
    {
        SetAutoCal(setting);
        setting.Diagnosis = Diagnosis;
        setting.DiagnosisInterval = DiagnosisInterval;
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
