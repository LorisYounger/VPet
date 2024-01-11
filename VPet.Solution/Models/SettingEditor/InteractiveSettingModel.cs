using HKW.HKWUtils.Observable;
using System.Collections.ObjectModel;
using VPet_Simulator.Core;

namespace VPet.Solution.Models.SettingEditor;

public class InteractiveSettingModel : ObservableClass<InteractiveSettingModel>
{
    // NOTE: 这玩意其实在存档里 而不是设置里
    //#region PetName
    //private string _petName;

    ///// <summary>
    ///// 宠物名称
    ///// </summary>
    //[ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.PetName))]
    //public string PetName
    //{
    //    get => _petName;
    //    set => SetProperty(ref _petName, value);
    //}
    //#endregion

    #region VoiceVolume
    private double _voiceVolume;

    /// <summary>
    /// 播放声音大小
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.VoiceVolume))]
    public double VoiceVolume
    {
        get => _voiceVolume;
        set => SetProperty(ref _voiceVolume, value);
    }
    #endregion

    #region CalFunState
    private GameSave.ModeType _calFunState;

    /// <summary>
    /// 非计算模式下默认模式
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.CalFunState))]
    public GameSave.ModeType CalFunState
    {
        get => _calFunState;
        set => SetProperty(ref _calFunState, value);
    }

    public ObservableCollection<GameSave.ModeType> ModeTypes { get; } =
        new(Enum.GetValues(typeof(GameSave.ModeType)).Cast<GameSave.ModeType>());
    #endregion

    #region PetHelper
    private bool _petHelper;

    /// <summary>
    /// 是否显示宠物帮助窗口
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.PetHelper))]
    public bool PetHelper
    {
        get => _petHelper;
        set => SetProperty(ref _petHelper, value);
    }
    #endregion

    #region LastCacheDate
    private DateTime _lastCacheDate;

    /// <summary>
    /// 上次清理缓存日期
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.LastCacheDate))]
    public DateTime LastCacheDate
    {
        get => _lastCacheDate;
        set => SetProperty(ref _lastCacheDate, value);
    }
    #endregion

    #region SaveTimes
    private int _saveTimes;

    /// <summary>
    /// 储存顺序次数
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.SaveTimes))]
    public int SaveTimes
    {
        get => _saveTimes;
        set => SetProperty(ref _saveTimes, value);
    }
    #endregion

    #region PressLength
    private int _pressLength;

    /// <summary>
    /// 按多久视为长按 单位毫秒
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.PressLength))]
    public int PressLength
    {
        get => _pressLength;
        set => SetProperty(ref _pressLength, value);
    }
    #endregion

    #region InteractionCycle
    private int _interactionCycle;

    /// <summary>
    /// 互动周期
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.InteractionCycle))]
    public int InteractionCycle
    {
        get => _interactionCycle;
        set => SetProperty(ref _interactionCycle, value);
    }
    #endregion

    #region LogicInterval
    private double _logicInterval;

    /// <summary>
    /// 计算间隔 (秒)
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.LogicInterval))]
    public double LogicInterval
    {
        get => _logicInterval;
        set => SetProperty(ref _logicInterval, value);
    }
    #endregion

    #region PetHelpLeft
    private double _petHelpLeft;

    /// <summary>
    /// 计算间隔
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.PetHelpLeft))]
    public double PetHelpLeft
    {
        get => _petHelpLeft;
        set => SetProperty(ref _petHelpLeft, value);
    }
    #endregion

    #region PetHelpTop
    private double _petHelpTop;

    /// <summary>
    /// 计算间隔
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.PetHelpTop))]
    public double PetHelpTop
    {
        get => _petHelpTop;
        set => SetProperty(ref _petHelpTop, value);
    }
    #endregion

    #region AllowMove
    private bool _allowMove;

    /// <summary>
    /// 允许移动事件
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.AllowMove))]
    public bool AllowMove
    {
        get => _allowMove;
        set => SetProperty(ref _allowMove, value);
    }
    #endregion

    #region SmartMove
    private bool _smartMove;

    /// <summary>
    /// 智能移动
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.SmartMove))]
    public bool SmartMove
    {
        get => _smartMove;
        set => SetProperty(ref _smartMove, value);
    }
    #endregion

    #region EnableFunction
    private bool _enableFunction;

    /// <summary>
    /// 启用计算等数据功能
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.EnableFunction))]
    public bool EnableFunction
    {
        get => _enableFunction;
        set => SetProperty(ref _enableFunction, value);
    }
    #endregion

    #region SmartMoveInterval
    private int _smartMoveInterval;

    /// <summary>
    /// 智能移动周期 (秒)
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.SmartMoveInterval))]
    public int SmartMoveInterval
    {
        get => _smartMoveInterval;
        set => SetProperty(ref _smartMoveInterval, value);
    }
    #endregion

    #region MessageBarOutside
    private bool _messageBarOutside;

    /// <summary>
    /// 消息框外置
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.MessageBarOutside))]
    public bool MessageBarOutside
    {
        get => _messageBarOutside;
        set => SetProperty(ref _messageBarOutside, value);
    }
    #endregion

    #region PetGraph
    private string _petGraph;

    /// <summary>
    /// 桌宠选择内容
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.PetGraph))]
    public string PetGraph
    {
        get => _petGraph;
        set => SetProperty(ref _petGraph, value);
    }
    #endregion

    #region MusicCatch
    private double _musicCatch;

    /// <summary>
    /// 当实时播放音量达到该值时运行音乐动作
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.MusicCatch))]
    public double MusicCatch
    {
        get => _musicCatch;
        set => SetProperty(ref _musicCatch, value);
    }
    #endregion

    #region MusicMax
    private double _musicMax;

    /// <summary>
    /// 当实时播放音量达到该值时运行特殊音乐动作
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.MusicMax))]
    public double MusicMax
    {
        get => _musicMax;
        set => SetProperty(ref _musicMax, value);
    }
    #endregion

    #region AutoBuy
    private bool _autoBuy;

    /// <summary>
    /// 允许桌宠自动购买食品
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.AutoBuy))]
    public bool AutoBuy
    {
        get => _autoBuy;
        set => SetProperty(ref _autoBuy, value);
    }
    #endregion

    #region AutoGift
    private bool _autoGift;

    /// <summary>
    /// 允许桌宠自动购买礼物
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.AutoGift))]
    public bool AutoGift
    {
        get => _autoGift;
        set => SetProperty(ref _autoGift, value);
    }
    #endregion

    #region MoveAreaDefault
    private bool _moveAreaDefault;

    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.MoveAreaDefault))]
    public bool MoveAreaDefault
    {
        get => _moveAreaDefault;
        set => SetProperty(ref _moveAreaDefault, value);
    }
    #endregion

    #region MoveArea
    private System.Drawing.Rectangle _moveArea;

    [ReflectionProperty(nameof(VPet_Simulator.Windows.Interface.Setting.MoveArea))]
    public System.Drawing.Rectangle MoveArea
    {
        get => _moveArea;
        set => SetProperty(ref _moveArea, value);
    }
    #endregion
}
