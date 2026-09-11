using Avalonia.Controls;
using Avalonia.Interactivity;
using LinePutScript.Localization;
using System;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.MutiPlatform.Display.WorkTimer.FinishWorkInfo;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 工作计时器
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Core/Display/WorkTimer.xaml.cs.
/// 数值结算不在这里, 在两个平台共用的 PetStatLogic 里; 这里只负责显示和起止.
public partial class WorkTimer : UserControl
{
    private readonly PetMain m;

    /// <summary>
    /// 无参构造仅供 Avalonia 设计器使用
    /// </summary>
    public WorkTimer() : this(null!)
    {
    }

    public WorkTimer(PetMain m)
    {
        InitializeComponent();
        this.m = m;
        //数据相关计算挪到 PetMainLogic
        //这里只显示UI
        if (m != null)
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
        public GraphHelper.Work work;
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
        public FinishWorkInfo(GraphHelper.Work work, double count, StopReason reason)
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
        public FinishWorkInfo(GraphHelper.Work work, double count, DateTime starttime, StopReason reason)
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
    private void M_TimeUIHandle(PetMain m)
    {
        if (!IsVisible || m.NowWork == null) return;
        TimeSpan ts = DateTime.Now - StartTime;
        TimeSpan tleft;
        if (ts.TotalMinutes > m.NowWork?.Time)
        {
            //干完了,停止
            FinishWorkInfo fwi = new FinishWorkInfo(m.NowWork, GetCount, StopReason.TimeFinish);
            if (m.NowWork.Type == GraphHelper.Work.WorkType.Work)
            {
                m.Core.Save!.Money += GetCount * m.NowWork.FinishBonus;
                Stop(() => m.SayRnd(LocalizeCore.Translate("{2}完成啦, 累计赚了 {0:f2} 金钱\n共计花费了{1}分钟", fwi.count,
                    fwi.spendtime, fwi.work.NameTrans), true), StopReason.TimeFinish);
            }
            else
            {
                m.Core.Save!.Exp += GetCount * m.NowWork.FinishBonus;
                Stop(() => m.SayRnd(LocalizeCore.Translate("{2}完成啦, 累计获得 {0:f2} 经验\n共计花费了{1}分钟", fwi.count,
                    fwi.spendtime, fwi.work.NameTrans), true), StopReason.TimeFinish);
            }
            return;
        }
        else
        {
            tleft = TimeSpan.FromMinutes(m.NowWork!.Time) - ts;
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
                if (m.NowWork!.Type == GraphHelper.Work.WorkType.Work)
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
            DisplayBorder.IsVisible = false;
        }
        else
        {
            btnSwitch.Opacity = 1;
            DisplayBorder.IsVisible = true;
            btnStop.Content = LocalizeCore.Translate("停止") + m.NowWork!.NameTrans;
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
                    if (m.NowWork.Type == GraphHelper.Work.WorkType.Work)
                        tNow.Text = LocalizeCore.Translate("累计金钱收益");
                    else
                        tNow.Text = LocalizeCore.Translate("获得经验值");
                    break;
            }
        }
        M_TimeUIHandle(m);
    }

    private void SwitchState_Click(object? sender, RoutedEventArgs e)
    {
        DisplayType++;
        if (DisplayType >= 4)
            DisplayType = 0;
        DisplayUI();
    }

    public void Start(GraphHelper.Work work)
    {
        IsVisible = true;
        m.State = PetMain.WorkingState.Work;
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
    /// <param name="then">停止后接着做什么</param>
    public void Stop(Action? then = null, StopReason reason = StopReason.MenualStop)
    {
        if (m.State == PetMain.WorkingState.Work && m.NowWork != null)
        {
            FinishWorkInfo fwi = new FinishWorkInfo(m.NowWork, GetCount, StartTime, reason);
            E_FinishWork?.Invoke(fwi);
        }
        IsVisible = false;
        m.State = PetMain.WorkingState.Nomal;
        m.Display(m.NowWork?.Graph, AnimatType.C_End, then ?? m.DisplayNomal);
    }

    private void btnStop_Click(object? sender, RoutedEventArgs e)
    {
        Stop(reason: StopReason.MenualStop);
    }

    /// <summary>
    /// 任务完成时调用该参数
    /// </summary>
    public event Action<FinishWorkInfo>? E_FinishWork;
}
