using Avalonia;
using Avalonia.Media;
using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Timers;

namespace VPet_Simulator.Core.MutiPlatform;

public static class GraphHelper
{
    /// <summary>
    /// 移动状态机需要从桌宠主体获取的能力
    /// </summary>
    /// Windows 版的 Move 直接依赖 Main 这个 WPF 控件, 跨平台侧用这个接口把它隔开.
    /// 与 Windows 版 Move 里用到的 Main 成员一一对应.
    public interface IMoveHost
    {
        /// <summary>
        /// 游戏使用资源
        /// </summary>
        GameCore Core { get; }
        /// <summary>
        /// 是否启用智能移动
        /// </summary>
        bool MoveTimerSmartMove { get; }
        /// <summary>
        /// 每次移动的位移
        /// </summary>
        Point MoveTimerPoint { get; set; }
        /// <summary>
        /// 移动计时器
        /// </summary>
        Timer MoveTimer { get; }
        /// <summary>
        /// 连续播放普通动画的次数
        /// </summary>
        int CountNomal { get; set; }
        /// <summary>
        /// 显示指定动画
        /// </summary>
        void Display(string graphName, GraphInfo.AnimatType animat, Action? endAction);
        /// <summary>
        /// 强制循环播放指定动画的 B 循环
        /// </summary>
        void DisplayBLoopingForce(string graphName);
        /// <summary>
        /// 回到默认状态
        /// </summary>
        void DisplayToNomal();
        /// <summary>
        /// 触发开始移动事件
        /// </summary>
        void Event_MoveStartInvoke(Move move);
        /// <summary>
        /// 触发结束移动事件
        /// </summary>
        void Event_MoveEndInvoke(Move move);
    }

    /// <summary>
    /// LPS 反序列化时统一转成小写
    /// </summary>
    public class LPSConvertToLower : LPSConvert.ConvertFunction
    {
        public override string Convert(dynamic value) => value;

        public override dynamic ConvertBack(string info) => info.ToLowerInvariant();
    }

    public class Work : ICloneable, IWorkDefinition
    {
        public enum WorkType { Work, Study, Play }

        // 数值模拟(PetStatLogic)与 Windows 版编译同一份源码, 通过这个窄接口读取
        // 工作参数. 全部显式实现, 不占用任何公开名字.
        PetWorkKind IWorkDefinition.Kind => (PetWorkKind)Type;
        double IWorkDefinition.MoneyBase => MoneyBase;
        double IWorkDefinition.StrengthFood => StrengthFood;
        double IWorkDefinition.StrengthDrink => StrengthDrink;
        double IWorkDefinition.Feeling => Feeling;

        [Line(ignoreCase: true)]
        public WorkType Type { get; set; }
        [Line(ignoreCase: true)]
        public string Name { get; set; } = string.Empty;
        public string? nametrans = null;
        /// <summary>
        /// 工作名称 已翻译
        /// </summary>
        public string NameTrans
        {
            get
            {
                if (nametrans == null)
                    nametrans = Name.Translate();
                return nametrans;
            }
            set => nametrans = value;
        }
        [Line(ignoreCase: true)]
        public string Graph { get; set; } = string.Empty;
        [Line(ignoreCase: true)]
        public double MoneyBase { get; set; }
        [Line(ignoreCase: true)]
        public double StrengthFood { get; set; }
        [Line(ignoreCase: true)]
        public double StrengthDrink { get; set; }
        [Line(ignoreCase: true)]
        public double Feeling { get; set; }
        [Line(ignoreCase: true)]
        public int LevelLimit { get; set; }
        [Line(ignoreCase: true)]
        public int Time { get; set; }
        [Line(ignoreCase: true)]
        public double FinishBonus { get; set; }

        // 以下是工作计时器的外观与布局参数, 由 Work.SetStyle 套到 WorkTimer 上.
        // 一个都不能少 —— Config 是用 LPSConvert 反序列化的, 少一个字段 MOD 里对应的
        // 设置就会被静默丢弃.
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

