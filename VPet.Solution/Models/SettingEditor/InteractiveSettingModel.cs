using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using HKW.HKWMapper;
using HKW.HKWReactiveUI;
using HKW.HKWUtils;
using ReactiveUI;
using VPet_Simulator.Core;

namespace VPet.Solution.Models.SettingEditor;

[MapTo(typeof(Setting), ScrutinyMode = true)]
[MapFrom(typeof(Setting), ScrutinyMode = true)]
[MapFrom(typeof(InteractiveSettingModel), ScrutinyMode = true)]
public partial class InteractiveSettingModel : ReactiveObject, ISubSettingModel
{
    [MapIgnoreProperty]
    public SubSettingModelType ModelType => SubSettingModelType.Interactive;

    /// <summary>
    /// 播放声音大小
    /// </summary>
    [ReactiveProperty]
    public double VoiceVolume { get; set; }

    /// <summary>
    /// 启用计算等数据功能
    /// </summary>
    [ReactiveProperty]
    public bool EnableFunction { get; set; }

    /// <summary>
    /// 非计算模式下默认模式
    /// </summary>
    [ReactiveProperty]
    public IGameSave.ModeType CalFunState { get; set; }

    public static ImmutableArray<IGameSave.ModeType> CalFunStates { get; } =
        EnumInfo<IGameSave.ModeType>.StaticValues;

    /// <summary>
    /// 上次清理缓存日期
    /// </summary>
    [ReactiveProperty]
    public DateTime LastCacheDate { get; set; }

    /// <summary>
    /// 储存顺序次数
    /// </summary>
    [ReactiveProperty]
    public int SaveTimes { get; set; }

    /// <summary>
    /// 按多久视为长按 单位毫秒
    /// </summary>
    [ReactiveProperty]
    public int PressLength { get; set; }

    /// <summary>
    /// 互动周期
    /// </summary>
    [ReactiveProperty]
    public int InteractionCycle { get; set; }

    /// <summary>
    /// 计算间隔 (秒)
    /// </summary>
    [ReactiveProperty]
    public double LogicInterval { get; set; }

    /// <summary>
    /// 允许移动事件
    /// </summary>
    [ReactiveProperty]
    public bool AllowMove { get; set; }

    /// <summary>
    /// 智能移动
    /// </summary>
    [ReactiveProperty]
    public bool SmartMove { get; set; }

    /// <summary>
    /// 智能移动周期 (秒)
    /// </summary>
    [ReactiveProperty]
    [DefaultValue(1)]
    public int SmartMoveInterval { get; set; }

    public static int[] SmartMoveIntervals { get; } = [1, 2, 5, 10, 20, 30, 40, 50, 60];

    /// <summary>
    /// 桌宠选择内容
    /// </summary>
    [ReactiveProperty]
    public string PetGraph { get; set; } = string.Empty;

    /// <summary>
    /// 当实时播放音量达到该值时运行音乐动作
    /// </summary>
    [ReactiveProperty]
    [InteractiveSettingModelMapToSettingProperty(typeof(PercentageConverter))]
    [InteractiveSettingModelMapFromSettingProperty(typeof(PercentageConverter))]
    public int MusicCatch { get; set; }

    /// <summary>
    /// 当实时播放音量达到该值时运行特殊音乐动作
    /// </summary>
    [ReactiveProperty]
    [InteractiveSettingModelMapToSettingProperty(typeof(PercentageConverter))]
    [InteractiveSettingModelMapFromSettingProperty(typeof(PercentageConverter))]
    public int MusicMax { get; set; }

    /// <summary>
    /// 允许桌宠自动购买食品
    /// </summary>
    [ReactiveProperty]
    public bool AutoBuy { get; set; }

    /// <summary>
    /// 允许桌宠自动购买礼物
    /// </summary>
    [ReactiveProperty]
    public bool AutoGift { get; set; }

    [ReactiveProperty]
    public bool MoveAreaDefault { get; set; }

    [ReactiveProperty]
    public System.Drawing.Rectangle MoveArea { get; set; }

    public void Load(Setting setting)
    {
        this.MapFromSetting(setting);
    }

    public void Save(Setting setting)
    {
        this.MapToSetting(setting);
    }
}

public class SecondToMinuteConverter : MapConverter<int, double>
{
    public override double Convert(object source, int value)
    {
        return value * 60;
    }

    public override int ConvertBack(object source, double value)
    {
        if (value == 30d)
            return 1;
        else
            return System.Convert.ToInt32(value / 60);
    }
}

public class PercentageConverter : MapConverter<int, double>
{
    public override double Convert(object source, int value)
    {
        return value / 100d;
    }

    public override int ConvertBack(object source, double value)
    {
        return System.Convert.ToInt32(value * 100);
    }
}
