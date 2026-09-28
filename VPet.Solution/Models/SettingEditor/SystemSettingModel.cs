using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.HKWMapper;
using HKW.MVVM;

namespace VPet.Solution.Models.SettingEditor;

[MapTarget(typeof(Setting))]
[MapTarget(typeof(SystemSettingModel))]
public partial class SystemSettingModel : ObservableObjectEx, ISubSettingModel
{
    [MapIgnoreProperty]
    public SubSettingModelType ModelType => SubSettingModelType.System;

    ///// <summary>
    ///// 数据收集是否被禁止(当日)
    ///// </summary>
    //public bool DiagnosisDayEnable { get; set; }

    /// <inheritdoc cref="Setting.AutoSaveInterval"/>
    [ObservableProperty]
    public int AutoSaveInterval { get; set; }

    public static ObservableCollection<int> AutoSaveIntervals { get; } = [10, 20, 30, 60];

    /// <inheritdoc cref="Setting.BackupSaveMaxNum"/>
    [ObservableProperty]
    public int BackupSaveMaxNum { get; set; }

    /// <inheritdoc cref="Setting.LastCacheDate"/>
    [ObservableProperty]
    public DateTime LastCacheDate { get; set; }

    public void Load(Setting setting)
    {
        this.MapFrom(setting);
    }

    public void Save(Setting setting)
    {
        this.MapTo(setting);
    }
}
