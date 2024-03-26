using LinePutScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 设置方法接口
    /// </summary>
    public interface ISetting
    {
        /// <summary>
        /// 获取当前缩放倍率
        /// </summary>
        double ZoomLevel { get; }

        /// <summary>
        /// 设置缩放倍率
        /// </summary>
        /// <param name="level">缩放等级</param>
        void SetZoomLevel(double level);

        /// <summary>
        /// 获取当前播放声音的大小
        /// </summary>
        double VoiceVolume { get; }

        /// <summary>
        /// 设置播放声音的大小
        /// </summary>
        /// <param name="volume">声音大小</param>
        void SetVoiceVolume(double volume);

        /// <summary>
        /// 获取当前自动保存的频率（分钟）
        /// </summary>
        int AutoSaveInterval { get; }

        /// <summary>
        /// 设置自动保存的频率（分钟）
        /// </summary>
        /// <param name="interval">保存间隔</param>
        void SetAutoSaveInterval(int interval);

        /// <summary>
        /// 获取或设置备份保存的最大数量
        /// </summary>
        int BackupSaveMaxNum { get; set; }

        /// <summary>
        /// 获取当前是否置于顶层
        /// </summary>
        bool TopMost { get; }

        /// <summary>
        /// 设置是否置于顶层
        /// </summary>
        /// <param name="topMost">是否置顶</param>
        void SetTopMost(bool topMost);

        /// <summary>
        /// 获取或设置上次清理缓存的日期
        /// </summary>
        DateTime LastCacheDate { get; set; }

        /// <summary>
        /// 获取当前语言
        /// </summary>
        string Language { get; }

        /// <summary>
        /// 设置语言
        /// </summary>
        /// <param name="language">语言代码</param>
        void SetLanguage(string language);

        /// <summary>
        /// 获取或设置按多久视为长按（毫秒）
        /// </summary>
        int PressLength { get; set; }

        /// <summary>
        /// 获取或设置互动周期
        /// </summary>
        int InteractionCycle { get; set; }

        /// <summary>
        /// 获取当前计算间隔（秒）
        /// </summary>
        double LogicInterval { get; }

        /// <summary>
        /// 设置计算间隔（秒）
        /// </summary>
        /// <param name="interval">计算间隔</param>
        void SetLogicInterval(double interval);

        /// <summary>
        /// 获取当前是否允许移动
        /// </summary>
        bool AllowMove { get; }

        /// <summary>
        /// 设置是否允许移动
        /// </summary>
        /// <param name="allowMove">是否允许移动</param>
        void SetAllowMove(bool allowMove);

        /// <summary>
        /// 获取当前是否启用智能移动
        /// </summary>
        bool SmartMove { get; }

        /// <summary>
        /// 设置是否启用智能移动
        /// </summary>
        /// <param name="smartMove">是否启用智能移动</param>
        void SetSmartMove(bool smartMove);

        /// <summary>
        /// 获取当前是否启用计算等数据功能
        /// </summary>
        bool EnableFunction { get; }

        /// <summary>
        /// 设置是否启用计算等数据功能
        /// </summary>
        /// <param name="enableFunction">是否启用功能</param>
        void SetEnableFunction(bool enableFunction);

        /// <summary>
        /// 获取当前智能移动周期（秒）
        /// </summary>
        int SmartMoveInterval { get; }

        /// <summary>
        /// 设置智能移动周期（秒）
        /// </summary>
        /// <param name="interval">智能移动周期</param>
        void SetSmartMoveInterval(int interval);

        /// <summary>
        /// 获取或设置消息框是否外置
        /// </summary>
        bool MessageBarOutside { get; set; }

        /// <summary>
        /// 获取当前是否记录游戏退出位置
        /// </summary>
        bool StartRecordLast { get; set; }

        /// <summary>
        /// 获取上次退出位置
        /// </summary>
        Point StartRecordLastPoint { get; }

        /// <summary>
        /// 获取或设置桌宠启动的位置
        /// </summary>
        Point StartRecordPoint { get; set; }

        /// <summary>
        /// 获取或设置当实时播放音量达到该值时运行音乐动作
        /// </summary>
        double MusicCatch { get; set; }

        /// <summary>
        /// 获取或设置当实时播放音量达到该值时运行特殊音乐动作
        /// </summary>
        double MusicMax { get; set; }

        /// <summary>
        /// 获取或设置桌宠图形渲染的分辨率，越高图形越清晰，重启后生效
        /// </summary>
        int Resolution { get; set; }

        /// <summary>
        /// 获取或设置是否允许桌宠自动购买食品
        /// </summary>
        bool AutoBuy { get; set; }

        /// <summary>
        /// 获取或设置是否允许桌宠自动购买礼物
        /// </summary>
        bool AutoGift { get; set; }

        /// <summary>
        /// 获取或设置在任务切换器(Alt+Tab)中是否隐藏窗口，重启后生效
        /// </summary>
        bool HideFromTaskControl { get; set; }
       
        /// <summary>
        /// 读写自定义游戏设置(给mod准备的接口)
        /// </summary>
        /// <param name="lineName">游戏设置</param>
        /// <returns>如果找到相同名称的第一个Line,则为该Line; 否则为新建的相同名称Line</returns>
        ILine this[string lineName] { get; set; }

        /// <summary>
        /// 联机允许交互
        /// </summary>
        bool MPNOTouch { get; set; }
    }

}
