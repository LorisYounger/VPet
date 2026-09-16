//跨平台: 对应 VPet-Simulator.Windows.Interface/IMainWindow.cs, 成员逐条相同, 只把 WPF 的类型换成 Avalonia 的
//(Dispatcher / Window / Grid / ImageSource→Bitmap). 老式 MainPlugin 与 ITalkAPI 那几项没有: 旧 WPF MOD 在别的平台
//跑不起来, 跨平台 MOD 走统一契约 (UnifiedPlugin).
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LinePutScript.Dictionary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 主窗体接口
    /// </summary>
    public interface IMainWindow
    {
        /// <summary>
        /// 多开前缀
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
        /// Steam用户ID
        /// </summary>
        public ulong SteamID { get; }
        /// <summary>
        /// SteamAccountId
        /// </summary>
        public uint SteamAuthorID { get; }
        /// <summary>
        /// 游戏设置
        /// </summary>
        ISetting Set { get; }
        /// <summary>
        /// 宠物加载器
        /// </summary>
        List<PetLoader> Pets { get; }
        /// <summary>
        /// 游戏核心
        /// </summary>
        GameCore Core { get; }
        /// <summary>
        /// 桌宠主体
        /// </summary>
        Main Main { get; }
        /// <summary>
        /// 版本号
        /// </summary>
        int version { get; }
        /// <summary>
        /// 版本号 (文本)
        /// </summary>
        string Version { get; }
        /// <summary>
        /// 上次点击时间
        /// </summary>
        long lastclicktime { get; set; }
        /// <summary>
        /// 食物
        /// </summary>
        List<Food> Foods { get; }
        /// <summary>
        /// 低食物文本
        /// </summary>
        List<LowText> LowFoodText { get; }
        /// <summary>
        /// 低水文本
        /// </summary>
        List<LowText> LowDrinkText { get; }
        /// <summary>
        /// 点击文本
        /// </summary>
        List<ClickText> ClickTexts { get; }
        /// <summary>
        /// 选择文本
        /// </summary>
        List<SelectText> SelectTexts { get; }
        /// <summary>
        /// 获得自动点击的文本
        /// </summary>
        ClickText? GetClickText();
        /// <summary>
        /// 照片
        /// </summary>
        List<Photo> Photos { get; }
        /// <summary>
        /// 图片资源
        /// </summary>
        ImageResources ImageSources { get; }
        /// <summary>
        /// 文件资源
        /// </summary>
        Resources FileSources { get; }
        /// <summary>
        /// 设置缩放
        /// </summary>
        void SetZoomLevel(double zl);
        /// <summary>
        /// 保存
        /// </summary>
        void Save();
        /// <summary>
        /// 重载DIY按钮
        /// </summary>
        void LoadDIY();
        /// <summary>
        /// 显示设置
        /// </summary>
        void ShowSetting(int page = -1);
        /// <summary>
        /// 显示商店
        /// </summary>
        void ShowBetterBuy(Food.FoodType type);
        /// <summary>
        /// 显示图库
        /// </summary>
        void ShowGallery();
        /// <summary>
        /// 关闭
        /// </summary>
        void Close();
        /// <summary>
        /// 重启
        /// </summary>
        void Restart();
        /// <summary>
        /// 鼠标穿透
        /// </summary>
        bool MouseHitThrough { get; set; }
        /// <summary>
        /// 是否开了防作弊
        /// </summary>
        bool HashCheck { get; }
        /// <summary>
        /// 关闭确认
        /// </summary>
        bool CloseConfirm { get; }
        /// <summary>
        /// 关闭防作弊
        /// </summary>
        void HashCheckOff();
        /// <summary>
        /// 开着的子窗口
        /// </summary>
        List<Window> Windows { get; }
        /// <summary>
        /// 存档
        /// </summary>
        GameSave_v2 GameSavesData { get; }
        /// <summary>
        /// 主窗口的承载网格
        /// </summary>
        Grid MGHost { get; }
        /// <summary>
        /// 桌宠所在的网格
        /// </summary>
        Grid PetGrid { get; }
        /// <summary>
        /// 显示食物动画
        /// </summary>
        void DisplayFoodAnimation(string graphName, Bitmap imageSource);
        /// <summary>
        /// 使用物品
        /// </summary>
        void TakeItem(Food item);
        /// <summary>
        /// 显示输入框
        /// </summary>
        void ShowInputBox(string title, string text, string defaulttext, Action<string> ENDAction, bool AllowMutiLine = false, bool TextCenter = true, bool CanHide = false);
        /// <summary>
        /// 调度器
        /// </summary>
        Dispatcher Dispatcher { get; }
        /// <summary>
        /// 全部 MOD 信息
        /// </summary>
        IEnumerable<IModInfo> ModInfo { get; }
        /// <summary>
        /// 启用的 MOD
        /// </summary>
        IEnumerable<IModInfo> OnModInfo { get; }
        /// <summary>
        /// MOD 路径
        /// </summary>
        List<DirectoryInfo> MODPath { get; }
        /// <summary>
        /// 日程表
        /// </summary>
        ScheduleTask ScheduleTask { get; }
        /// <summary>
        /// 日程表套餐
        /// </summary>
        List<ScheduleTask.PackageFull> SchedulePackage { get; }
        /// <summary>
        /// 使用物品事件
        /// </summary>
        event Action<Food> Event_TakeItem;
        /// <summary>
        /// 新的一天事件
        /// </summary>
        event Action Event_NewDay;
        /// <summary>
        /// 跨 MOD 共享的资源
        /// </summary>
        Dictionary<string, object> DynamicResources { get; }
        /// <summary>
        /// 生成验证密钥
        /// </summary>
        Task<int> GenerateAuthKey();
        /// <summary>
        /// 记录物品使用
        /// </summary>
        public void TakeItemHandle(Food item, int count, string from);
        /// <summary>
        /// 活动日志
        /// </summary>
        public ObservableCollection<ActivityLog> ActivityLogs { get; }
        /// <summary>
        /// 背包
        /// </summary>
        public List<Item> Items { get; }
        /// <summary>
        /// 往背包里加物品
        /// </summary>
        public void ItemsAdd(Item item);
        /// <summary>
        /// 上次使用物品时间
        /// </summary>
        public DateTime LastTakeItemTime { get; }
    }
}
