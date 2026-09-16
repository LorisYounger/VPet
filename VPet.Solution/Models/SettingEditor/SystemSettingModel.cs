using CommunityToolkit.Mvvm.ComponentModel;
using HKW.HKWMapper;
using HKW.MVVM;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace VPet.Solution.Models.SettingEditor;

[MapTo(typeof(Setting), ScrutinyMode = true)]
[MapFrom(typeof(Setting), ScrutinyMode = true)]
[MapFrom(typeof(SystemSettingModel), ScrutinyMode = true)]
public partial class SystemSettingModel : ObservableObjectEx, ISubSettingModel
{
    [MapIgnoreProperty]
    public SubSettingModelType ModelType => SubSettingModelType.System;

    ///// <summary>
    ///// 数据收集是否被禁止(当日)
    ///// </summary>
    //public bool DiagnosisDayEnable { get; set; }

    [ObservableProperty]
    /// <summary>
    /// 自动保存频率 (min)
    /// </summary>
    public int AutoSaveInterval { get; set; }

    public static ObservableCollection<int> AutoSaveIntervals { get; } = [10, 20, 30, 60];

    [ObservableProperty]
    /// <summary>
    /// 备份保存最大数量
    /// </summary>
    public int BackupSaveMaxNum { get; set; }
    
    /// <summary>
    /// 上次清理缓存日期
    /// </summary>
    [ObservableProperty]
    public DateTime LastCacheDate { get; set; }

    public void Load(Setting setting)
    {
        this.MapFromSetting(setting);
    }

    public void Save(Setting setting)
    {
        this.MapToSetting(setting);
    }
}
