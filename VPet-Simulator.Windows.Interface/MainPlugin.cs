using LinePutScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 这是插件的主体内容 请继承这个类
    /// </summary>
    public abstract class MainPlugin
    {
        /// <summary>
        /// 主窗体, 主程序提供的各种功能和设置等 大部分参数和调用均在这里
        /// </summary>
        public IMainWindow MW;
        /// <summary>
        /// MOD插件初始化
        /// </summary>
        /// <param name="mainwin">主窗体</param>
        /// 请不要加载游戏和玩家数据,仅用作初始化
        /// 加载数据(CORE)/游戏(SAVE),请使用 StartGame
        public MainPlugin(IMainWindow mainwin)
        {
            //此处主窗体玩家,Core等信息均为空,请不要加载游戏和玩家数据
            MW = mainwin;
        }
        ///// <summary>//TODO
        ///// 加载游戏主题
        ///// </summary>
        ///// <param name="theme">主题</param>
        //public virtual void LoadTheme(Theme theme) { }
        /// <summary>
        /// 游戏开始 (可以读取Save存档) (如果玩家登出后重新开始游戏,将会被再次调用)
        /// </summary>
        public virtual void StartGame() { }

        /// <summary>
        /// 游戏结束 (可以保存或清空等,不过保存有专门的Save())
        /// </summary>
        public virtual void EndGame() { }

        /// <summary>
        /// 储存游戏 (可以写 Save.Other 储存设置和数据等)
        /// </summary>
        public virtual void Save() { }

        /// <summary>
        /// 打开代码插件设置
        /// </summary>
        public virtual void Setting() { }
        /// <summary>
        /// 重载DIY按钮, 如需添加自定义按钮可在此处添加
        /// </summary>
        public virtual void LoadDIY() { }
    }
}
