using System.Collections.ObjectModel;
using HKW.HKWMapper;
using HKW.HKWReactiveUI;
using ReactiveUI;

namespace VPet.Solution.Models.SettingEditor;

[MapTo(typeof(Setting), ScrutinyMode = true)]
[MapFrom(typeof(Setting), ScrutinyMode = true)]
[MapFrom(typeof(SystemSettingModel), ScrutinyMode = true)]
public partial class SystemSettingModel : ReactiveObject, ISubSettingModel
{
    [MapIgnoreProperty]
    public SubSettingModelType ModelType => SubSettingModelType.System;

    ///// <summary>
    ///// 数据收集是否被禁止(当日)
    ///// </summary>
    //public bool DiagnosisDayEnable { get; set; }

    [ReactiveProperty]
    /// <summary>
    /// 自动保存频率 (min)
    /// </summary>
    public int AutoSaveInterval { get; set; }

    public static ObservableCollection<int> AutoSaveIntervals { get; } = [-1, 2, 5, 10, 20, 30, 60];

    [ReactiveProperty]
    /// <summary>
    /// 备份保存最大数量
    /// </summary>
    public int BackupSaveMaxNum { get; set; }

    public void Load(Setting setting)
    {
        this.MapFromSetting(setting);
    }

    public void Save(Setting setting)
    {
        this.MapToSetting(setting);
    }
}
