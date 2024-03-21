using HKW.HKWUtils.Observable;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.Models.SettingEditor;

public class DiagnosticSettingModel : ObservableClass<DiagnosticSettingModel>
{
    #region AutoCal
    private bool _autoCal;

    /// <summary>
    /// 自动修复超模
    /// </summary>
    public bool AutoCal
    {
        get => _autoCal;
        set => SetProperty(ref _autoCal, value);
    }
    #endregion

    #region Diagnosis
    private bool _diagnosis;

    /// <summary>
    /// 是否启用数据收集
    /// </summary>
    [ReflectionProperty(nameof(VPet.Solution.Models.SettingEditor.Setting.Diagnosis))]
    public bool Diagnosis
    {
        get => _diagnosis;
        set => SetProperty(ref _diagnosis, value);
    }
    #endregion

    #region DiagnosisInterval
    private int _diagnosisInterval = 500;

    /// <summary>
    /// 数据收集频率
    /// </summary>
    [DefaultValue(500)]
    [ReflectionProperty(nameof(VPet.Solution.Models.SettingEditor.Setting.DiagnosisInterval))]
    public int DiagnosisInterval
    {
        get => _diagnosisInterval;
        set => SetProperty(ref _diagnosisInterval, value);
    }
    public static ObservableCollection<int> DiagnosisIntervals { get; } =
        new() { 200, 500, 1000, 2000, 5000, 10000, 20000 };
    #endregion

    public void GetAutoCalFromSetting(Setting setting)
    {
        AutoCal = setting["gameconfig"].GetBool("noAutoCal") is false;
    }

    public void SetAutoCalToSetting(Setting setting)
    {
        setting["gameconfig"].SetBool("noAutoCal", AutoCal is false);
    }
}
