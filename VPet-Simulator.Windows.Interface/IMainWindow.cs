using LinePutScript.Dictionary;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 游戏主窗体
    /// </summary>
    public interface IMainWindow
    {
        /// <summary>
        /// 存档前缀, 用于多开游戏, 为空时使用默认存档, 不为空时前缀的前缀一般为'-'
        /// </summary>
        string PrefixSave { get; }
        /// <summary>
        /// 启动参数
        /// </summary>
        LPS_D Args { get; }
        /// <summary>
        /// 是否为Steam用户
        /// </summary>
        bool IsSteamUser { get; }
        /// <summary>
        /// SteamID
        /// </summary>
        public ulong SteamID { get; }
        /// <summary>
        /// 游戏设置
        /// </summary>
        ISetting Set { get; }
        /// <summary>
        /// 宠物加载器列表
        /// </summary>
        List<PetLoader> Pets { get; }
        /// <summary>
        /// 所有可用聊天API
        /// </summary>
        List<ITalkAPI> TalkAPI { get; }
        /// <summary>
        /// 当前正在使用的TalkBox
        /// </summary>
        ITalkAPI TalkBoxCurr { get; }
        /// <summary>
        /// 桌宠数据核心
        /// </summary>
        GameCore Core { get; }
        /// <summary>
        /// 桌宠主要部件
        /// </summary>
        Main Main { get; }
        /// <summary>
        /// 版本号
        /// </summary>
        int version { get; }
        /// <summary>
        /// 版本号
        /// </summary>
        string Version { get; }
        /// <summary>
        /// 上次点击时间 (Tick)
        /// </summary>
        long lastclicktime { get; set; }
        /// <summary>
        /// 所有三方插件
        /// </summary>
        List<MainPlugin> Plugins { get; }
        /// <summary>
        /// 所有食物
        /// </summary>
        List<Food> Foods { get; }
        /// <summary>
        /// 需要食物时会说的话
        /// </summary>
        List<LowText> LowFoodText { get; }
        /// <summary>
        /// 需要饮料时会说的话
        /// </summary>
        List<LowText> LowDrinkText { get; }
        /// <summary>
        /// 点击时会说的话
        /// </summary>
        List<ClickText> ClickTexts { get; }
        /// <summary>
        /// 选择说的话
        /// </summary>
        List<SelectText> SelectTexts { get; }
        /// <summary>
        /// 获得自动点击的文本
        /// </summary>
        /// <returns>说话内容</returns>
        ClickText GetClickText();
        /// <summary>
        /// 图片资源
        /// </summary>
        ImageResources ImageSources { get; }
        /// <summary>
        /// 文件资源, 储存的为文件路径 : 可以给代码插件MOD用
        /// </summary>
        Resources FileSources { get; }
        /// <summary>
        /// 设置游戏缩放倍率
        /// </summary>
        /// <param name="zl">缩放倍率 范围0.1-10</param>
        void SetZoomLevel(double zl);
        /// <summary>
        /// 保存设置
        /// </summary>
        void Save();
        /// <summary>
        /// 加载DIY内容
        /// </summary>
        void LoadDIY();
        /// <summary>
        /// 显示设置页面
        /// </summary>
        /// <param name="page">设置页</param>
        void ShowSetting(int page = -1);
        /// <summary>
        /// 显示更好买页面
        /// </summary>
        /// <param name="type">食物类型</param>
        void ShowBetterBuy(Food.FoodType type);
        /// <summary>
        /// 关闭桌宠
        /// </summary>
        void Close();
        /// <summary>
        /// 重启桌宠
        /// </summary>
        void Restart();
        /// <summary>
        /// 鼠标穿透
        /// </summary>
        bool MouseHitThrough { get; set; }

        /// <summary>
        /// 存档 Hash检查 是否通过
        /// </summary>
        bool HashCheck { get; }

        /// <summary>
        /// 获得当前系统音乐播放音量
        /// </summary>
        float AudioPlayingVolume();
        /// <summary>
        /// 关闭指示器,默认为true
        /// </summary>
        bool CloseConfirm { get; }
        /// <summary>
        /// 关闭该玩家的HashCheck检查
        /// 如果你的mod属于作弊mod/有作弊内容,请在作弊前调用这个方法
        /// </summary>
        void HashCheckOff();
        /// <summary>
        /// 游戏打开过的窗口, 会在退出时统一调用退出
        /// </summary>
        List<Window> Windows { get; set; }
        /// <summary>
        /// 游戏存档数据
        /// </summary>
        GameSave_v2 GameSavesData { get; }
        /// <summary>
        /// 主窗体 Grid
        /// </summary>
        Grid MGHost { get; }
        /// <summary>
        /// 主窗体 Pet Grid
        /// </summary>
        Grid PetGrid { get; }
        /// <summary>
        /// 当创建/加入新的多人联机窗口(访客表)时触发
        /// 如果你想写联机功能,请监听这个事件
        /// </summary>
        event Action<IMPWindows> MutiPlayerHandle;
        /// <summary>
        /// 当创建/加入新的多人联机窗口(访客表)时触发
        /// 用于给MOD定义自己的联机窗口时准备的, 一般联机功能不需要调用这个
        /// </summary>
        /// <param name="mp"></param>
        void MutiPlayerStart(IMPWindows mp);

        /// <summary>
        /// 显示吃东西(夹层)动画
        /// </summary>
        /// <param name="graphName">夹层动画名</param>
        /// <param name="imageSource">被夹在中间的图片</param>
        void DisplayFoodAnimation(string graphName, ImageSource imageSource);
        /// <summary>
        /// 使用/食用物品 (自动扣钱) (不包括显示动画)
        /// </summary>
        /// <param name="item">物品</param>
        void TakeItem(Food item);

        /// <summary>
        /// 显示输入框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="text">文本</param>
        /// <param name="defaulttext">默认文本</param>
        /// <param name="ENDAction">结束事件</param>
        /// <param name="AllowMutiLine">是否允许多行输入</param>
        /// <param name="TextCenter">文本居中</param>
        /// <param name="CanHide">能否隐藏</param>
        void ShowInputBox(string title, string text, string defaulttext, Action<string> ENDAction, bool AllowMutiLine = false, bool TextCenter = true, bool CanHide = false);
        /// <summary>
        /// UI线程调用位置
        /// </summary>
        Dispatcher Dispatcher { get; }
    }

}