        /// <summary>
        /// 把 MOD 里配置的外观套到工作计时器上
        /// </summary>
        /// 与 Windows 版一样直接替换控件自己的资源字典, 这样 XAML 里的
        /// DynamicResource 会重新解析到这份新画刷.
        public void SetStyle(Display.WorkTimer wt)
        {
            wt.Margin = new Thickness(Left, Top, 0, 0);
            wt.Width = Width;
            wt.Height = Width / 300 * 180;
            wt.Resources["WorkBorderBrush"] = MakeBrush("FF", BorderBrush);
            wt.Resources["WorkBackground"] = MakeBrush("FF", Background);
            wt.Resources["WorkButtonBackground"] = MakeBrush("AA", ButtonBackground);
            wt.Resources["WorkButtonBackgroundHover"] = MakeBrush("FF", ButtonBackground);
            wt.Resources["WorkButtonForeground"] = MakeBrush("FF", ButtonForeground);
            wt.Resources["WorkForeground"] = MakeBrush("FF", Foreground);
        }

        /// <summary>
        /// 按 透明度+RGB 拼出画刷
        /// </summary>
        /// MOD 数据里写的是不带 # 的六位十六进制. 解析不了时退回透明而不是抛异常 ——
        /// 一个 MOD 把颜色写错了, 不该让整个工作功能崩掉.
        private static SolidColorBrush MakeBrush(string alpha, string rgb)
        {
            try
            {
                return new SolidColorBrush(Color.Parse("#" + alpha + rgb));
            }
            catch
            {
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        /// <summary>
        /// 显示工作/学习动画
        /// </summary>
        public void Display(IMoveHost m)
        {
            m.Display(Graph, GraphInfo.AnimatType.A_Start, () => m.DisplayBLoopingForce(Graph));
        }

        /// <summary>
        /// 克隆相同的工作/学习
        /// </summary>
        public object Clone() => MemberwiseClone();
    }

    /// <summary>
    /// 移动定义
    /// </summary>
    /// 与 Windows 版 VPet-Simulator.Core/Graph/GraphHelper.cs 的 Move 逐字段对应.
    /// 之前这个类只有 Graph 一个字段, 导致 MOD 宠物配置里的移动参数(速度/方向/
    /// 边缘检测)在反序列化时被全部静默丢弃, 桌宠根本动不起来.
    ///
    /// 刻意不与 Windows 版共用同一个类型: Windows 版的 Move 是 GraphHelper 的嵌套
    /// 类型, 挪到共享程序集会改变它的完整类型名, 从而破坏已编译 MOD 的 ABI.
    public class Move
    {
        /// <summary>
        /// 使用动画名称
        /// </summary>
        [Line(ignoreCase: true, converter: typeof(LPSConvertToLower))]
        public string Graph { get; set; } = "";
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
            // 这里原本误写成 checkType, 会在赋值时改坏 CheckType 而 Mode 本身不变.
            // 全仓没有任何地方调用过这个 setter, 所以修正它不改变现有行为
            set => modeType = (int)value;
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
        public static ModeType GetModeType(IGameSave.ModeType type)
        {
            switch (type)
            {
                case IGameSave.ModeType.Happy:
                    return ModeType.Happy;
                case IGameSave.ModeType.Nomal:
                    return ModeType.Nomal;
                case IGameSave.ModeType.PoorCondition:
                    return ModeType.PoorCondition;
                case IGameSave.ModeType.Ill:
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
        /// 连续播放普通动画多少次后重新随机 (与 Windows 版 Main.TreeRND 一致)
        /// </summary>
        private const int TreeRND = 5;

        /// <summary>
        /// 是否可以触发
        /// </summary>
        public bool Triggered(IMoveHost m)
        {
            var c = m.Core.Controller;
            if (!Mode.HasFlag(GetModeType(m.Core.Save!.Mode))) return false;
            if (TriggerType == DirectionType.None) return true;
            if (TriggerType.HasFlag(DirectionType.Left) && c!.GetWindowsDistanceLeft() > TriggerLeft * c.ZoomRatio)
                return false;
            if (TriggerType.HasFlag(DirectionType.Right) && c!.GetWindowsDistanceRight() > TriggerRight * c.ZoomRatio)
                return false;
            if (TriggerType.HasFlag(DirectionType.Top) && c!.GetWindowsDistanceUp() > TriggerTop * c.ZoomRatio)
                return false;
            if (TriggerType.HasFlag(DirectionType.Bottom) && c!.GetWindowsDistanceDown() > TriggerBottom * c.ZoomRatio)
                return false;
            if (TriggerType.HasFlag(DirectionType.LeftGreater) && c!.GetWindowsDistanceLeft() < TriggerLeft * c.ZoomRatio)
                return false;
            if (TriggerType.HasFlag(DirectionType.RightGreater) && c!.GetWindowsDistanceRight() < TriggerRight * c.ZoomRatio)
                return false;
            if (TriggerType.HasFlag(DirectionType.TopGreater) && c!.GetWindowsDistanceUp() < TriggerTop * c.ZoomRatio)
                return false;
            if (TriggerType.HasFlag(DirectionType.BottomGreater) && c!.GetWindowsDistanceDown() < TriggerBottom * c.ZoomRatio)
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
        public Move? GetCompatibilityMove(IMoveHost main)
        {
            List<Move> ms = new List<Move>();
            bool x = SpeedX > 0;
            bool y = SpeedY > 0;
            foreach (Move m in main.Core.Graph!.GraphConfig!.Moves)
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
        public void Display(IMoveHost m)
        {
            m.Event_MoveStartInvoke(this);
            walklength = 0;
            m.CountNomal = 0;
            m.Display(Graph, GraphInfo.AnimatType.A_Start, () =>
            {
                if (m.MoveTimerSmartMove)
                {
                    switch (LocateType)
                    {
                        case DirectionType.Top:
                            m.Core.Controller!.MoveWindows(0, -m.Core.Controller!.GetWindowsDistanceUp() / m.Core.Controller!.ZoomRatio - LocateLength);
                            break;
                        case DirectionType.Bottom:
                            m.Core.Controller!.MoveWindows(0, m.Core.Controller!.GetWindowsDistanceDown() / m.Core.Controller!.ZoomRatio + LocateLength);
                            break;
                        case DirectionType.Left:
                            m.Core.Controller!.MoveWindows(-m.Core.Controller!.GetWindowsDistanceLeft() / m.Core.Controller!.ZoomRatio - LocateLength, 0);
                            break;
                        case DirectionType.Right:
                            m.Core.Controller!.MoveWindows(m.Core.Controller!.GetWindowsDistanceRight() / m.Core.Controller!.ZoomRatio + LocateLength, 0);
                            break;
                    }
                    m.MoveTimerPoint = new Point(SpeedX, SpeedY);
                    m.MoveTimer.Interval = Interval;
                    m.MoveTimer.Start();
                }
                Displaying(m);
            });
        }
        /// <summary>
        /// 显示正在移动
        /// </summary>
        public void Displaying(IMoveHost m)
        {
            //看看距离是不是不足
            if (!Checked(m.Core.Controller!))
            {//是,停下恢复默认 or/爬墙
                if (Function.Rnd.Next(TreeRND) <= 1)
                {
                    var newmove = GetCompatibilityMove(m);
                    if (newmove != null)
                    {
                        newmove.Display(m);
                        return;
                    }
                }
                StopMoving(m);
                return;
            }
            //不是:继续右边走or停下
            if (Function.Rnd.Next(walklength++) < Distance)
            {
                m.Display(Graph, GraphInfo.AnimatType.B_Loop, () => Displaying(m));
                return;
            }
            else if (Function.Rnd.Next(TreeRND) <= 1)
            {//停下来
                var newmove = GetCompatibilityMove(m);
                if (newmove != null)
                {
                    newmove.Display(m);
                    return;
                }
            }
            StopMoving(m);
        }

        private void StopMoving(IMoveHost m)
        {
            if (m.Core.Controller!.RePositionActive)
                m.Core.Controller!.ResetPosition();
            m.Core.Controller!.RePositionActive = !m.Core.Controller!.CheckPosition();
            m.MoveTimer.Enabled = false;

            m.Display(Graph, GraphInfo.AnimatType.C_End, () => { m.Event_MoveEndInvoke(this); m.DisplayToNomal(); });
        }
    }
}
