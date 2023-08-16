using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.IGraph;
using static VPet_Simulator.Core.Picture;
using System.Windows.Controls;
using System.Windows.Media;
using LinePutScript.Converter;
using System.Windows;
using Panuon.WPF.UI;
using LinePutScript.Localization.WPF;

namespace VPet_Simulator.Core
{
    public static class GraphHelper
    {
        internal static string[][] graphtypevalue = null;
        /// <summary>
        /// 动画类型默认前文本
        /// </summary>
        public static string[][] GraphTypeValue
        {
            get
            {
                if (graphtypevalue == null)
                {
                    List<string[]> gtv = new List<string[]>();
                    foreach (string v in Enum.GetNames(typeof(GraphType)))
                    {
                        gtv.Add(v.ToLower().Split('_'));
                    }
                    graphtypevalue = gtv.ToArray();
                }
                return graphtypevalue;
            }
        }
        /// <summary>
        /// 使用RunImage 从0开始运行该动画 若无RunImage 则使用Run
        /// </summary>
        /// <param name="graph">动画接口</param>
        /// <param name="parant">显示位置</param>
        /// <param name="EndAction">结束方法</param>
        /// <param name="image">额外图片</param>
        public static void Run(this IGraph graph, Border parant, ImageSource image, Action EndAction = null)
        {
            if (graph is IRunImage iri)
            {
                iri.Run(parant, image, EndAction);
            }
            else
            {
                graph.Run(parant, EndAction);
            }
        }
        /// <summary>
        /// 使用ImageRun 指定图像图像控件准备运行该动画
        /// </summary>
        /// <param name="graph">动画接口</param>
        /// <param name="img">用于显示的Image</param>
        /// <param name="EndAction">结束动画</param>
        /// <returns>准备好的线程</returns>
        public static Task Run(this IGraph graph, Image img, Action EndAction = null)
        {
            if (graph is IImageRun iri)
            {
                return iri.Run(img, EndAction);
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// 工作/学习
        /// </summary>
        public class Work
        {
            /// <summary>
            /// 类型
            /// </summary>
            public enum WorkType { Work, Study }
            /// <summary>
            /// 工作/学习
            /// </summary>
            [Line(ignoreCase: true)]
            public WorkType Type { get; set; }
            /// <summary>
            /// 工作名称
            /// </summary>
            [Line(ignoreCase: true)]
            public string Name { get; set; }
            /// <summary>
            /// 工作名称 已翻译
            /// </summary>
            public string NameTrans => Name.Translate();
            /// <summary>
            /// 使用动画名称
            /// </summary>
            [Line(ignoreCase: true)]
            public string Graph { get; set; }
            /// <summary>
            /// 工作盈利/学习等级倍率
            /// </summary>
            [Line(ignoreCase: true)]
            public double MoneyLevel { get; set; }
            /// <summary>
            /// 工作盈利/学习基本倍率
            /// </summary>
            [Line(ignoreCase: true)]
            public double MoneyBase { get; set; }
            /// <summary>
            /// 工作体力(食物)消耗倍率
            /// </summary>
            [Line(ignoreCase: true)]
            public double StrengthFood { get; set; }
            /// <summary>
            /// 工作体力(饮料)消耗倍率
            /// </summary>
            [Line(ignoreCase: true)]
            public double StrengthDrink { get; set; }
            /// <summary>
            /// 心情消耗倍率
            /// </summary>
            [Line(ignoreCase: true)]
            public double Feeling { get; set; }
            /// <summary>
            /// 等级限制
            /// </summary>
            [Line(ignoreCase: true)]
            public int LevelLimit { get; set; }
            /// <summary>
            /// 花费时间(分钟)
            /// </summary>
            [Line(ignoreCase: true)]
            public int Time { get; set; }
            /// <summary>
            /// 完成奖励倍率(0+)
            /// </summary>
            [Line(ignoreCase: true)]
            public double FinishBonus { get; set; }


            [Line(ignoreCase: true)]
            public string BorderBrush = "0290D5";
            [Line(ignoreCase: true)]
            public string Background = "81d4fa";
            [Line(ignoreCase: true)]
            public string ButtonBackground = "0286C6";
            [Line(ignoreCase: true)]
            public string ButtonForeground = "ffffff";
            [Line(ignoreCase: true)]
            public string Foreground = "0286C6";
            [Line(ignoreCase: true)]
            public double Left = 100;
            [Line(ignoreCase: true)]
            public double Top = 160;
            [Line(ignoreCase: true)]
            public double Width = 300;

            public void SetStyle(WorkTimer wt)
            {
                wt.Margin = new Thickness(Left, Top, 0, 0);
                wt.Width = Width;
                wt.Height = Width / 300 * 180;
                wt.Resources.Clear();
                wt.Resources.Add("BorderBrush", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + BorderBrush)));
                wt.Resources.Add("Background", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + Background)));
                wt.Resources.Add("ButtonBackground", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AA" + ButtonBackground)));
                wt.Resources.Add("ButtonBackgroundHover", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + ButtonBackground)));
                wt.Resources.Add("ButtonForeground", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + ButtonForeground)));
                wt.Resources.Add("Foreground", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + Foreground)));
            }
            /// <summary>
            /// 显示工作/学习动画
            /// </summary>
            /// <param name="m"></param>
            public void Display(Main m)
            {
                m.Display(Graph, AnimatType.A_Start, () => m.DisplayBLoopingForce(Graph));
            }
        }

        /// <summary>
        /// 移动
        /// </summary>
        public class Move
        {
            /// <summary>
            /// 使用动画名称
            /// </summary>
            [Line(ignoreCase: true)]
            public string Graph { get; set; }
            /// <summary>
            /// 定位类型
            /// </summary>
            [Flags]
            public enum DirectionType
            {
                None,
                Left,
                Right = 2,
                Top = 4,
                Bottom = 8,
                LeftGreater = 16,
                RightGreater = 32,
                TopGreater = 64,
                BottomGreater = 128,
            }
            /// <summary>
            /// 定位类型: 需要固定到屏幕边缘启用这个
            /// </summary>
            [Line(ignoreCase: true)]
            public DirectionType LocateType { get; set; } = DirectionType.None;
            /// <summary>
            /// 移动间隔
            /// </summary>
            [Line(ignoreCase: true)]
            public int Interval { get; set; } = 125;

            [Line(ignoreCase: true)]
            private int checkType { get; set; }
            /// <summary>
            /// 检查类型
            /// </summary>
            public DirectionType CheckType
            {
                get => (DirectionType)checkType;
                set => checkType = (int)value;
            }
            [Line(ignoreCase: true)]
            private int modeType { get; set; } = 30;

            /// <summary>
            /// 支持的动画模式
            /// </summary>
            public ModeType Mode
            {
                get => (ModeType)modeType;
                set => checkType = (int)value;
            }

            /// <summary>
            /// 宠物状态模式 (Flag版)
            /// </summary>
            [Flags]
            public enum ModeType
            {
                /// <summary>
                /// 高兴
                /// </summary>
                Happy = 2,
                /// <summary>
                /// 普通
                /// </summary>
                Nomal = 4,
                /// <summary>
                /// 状态不佳
                /// </summary>
                PoorCondition = 8,
                /// <summary>
                /// 生病(躺床)
                /// </summary>
                Ill = 16,
            }
            public static ModeType GetModeType(GameSave.ModeType type)
            {
                switch (type)
                {
                    case GameSave.ModeType.Happy:
                        return ModeType.Happy;
                    case GameSave.ModeType.Nomal:
                        return ModeType.Nomal;
                    case GameSave.ModeType.PoorCondition:
                        return ModeType.PoorCondition;
                    case GameSave.ModeType.Ill:
                        return ModeType.Ill;
                    default:
                        return ModeType.Nomal;
                }
            }
            /// <summary>
            /// 检查距离左边
            /// </summary>
            [Line(ignoreCase: true)] public int CheckLeft { get; set; } = 100;
            /// <summary>
            /// 检查距离右边
            /// </summary>
            [Line(ignoreCase: true)] public int CheckRight { get; set; } = 100;
            /// <summary>
            /// 检查距离上面
            /// </summary>
            [Line(ignoreCase: true)] public int CheckTop { get; set; } = 100;
            /// <summary>
            /// 检查距离下面
            /// </summary>
            [Line(ignoreCase: true)] public int CheckBottom { get; set; } = 100;
            /// <summary>
            /// 移动速度(X轴)
            /// </summary>
            [Line(ignoreCase: true)] public int SpeedX { get; set; }
            /// <summary>
            /// 移动速度(Y轴)
            /// </summary>
            [Line(ignoreCase: true)] public int SpeedY { get; set; }
            /// <summary>
            /// 定位位置
            /// </summary>
            [Line(ignoreCase: true)]
            public int LocateLength { get; set; }
            /// <summary>
            /// 移动距离
            /// </summary>
            [Line(ignoreCase: true)] public int Distance { get; set; } = 5;

            [Line(ignoreCase: true)]
            private int triggerType { get; set; }
            /// <summary>
            /// 触发检查类型
            /// </summary>
            public DirectionType TriggerType
            {
                get => (DirectionType)triggerType;
                set => triggerType = (int)value;
            }
            /// <summary>
            /// 检查距离左边
            /// </summary>
            [Line(ignoreCase: true)] public int TriggerLeft { get; set; } = 100;
            /// <summary>
            /// 检查距离右边
            /// </summary>
            [Line(ignoreCase: true)] public int TriggerRight { get; set; } = 100;
            /// <summary>
            /// 检查距离上面
            /// </summary>
            [Line(ignoreCase: true)] public int TriggerTop { get; set; } = 100;
            /// <summary>
            /// 检查距离下面
            /// </summary>
            [Line(ignoreCase: true)] public int TriggerBottom { get; set; } = 100;
            /// <summary>
            /// 是否可以触发
            /// </summary>
            public bool Triggered(Main m)
            {
                var c = m.Core.Controller;
                if (!Mode.HasFlag(GetModeType(m.Core.Save.Mode))) return false;
                if (TriggerType == DirectionType.None) return true;
                if (TriggerType.HasFlag(DirectionType.Left) && c.GetWindowsDistanceLeft() > TriggerLeft * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.Right) && c.GetWindowsDistanceRight() > TriggerRight * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.Top) && c.GetWindowsDistanceUp() > TriggerTop * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.Bottom) && c.GetWindowsDistanceDown() > TriggerBottom * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.LeftGreater) && c.GetWindowsDistanceLeft() < TriggerLeft * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.RightGreater) && c.GetWindowsDistanceRight() < TriggerRight * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.TopGreater) && c.GetWindowsDistanceUp() < TriggerTop * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.BottomGreater) && c.GetWindowsDistanceDown() < TriggerBottom * c.ZoomRatio)
                    return false;
                return true;
            }

            /// <summary>
            /// 是否可以继续动
            /// </summary>
            public bool Checked(IController c)
            {
                if (CheckType == DirectionType.None) return true;
                if (CheckType.HasFlag(DirectionType.Left) && c.GetWindowsDistanceLeft() > CheckLeft * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.Right) && c.GetWindowsDistanceRight() > CheckRight * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.Top) && c.GetWindowsDistanceUp() > CheckTop * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.Bottom) && c.GetWindowsDistanceDown() > CheckBottom * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.LeftGreater) && c.GetWindowsDistanceLeft() < CheckLeft * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.RightGreater) && c.GetWindowsDistanceRight() < CheckRight * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.TopGreater) && c.GetWindowsDistanceUp() < CheckTop * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.BottomGreater) && c.GetWindowsDistanceDown() < CheckBottom * c.ZoomRatio)
                    return false;
                return true;
            }

            int walklength = 0;
            /// <summary>
            /// 获取兼容支持下个播放的移动
            /// </summary>
            public Move GetCompatibilityMove(Main main)
            {
                List<Move> ms = new List<Move>();
                bool x = SpeedX > 0;
                bool y = SpeedY > 0;
                foreach (Move m in main.Core.Graph.GraphConfig.Moves)
                {
                    //if (m == this) continue;
                    int bns = 0;
                    if (SpeedX != 0 && m.SpeedX != 0)
                    {
                        if ((m.SpeedX > 0) != x)
                            bns--;
                        else
                            bns++;
                    }
                    if (SpeedY != 0 && m.SpeedY != 0)
                    {
                        if ((m.SpeedY > 0) != y)
                            bns--;
                        else
                            bns++;
                    }
                    if (bns >= 0 && m.Triggered(main))
                    {
                        ms.Add(m);
                    }
                }
                if (ms.Count == 0) return null;
                return ms[Function.Rnd.Next(ms.Count)];
            }

            /// <summary>
            /// 显示开始移动 (假设已经检查过了)
            /// </summary>
            public void Display(Main m)
            {
                walklength = 0;
                m.CountNomal = 0;
                m.Display(Graph, AnimatType.A_Start, () =>
                {
                    switch (LocateType)
                    {
                        case DirectionType.Top:
                            m.Core.Controller.MoveWindows(0, -m.Core.Controller.GetWindowsDistanceUp() / m.Core.Controller.ZoomRatio - LocateLength);
                            break;
                        case DirectionType.Bottom:
                            m.Core.Controller.MoveWindows(0, m.Core.Controller.GetWindowsDistanceDown() / m.Core.Controller.ZoomRatio + LocateLength);
                            break;
                        case DirectionType.Left:
                            m.Core.Controller.MoveWindows(-m.Core.Controller.GetWindowsDistanceLeft() / m.Core.Controller.ZoomRatio - LocateLength, 0);
                            break;
                        case DirectionType.Right:
                            m.Core.Controller.MoveWindows(m.Core.Controller.GetWindowsDistanceRight() / m.Core.Controller.ZoomRatio + LocateLength, 0);
                            break;
                    }

                    m.MoveTimerPoint = new Point(SpeedX, SpeedY);
                    m.MoveTimer.Interval = Interval;
                    m.MoveTimer.Start();
                    Displaying(m);
                });
            }
            /// <summary>
            /// 显示正在移动
            /// </summary>
            /// <param name="m"></param>
            public void Displaying(Main m)
            {
                //看看距离是不是不足
                if (!Checked(m.Core.Controller))
                {//是,停下恢复默认 or/爬墙
                    if (Function.Rnd.Next(Main.TreeRND) <= 1)
                    {
                        var newmove = GetCompatibilityMove(m);
                        if (newmove != null)
                        {
                            newmove.Display(m);
                            return;
                        }
                    }
                    m.MoveTimer.Enabled = false;
                    m.Display(Graph, AnimatType.C_End, m.DisplayToNomal);
                    return;
                }
                //不是:继续右边走or停下
                if (Function.Rnd.Next(walklength++) < Distance)
                {
                    m.Display(Graph, AnimatType.B_Loop, () => Displaying(m));
                    return;
                }
                else if (Function.Rnd.Next(Main.TreeRND) <= 1)
                {//停下来
                    var newmove = GetCompatibilityMove(m);
                    if (newmove != null)
                    {
                        newmove.Display(m);
                        return;
                    }
                }
                m.MoveTimer.Enabled = false;
                m.Display(Graph, AnimatType.C_End, m.DisplayToNomal);
            }
        }

    }

}
