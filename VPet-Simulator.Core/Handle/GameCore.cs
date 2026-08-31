using System;
using System.Windows;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 游戏使用资源
    /// </summary>
    public class GameCore : GameCoreBase<TouchArea>
    {
        /// <summary>
        /// 图形核心
        /// </summary>
        public GraphCore? Graph;
    }

    /// <summary>
    /// 触摸范围事件
    /// </summary>
    public class TouchArea : TouchAreaBase
    {
        /// <summary>
        /// 位置
        /// </summary>
        public Point Locate
        {
            get => new Point(LocateX, LocateY);
            set
            {
                LocateX = value.X;
                LocateY = value.Y;
            }
        }

        /// <summary>
        /// 大小
        /// </summary>
        public Size Size
        {
            get => new Size(Width, Height);
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        /// 创建个触摸范围事件
        /// </summary>
        /// <param name="locate">位置</param>
        /// <param name="size">大小</param>
        /// <param name="doAction">如果是触发的内容</param>
        /// <param name="isPress">否:立即触发/是:长按触发</param>
        public TouchArea(Point locate, Size size, Func<bool> doAction, bool isPress = false)
            : base(locate.X, locate.Y, size.Width, size.Height, doAction, isPress)
        {
        }

        /// <summary>
        /// 判断是否成功触发该点击事件
        /// </summary>
        /// <param name="point">位置</param>
        /// <returns>是否成功</returns>
        public bool Touch(Point point)
        {
            return Touch(point.X, point.Y);
        }
    }
}
