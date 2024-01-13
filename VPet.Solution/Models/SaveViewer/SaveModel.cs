using HKW.HKWUtils.Observable;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.Models.SaveViewer;

/// <summary>
/// 存档模型
/// </summary>
public class SaveModel : ObservableClass<SaveModel>
{
    /// <summary>
    /// 名称
    /// </summary>
    [ReflectionPropertyIgnore]
    public string Name { get; set; }

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// 统计数据
    /// </summary>
    public ObservableCollection<StatisticDataModel> Statistics { get; set; } = new();

    /// <summary>
    /// 是损坏的
    /// </summary>
    public bool IsDamaged { get; set; }

    #region DateSaved
    private DateTime _dateSaved;
    public DateTime DateSaved
    {
        get => _dateSaved;
        set => SetProperty(ref _dateSaved, value);
    }
    #endregion


    #region PetName
    private string _petName;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.Name))]
    public string PetName
    {
        get => _petName;
        set => SetProperty(ref _petName, value);
    }
    #endregion

    #region Level
    private int _level;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.Level))]
    public int Level
    {
        get => _level;
        set => SetProperty(ref _level, value);
    }
    #endregion

    #region Money
    private double _money = 100;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.Money))]
    public double Money
    {
        get => _money;
        set => SetProperty(ref _money, value);
    }
    #endregion

    #region Exp
    private double _exp = 0;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.Exp))]
    public double Exp
    {
        get => _exp;
        set => SetProperty(ref _exp, value);
    }
    #endregion

    #region Feeling
    private double _feeling = 60;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.Feeling))]
    public double Feeling
    {
        get => _feeling;
        set => SetProperty(ref _feeling, value);
    }
    #endregion

    #region Health
    private double _health = 100;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.Health))]
    public double Health
    {
        get => _health;
        set => SetProperty(ref _health, value);
    }
    #endregion

    #region Likability
    private double _likability = 0;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.Likability))]
    public double Likability
    {
        get => _likability;
        set => SetProperty(ref _likability, value);
    }
    #endregion

    #region Mode
    private VPet_Simulator.Core.GameSave.ModeType _mode;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.Mode))]
    public VPet_Simulator.Core.GameSave.ModeType Mode
    {
        get => _mode;
        set => SetProperty(ref _mode, value);
    }
    #endregion

    #region Strength
    private double _strength = 100;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.Strength))]
    public double Strength
    {
        get => _strength;
        set => SetProperty(ref _strength, value);
    }
    #endregion

    #region StrengthFood
    private double _strengthFood = 100;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.StrengthFood))]
    public double StrengthFood
    {
        get => _strengthFood;
        set => SetProperty(ref _strengthFood, value);
    }
    #endregion

    #region StrengthDrink
    private double _strengthDrink = 100;

    [ReflectionProperty(nameof(VPet_Simulator.Core.GameSave.StrengthDrink))]
    public double StrengthDrink
    {
        get => _strengthDrink;
        set => SetProperty(ref _strengthDrink, value);
    }
    #endregion


    #region HashChecked
    private bool _hashChecked;

    /// <summary>
    /// Hash已检查
    /// </summary>
    public bool HashChecked
    {
        get => _hashChecked;
        set => SetProperty(ref _hashChecked, value);
    }
    #endregion



    #region TotalTime
    private long _totalTime;

    /// <summary>
    /// 游玩总时长
    /// </summary>
    public long TotalTime
    {
        get => _totalTime;
        set => SetProperty(ref _totalTime, value);
    }
    #endregion


    public SaveModel(string filePath, GameSave_v2 save)
    {
        Name = Path.GetFileNameWithoutExtension(filePath);
        FilePath = filePath;
        DateSaved = File.GetLastWriteTime(filePath);
        LoadSave(save.GameSave);
        if (save.Statistics.Data.TryGetValue("stat_total_time", out var time))
            TotalTime = time.GetInteger64();
        HashChecked = save.HashCheck;
        foreach (var data in save.Statistics.Data)
        {
            Statistics.Add(
                new()
                {
                    Id = data.Key,
                    Name = data.Key.Translate(),
                    Value = data.Value
                }
            );
        }
    }

    private void LoadSave(VPet_Simulator.Core.GameSave save)
    {
        ReflectionUtils.SetValue(save, this);
    }
}
