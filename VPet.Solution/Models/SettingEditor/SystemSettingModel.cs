using System.Collections.ObjectModel;

namespace VPet.Solution.Models.SettingEditor;

public class SystemSettingModel : ObservableClass<SystemSettingModel>
{
    /// <summary>
    /// 数据收集是否被禁止(当日)
    /// </summary>
    public bool DiagnosisDayEnable { get; } = true;

    #region AutoSaveInterval
    private int _autoSaveInterval;

    /// <summary>
    /// 自动保存频率 (min)
    /// </summary>
    [ReflectionProperty(nameof(Setting.AutoSaveInterval))]
    public int AutoSaveInterval
    {
        get => _autoSaveInterval;
        set => SetProperty(ref _autoSaveInterval, value);
    }

    public static ObservableCollection<int> SaveIntervals { get; } =
        new() { -1, 2, 5, 10, 20, 30, 60 };
    #endregion

    #region BackupSaveMaxNum
    private int _backupSaveMaxNum;

    /// <summary>
    /// 备份保存最大数量
    /// </summary>
    [ReflectionProperty(nameof(Setting.BackupSaveMaxNum))]
    public int BackupSaveMaxNum
    {
        get => _backupSaveMaxNum;
        set => SetProperty(ref _backupSaveMaxNum, value);
    }
    #endregion
}
