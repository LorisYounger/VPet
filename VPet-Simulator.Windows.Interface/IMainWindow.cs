using LinePutScript;
using LinePutScript.Dictionary;
using System.Collections.Generic;
using System.Windows.Media;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 游戏主窗体
    /// </summary>
    public interface IMainWindow
    {
        /// <summary>
        /// 是否为Steam用户
        /// </summary>
        bool IsSteamUser { get; }
        /// <summary>
        /// 游戏设置
        /// </summary>
        Setting Set { get; }
        /// <summary>
        /// 宠物加载器列表
        /// </summary>
        List<PetLoader> Pets { get; }
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
        int verison { get; }
        /// <summary>
        /// 版本号
        /// </summary>
        string Verison { get; }
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
        /// 获得自动点击的文本
        /// </summary>
        /// <returns>说话内容</returns>
        ClickText GetClickText();
        /// <summary>
        /// 图片资源
        /// </summary>
        ImageResources ImageSources { get; }
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
    }

}
