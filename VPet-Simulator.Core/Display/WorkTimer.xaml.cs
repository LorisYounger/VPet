using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LinePutScript.Localization.WPF;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;

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
        /// UI相关显示
        /// </summary>
        /// <param name="m"></param>
        private void M_TimeUIHandle(Main m)
        {
            if (Visibility == Visibility.Collapsed) return;
            TimeSpan ts = DateTime.Now - StartTime;
            TimeSpan tleft;
            if (ts.TotalMinutes > nowWork.Time)
            {
                //学完了,停止
                //ts = TimeSpan.FromMinutes(MaxTime);
                //tleft = TimeSpan.Zero;
                //PBLeft.Value = MaxTime;
                if (nowWork.Type == Work.WorkType.Work)
                {
                    m.Core.Save.Money += GetCount * nowWork.FinishBonus;
                    Stop(() => m.SayRnd(LocalizeCore.Translate("{2}完成啦, 累计赚了 {0:f2} 金钱\n共计花费了{1}分钟", GetCount * (1 + nowWork.FinishBonus),
                        nowWork.Time, nowWork.NameTrans), true));
                }
                else
                    m.Core.Save.Money += GetCount * nowWork.FinishBonus;
                Stop(() => m.SayRnd(LocalizeCore.Translate("{2}完成啦, 累计获得 {0:f2} 经验\n共计花费了{1}分钟", GetCount * (1 + nowWork.FinishBonus),
                    nowWork.Time, nowWork.NameTrans), true));
                return;
            }
            else
            {
                tleft = TimeSpan.FromMinutes(nowWork.Time) - ts;
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
                    if (nowWork.Type == Work.WorkType.Work)
                        tNumberUnit.Text = LocalizeCore.Translate("钱");
                    else
                        tNumberUnit.Text = LocalizeCore.Translate("EXP");
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
                DisplayBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                DisplayBorder.Visibility = Visibility.Visible;
                btnStop.Content = LocalizeCore.Translate("停止") + nowWork.NameTrans;
                switch (DisplayType)
                {
                    default:
                    case 0:
                        tNow.Text = LocalizeCore.Translate("当前已") + nowWork.NameTrans;
                        break;
                    case 1:
                        tNow.Text = LocalizeCore.Translate("剩余{0}时间", nowWork.NameTrans);
                        break;
                    case 2:
                        if (nowWork.Type == Work.WorkType.Work)
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
            m.StateID = m.Core.Graph.GraphConfig.Works.IndexOf(work);
            StartTime = DateTime.Now;
            GetCount = 0;

            work.SetStyle(this);
            work.Display(m);

            PBLeft.Maximum = work.Time;
            nowWork = work;
            DisplayUI();
        }
        private Work nowWork;
        public void Stop(Action @then = null)
        {
            Visibility = Visibility.Collapsed;
            m.State = Main.WorkingState.Nomal;
            m.Display(nowWork.Graph, AnimatType.C_End, then ?? m.DisplayNomal);
        }
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }
    }
}
