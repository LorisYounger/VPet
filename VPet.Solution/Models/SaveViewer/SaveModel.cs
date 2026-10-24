using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using HKW.HKWMapper;
using HKW.MVVM.SourceGenerator;
using LinePutScript.Localization.WPF;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.Models.SaveViewer;

/// <summary>
/// 存档模型
/// </summary>
[MapTarget(typeof(GameSave_VPet))]
public class SaveModel
{
    public SaveModel(string filePath, GameSave_v2 save)
    {
        Name = Path.GetFileNameWithoutExtension(filePath);
        FilePath = filePath;
        DateSaved = File.GetLastWriteTime(filePath);
        this.MapFrom(save.GameSave);
        HashChecked = save.HashCheck;

        if (save.Statistics.Data.TryGetValue("stat_total_time", out var time) && time is not null)
            TotalTime = time.GetInteger64();
        foreach (var data in save.Statistics.Data)
        {
            Statistics.Add(new(data.Key, data.Key.Translate(), data.Value!));
        }
    }

    /// <summary>
    /// 名称
    /// </summary>
    [MapIgnoreProperty]
    public string Name { get; private set; }

    /// <summary>
    /// 文件路径
    /// </summary>
    [MapIgnoreProperty]
    public string FilePath { get; private set; }

    /// <summary>
    /// 统计数据
    /// </summary>
    [MapIgnoreProperty]
    public List<StatisticModel> Statistics { get; } = [];

    /// <summary>
    /// 是损坏的
    /// </summary>
    [MapIgnoreProperty]
    public bool IsDamaged { get; set; }

    [MapIgnoreProperty]
    public DateTime DateSaved { get; set; }

    [MapProperty(typeof(GameSave_VPet), nameof(GameSave_VPet.Name))]
    public string PetName { get; set; } = string.Empty;

    [DefaultValue(100)]
    public double Money { get; set; }

    [MapIgnoreProperty]
    [NotifyPropertyChangeFrom(nameof(Exp))]
    public int Level => Exp < 0 ? 1 : (int)(Math.Sqrt(Exp) / 10) + 1;

    public double Exp { get; set; }

    [DefaultValue(60)]
    public double Feeling { get; set; }

    [DefaultValue(100)]
    public double Health { get; set; }

    public double Likability { get; set; }

    public VPet_Simulator.Core.IGameSave.ModeType Mode { get; set; }

    [DefaultValue(100)]
    public double Strength { get; set; }

    [DefaultValue(100)]
    public double StrengthFood { get; set; }

    [DefaultValue(100)]
    public double StrengthDrink { get; set; }

    /// <summary>
    /// Hash已检查
    /// </summary>
    [MapIgnoreProperty]
    public bool HashChecked { get; private set; }

    /// <summary>
    /// 游玩总时长
    /// </summary>
    [MapIgnoreProperty]
    public long TotalTime { get; private set; }
}

/// <summary>
/// 统计数据模型
/// </summary>
public record StatisticModel(string Id, string Name, object Value);
