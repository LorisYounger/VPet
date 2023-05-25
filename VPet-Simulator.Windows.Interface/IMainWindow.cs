using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        /// 运行动作
        /// </summary>
        /// <param name="action">动作名称</param>
        void RunAction(string action);
    }
}
