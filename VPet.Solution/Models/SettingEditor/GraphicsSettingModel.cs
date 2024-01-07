using System.ComponentModel;
using System.Windows;

namespace VPet.Solution.Models.SettingEditor;

public class GraphicsSettingModel : ObservableClass<GraphicsSettingModel>
{
    private double _zoomLevel = 1;

    /// <summary>
    /// 缩放倍率
    /// </summary>
    [DefaultValue(1)]
    public double ZoomLevel
    {
        get => _zoomLevel;
        set => SetProperty(ref _zoomLevel, value);
    }

    private int _resolution = 1000;

    /// <summary>
    /// 桌宠图形渲染的分辨率,越高图形越清晰
    /// </summary>
    [DefaultValue(1000)]
    public int Resolution
    {
        get => _resolution;
        set => SetProperty(ref _resolution, value);
    }

    private bool _isBiggerScreen;

    /// <summary>
    /// 是否为更大的屏幕
    /// </summary>
    public bool IsBiggerScreen
    {
        get => _isBiggerScreen;
        set => SetProperty(ref _isBiggerScreen, value);
    }

    private bool _topMost;

    /// <summary>
    /// 是否置于顶层
    /// </summary>
    public bool TopMost
    {
        get => _topMost;
        set => SetProperty(ref _topMost, value);
    }

    private bool _hitThrough;

    /// <summary>
    /// 是否鼠标穿透
    /// </summary>
    public bool HitThrough
    {
        get => _hitThrough;
        set => SetProperty(ref _hitThrough, value);
    }

    private string _language;

    /// <summary>
    /// 语言
    /// </summary>
    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }
    private string _font;

    /// <summary>
    /// 字体
    /// </summary>
    public string Font
    {
        get => _font;
        set => SetProperty(ref _font, value);
    }
    private string _theme;

    /// <summary>
    /// 主题
    /// </summary>
    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    private bool _startUPBoot;

    /// <summary>
    /// 开机启动
    /// </summary>
    public bool StartUPBoot
    {
        get => _startUPBoot;
        set => SetProperty(ref _startUPBoot, value);
    }

    private bool _startUPBootSteam;

    /// <summary>
    /// 开机启动 Steam
    /// </summary>
    public bool StartUPBootSteam
    {
        get => _startUPBootSteam;
        set => SetProperty(ref _startUPBootSteam, value);
    }

    private bool _startRecordLast = true;

    /// <summary>
    /// 是否记录游戏退出位置
    /// </summary>
    [DefaultValue(true)]
    public bool StartRecordLast
    {
        get => _startRecordLast;
        set => SetProperty(ref _startRecordLast, value);
    }

    //private Point _startRecordLastPoint;

    ///// <summary>
    ///// 记录上次退出位置
    ///// </summary>
    //public Point StartRecordLastPoint
    //{
    //    get => _startRecordLastPoint;
    //    set => SetProperty(ref _startRecordLastPoint, value);
    //}

    private ObservablePoint _startRecordPoint;

    /// <summary>
    /// 设置中桌宠启动的位置
    /// </summary>
    public ObservablePoint StartRecordPoint
    {
        get => _startRecordPoint;
        set => SetProperty(ref _startRecordPoint, value);
    }

    private bool _hideFromTaskControl;

    /// <summary>
    /// 在任务切换器(Alt+Tab)中隐藏窗口
    /// </summary>
    public bool HideFromTaskControl
    {
        get => _hideFromTaskControl;
        set => SetProperty(ref _hideFromTaskControl, value);
    }
}
