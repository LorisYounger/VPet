using HKW.HKWUtils.Observable;
using LinePutScript.Localization.WPF;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using VPet_Simulator.Windows;

namespace VPet.Solution.Models.SettingEditor;

public class GraphicsSettingModel : ObservableClass<GraphicsSettingModel>
{
    #region ZoomLevel
    private double _zoomLevel = 1;

    /// <summary>
    /// 缩放倍率
    /// </summary>
    [DefaultValue(1)]
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.ZoomLevel))]
    public double ZoomLevel
    {
        get => _zoomLevel;
        set => SetProperty(ref _zoomLevel, value);
    }

    private double _zoomLevelMinimum = 0.5;

    [DefaultValue(0.5)]
    public double ZoomLevelMinimum
    {
        get => _zoomLevelMinimum;
        set => SetProperty(ref _zoomLevelMinimum, value);
    }

    private double _zoomLevelMaximum = 3;

    [DefaultValue(3)]
    public double ZoomLevelMaximum
    {
        get => _zoomLevelMaximum;
        set => SetProperty(ref _zoomLevelMaximum, value);
    }
    #endregion

    #region Resolution
    private int _resolution = 1000;

    /// <summary>
    /// 桌宠图形渲染的分辨率,越高图形越清晰
    /// </summary>
    [DefaultValue(1000)]
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.Resolution))]
    public int Resolution
    {
        get => _resolution;
        set => SetProperty(ref _resolution, value);
    }
    #endregion

    #region IsBiggerScreen
    private bool _isBiggerScreen;

    /// <summary>
    /// 是否为更大的屏幕
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.IsBiggerScreen))]
    public bool IsBiggerScreen
    {
        get => _isBiggerScreen;
        set
        {
            SetProperty(ref _isBiggerScreen, value);
            if (value is true)
                ZoomLevelMaximum = 8;
            else
                ZoomLevelMaximum = 3;
        }
    }
    #endregion

    #region TopMost
    private bool _topMost;

    /// <summary>
    /// 是否置于顶层
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.TopMost))]
    public bool TopMost
    {
        get => _topMost;
        set => SetProperty(ref _topMost, value);
    }
    #endregion

    #region HitThrough
    private bool _hitThrough;

    /// <summary>
    /// 是否鼠标穿透
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.HitThrough))]
    public bool HitThrough
    {
        get => _hitThrough;
        set => SetProperty(ref _hitThrough, value);
    }
    #endregion

    #region Language
    private string _language;

    /// <summary>
    /// 语言
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.Language))]
    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    public static IEnumerable<string> Languages => LocalizeCore.AvailableCultures;

    #endregion

    #region Font
    private string _font;

    /// <summary>
    /// 字体
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.Font))]
    public string Font
    {
        get => _font;
        set => SetProperty(ref _font, value);
    }
    #endregion

    #region Theme
    private string _theme;

    /// <summary>
    /// 主题
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.Theme))]
    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }
    #endregion

    #region StartUPBoot
    private bool _startUPBoot;

    /// <summary>
    /// 开机启动
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.StartUPBoot))]
    public bool StartUPBoot
    {
        get => _startUPBoot;
        set => SetProperty(ref _startUPBoot, value);
    }
    #endregion

    #region StartUPBootSteam
    private bool _startUPBootSteam;

    /// <summary>
    /// 开机启动 Steam
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.StartUPBootSteam))]
    public bool StartUPBootSteam
    {
        get => _startUPBootSteam;
        set => SetProperty(ref _startUPBootSteam, value);
    }
    #endregion

    #region StartRecordLast
    private bool _startRecordLast = true;

    /// <summary>
    /// 是否记录游戏退出位置
    /// </summary>
    [DefaultValue(true)]
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.StartRecordLast))]
    public bool StartRecordLast
    {
        get => _startRecordLast;
        set => SetProperty(ref _startRecordLast, value);
    }
    #endregion
    //private Point _startRecordLastPoint;

    ///// <summary>
    ///// 记录上次退出位置
    ///// </summary>
    //public Point StartRecordLastPoint
    //{
    //    get => _startRecordLastPoint;
    //    set => SetProperty(ref _startRecordLastPoint, value);
    //}

    #region StartRecordPoint
    private ObservablePoint _startRecordPoint;

    /// <summary>
    /// 设置中桌宠启动的位置
    /// </summary>
    [ReflectionProperty]
    [ReflectionPropertyConverter(typeof(ObservablePointToPointConverter))]
    public ObservablePoint StartRecordPoint
    {
        get => _startRecordPoint;
        set => SetProperty(ref _startRecordPoint, value);
    }
    #endregion

    #region HideFromTaskControl
    private bool _hideFromTaskControl;

    /// <summary>
    /// 在任务切换器(Alt+Tab)中隐藏窗口
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.HideFromTaskControl))]
    public bool HideFromTaskControl
    {
        get => _hideFromTaskControl;
        set => SetProperty(ref _hideFromTaskControl, value);
    }
    #endregion

    #region MessageBarOutside
    private bool _messageBarOutside;

    /// <summary>
    /// 消息框外置
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.MessageBarOutside))]
    public bool MessageBarOutside
    {
        get => _messageBarOutside;
        set => SetProperty(ref _messageBarOutside, value);
    }
    #endregion

    #region PetHelper
    private bool _petHelper;

    /// <summary>
    /// 是否显示宠物帮助窗口
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.PetHelper))]
    public bool PetHelper
    {
        get => _petHelper;
        set => SetProperty(ref _petHelper, value);
    }
    #endregion

    #region PetHelpLeft
    private double _petHelpLeft;

    // TODO 加入 PetHelpLeft

    /// <summary>
    /// 快捷穿透按钮X坐标
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.PetHelpLeft))]
    public double PetHelpLeft
    {
        get => _petHelpLeft;
        set => SetProperty(ref _petHelpLeft, value);
    }
    #endregion

    #region PetHelpTop
    private double _petHelpTop;

    // TODO 加入 PetHelpTop

    /// <summary>
    /// 快捷穿透按钮Y坐标
    /// </summary>
    [ReflectionProperty(nameof(VPet_Simulator.Windows.Setting.PetHelpTop))]
    public double PetHelpTop
    {
        get => _petHelpTop;
        set => SetProperty(ref _petHelpTop, value);
    }
    #endregion
}
