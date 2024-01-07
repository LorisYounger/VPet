namespace VPet.Solution.Models.SettingEditor;

public class SystemSettingModel : ObservableClass<SystemSettingModel>
{
    /// <summary>
    /// 数据收集是否被禁止(当日)
    /// </summary>
    public bool DiagnosisDayEnable { get; } = true;

    private bool _diagnosis;

    /// <summary>
    /// 是否启用数据收集
    /// </summary>
    public bool Diagnosis
    {
        get => _diagnosis;
        set => SetProperty(ref _diagnosis, value);
    }

    private int _diagnosisInterval;

    /// <summary>
    /// 数据收集频率
    /// </summary>
    public int DiagnosisInterval
    {
        get => _diagnosisInterval;
        set => SetProperty(ref _diagnosisInterval, value);
    }

    private int _autoSaveInterval;

    /// <summary>
    /// 自动保存频率 (min)
    /// </summary>
    public int AutoSaveInterval
    {
        get => _autoSaveInterval;
        set => SetProperty(ref _autoSaveInterval, value);
    }

    private int _backupSaveMaxNum;

    /// <summary>
    /// 备份保存最大数量
    /// </summary>
    public int BackupSaveMaxNum
    {
        get => _backupSaveMaxNum;
        set => SetProperty(ref _backupSaveMaxNum, value);
    }
}
