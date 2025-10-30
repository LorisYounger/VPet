using LinePutScript.Localization.WPF;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.WorkTimer.FinishWorkInfo;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// WorkTimer.xaml 的交互逻辑
    /// </summary>
    public partial class WorkTimer : Viewbox
    {
        Main m;
        public WorkTimer(Main m)
        {
            InitializeComponent();
            this.m = m;
            //数据相关计算挪到MainLogic
            //这里只显示UI
            m.TimeUIHandle += M_TimeUIHandle;
        }
        /// <summary>
        /// 显示模式
        /// 0 = 默认
        /// 1 = 剩余时间
        /// 2 = 已获取(金钱/等级)
        /// </summary>
        public int DisplayType = 0;
        /// <summary>
        /// 累计获得的钱/经验值
        /// </summary>
        public double GetCount;
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime;
        /// <summary>
        /// 完成工作信息
        /// </summary>
        public struct FinishWorkInfo
        {
            /// <summary>
            /// 当前完成工作
            /// </summary>
            public Work work;
            /// <summary>
            /// 当前完成工作收入
            /// </summary>
            public double count;
            /// <summary>
            /// 当前完成工作花费时间 (分钟)
            /// </summary>
            public double spendtime;
            /// <summary>
            /// 停止工作的原因
            /// </summary>
            public enum StopReason
            {
                /// <summary>
                /// 时间结束完成
                /// </summary>
                TimeFinish,
                /// <summary>
                /// 玩家手动停止
                /// </summary>
                MenualStop,
                /// <summary>
                /// 因为状态等停止
                /// </summary>
                StateFail,
                /// <summary>
                /// 其他原因
                /// </summary>
                Other,
            }
            /// <summary>
            /// 停止原因
            /// </summary>
            public StopReason Reason;
            /// <summary>
            /// 完成工作信息
            /// </summary>
            /// <param name="work">当前工作</param>
            /// <param name="count">当前盈利(自动计算附加)</param>
            public FinishWorkInfo(Work work, double count, StopReason reason)
            {
                this.work = work;
                this.count = count * (1 + work.FinishBonus);
                this.spendtime = work.Time;
                this.Reason = reason;
            }
            /// <summary>
            /// 完成工作信息
            /// </summary>
            /// <param name="work">当前工作</param>
            /// <param name="count">当前盈利(自动计算附加)</param>
            public FinishWorkInfo(Work work, double count, DateTime starttime, StopReason reason)
            {
                this.work = work;
                this.count = count * (1 + work.FinishBonus);
                this.spendtime = DateTime.Now.Subtract(starttime).TotalMinutes;
                this.Reason = reason;
            }
        }
        /// <summary>
        /// UI相关显示
        /// </summary>
        /// <param name="m"></param>
        private void M_TimeUIHandle(Main m)
        {
            if (Visibility == Visibility.Collapsed) return;
            TimeSpan ts = DateTime.Now - StartTime;
            TimeSpan tleft;
            if (ts.TotalMinutes > m.NowWork.Time)
            {
                //学完了,停止
                //ts = TimeSpan.FromMinutes(MaxTime);
                //tleft = TimeSpan.Zero;
                //PBLeft.Value = MaxTime;
                FinishWorkInfo fwi = new FinishWorkInfo(m.NowWork, GetCount, FinishWorkInfo.StopReason.TimeFinish);
                if (m.NowWork.Type == Work.WorkType.Work)
                {
                    m.Core.Save.Money += GetCount * m.NowWork.FinishBonus;
                    Stop(() => m.SayRnd(LocalizeCore.Translate("{2}完成啦, 累计赚了 {0:f2} 金钱\n共计花费了{1}分钟", fwi.count,
                        fwi.spendtime, fwi.work.NameTrans), true), StopReason.TimeFinish);
                }
                else
                {
                    m.Core.Save.Exp += GetCount * m.NowWork.FinishBonus;
                    Stop(() => m.SayRnd(LocalizeCore.Translate("{2}完成啦, 累计获得 {0:f2} 经验\n共计花费了{1}分钟", fwi.count,
                        fwi.spendtime, fwi.work.NameTrans), true), StopReason.TimeFinish);
                }
                return;
            }
            else
            {
                tleft = TimeSpan.FromMinutes(m.NowWork.Time) - ts;
                PBLeft.Value = ts.TotalMinutes;
            }
            switch (DisplayType)
            {
                default:
                case 0:
                    ShowTimeSpan(ts); break;
                case 1:
                    ShowTimeSpan(tleft); break;
                case 2:
                    tNumber.Text = GetCount.ToString("f0");
                    if (m.NowWork.Type == Work.WorkType.Work)
                        tNumberUnit.Text = LocalizeCore.Translate("钱");
                    else
                        tNumberUnit.Text = "EXP";
                    break;
                case 3:
                    break;
            }
        }
        public void ShowTimeSpan(TimeSpan ts)
        {
            if (ts.TotalSeconds < 90)
            {
                tNumber.Text = ts.TotalSeconds.ToString("f1");
                tNumberUnit.Text = LocalizeCore.Translate("秒");
            }
            else if (ts.TotalMinutes < 90)
            {
                tNumber.Text = ts.TotalMinutes.ToString("f1");
                tNumberUnit.Text = LocalizeCore.Translate("分钟");
            }
            else
            {
                tNumber.Text = ts.TotalHours.ToString("f1");
                tNumberUnit.Text = LocalizeCore.Translate("小时");
            }
        }
        public void DisplayUI()
        {
            if (DisplayType == 3)
            {
                btnSwitch.Opacity = 0.5;
                DisplayBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnSwitch.Opacity = 1;
                DisplayBorder.Visibility = Visibility.Visible;
                btnStop.Content = LocalizeCore.Translate("停止") + m.NowWork.NameTrans;
                switch (DisplayType)
                {
                    default:
                    case 0:
                        tNow.Text = LocalizeCore.Translate("当前已{0}", m.NowWork.NameTrans);
                        break;
                    case 1:
                        tNow.Text = LocalizeCore.Translate("剩余{0}时间", m.NowWork.NameTrans);
                        break;
                    case 2:
                        if (m.NowWork.Type == Work.WorkType.Work)
                            tNow.Text = LocalizeCore.Translate("累计金钱收益");
                        else
                            tNow.Text = LocalizeCore.Translate("获得经验值");
                        break;
                }
            }
            M_TimeUIHandle(m);
        }
        private void SwitchState_Click(object sender, RoutedEventArgs e)
        {
            DisplayType++;
            if (DisplayType >= 4)
                DisplayType = 0;
            DisplayUI();
        }
        public void Start(Work work)
        {
            //if (state == Main.WorkingState.Nomal)
            //    return;
            Visibility = Visibility.Visible;
            m.State = Main.WorkingState.Work;
            m.NowWork = work;
            StartTime = DateTime.Now;
            GetCount = 0;

            work.SetStyle(this);
            work.Display(m);
            m.Event_WorkStartInvoke(work);

            PBLeft.Maximum = work.Time;
            DisplayUI();
        }
        /// <summary>
        /// 停止工作
        /// </summary>
        /// <param name="then"></param>
        public void Stop(Action @then = null, StopReason reason = StopReason.MenualStop)
        {
            if (m.State == Main.WorkingState.Work && m.NowWork != null)
            {
                FinishWorkInfo fwi = new FinishWorkInfo(m.NowWork, GetCount, StartTime, reason);
                E_FinishWork?.Invoke(fwi);
            }
            Visibility = Visibility.Collapsed;
            m.State = Main.WorkingState.Nomal;
            m.Display(m.NowWork.Graph, AnimatType.C_End, then ?? m.DisplayNomal);
        }
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop(reason: StopReason.MenualStop);
        }
        /// <summary>
        /// 任务完成时调用该参数
        /// </summary>
        public event Action<FinishWorkInfo> E_FinishWork;
    }
}
