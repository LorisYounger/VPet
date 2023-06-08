using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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
        /// 最大时间(分钟)
        /// </summary>
        public int MaxTime;
        /// <summary>
        /// UI相关显示
        /// </summary>
        /// <param name="m"></param>
        private void M_TimeUIHandle(Main m)
        {
            if (Visibility == Visibility.Hidden) return;
            TimeSpan ts = DateTime.Now - StartTime;
            TimeSpan tleft;
            if (ts.TotalMinutes > MaxTime)
            {
                //学完了,停止
                //ts = TimeSpan.FromMinutes(MaxTime);
                //tleft = TimeSpan.Zero;
                //PBLeft.Value = MaxTime;
                switch (m.State)
                {
                    case Main.WorkingState.Study:
                        m.Core.Save.Money += GetCount * 0.2;
                        Stop(() => m.Say($"学习完成啦, 累计学会了 {(GetCount * 1.2):f2} EXP\n共计花费了{MaxTime}分钟"));
                        break;
                    case Main.WorkingState.WorkONE:
                        m.Core.Save.Money += GetCount * 0.15;
                        Stop(() => m.Say($"{m.Core.Graph.GraphConfig.Str[(gstr)"work1"]}完成啦, 累计赚了 {GetCount * 1.15:f2} 金钱\n共计花费了{MaxTime}分钟"));
                        break;
                    case Main.WorkingState.WorkTWO:
                        m.Core.Save.Money += GetCount * 0.25;
                        Stop(() => m.Say($"{m.Core.Graph.GraphConfig.Str[(gstr)"work2"]}完成啦, 累计赚了 {GetCount * 1.25:f2} 金钱\n共计花费了{MaxTime}分钟"));
                        break;
                }

                return;
            }
            else
            {
                tleft = TimeSpan.FromMinutes(MaxTime) - ts;
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
                    switch (m.State)
                    {
                        case Main.WorkingState.Study:
                            tNumberUnit.Text = "EXP";
                            break;
                        case Main.WorkingState.WorkONE:
                        case Main.WorkingState.WorkTWO:
                            tNumberUnit.Text = "金钱";
                            break;
                    }
                    break;
            }
        }
        public void ShowTimeSpan(TimeSpan ts)
        {
            if (ts.TotalSeconds < 90)
            {
                tNumber.Text = ts.TotalSeconds.ToString("f1");
                tNumberUnit.Text = "秒";
            }
            else if (ts.TotalMinutes < 90)
            {
                tNumber.Text = ts.TotalMinutes.ToString("f1");
                tNumberUnit.Text = "分钟";
            }
            else
            {
                tNumber.Text = ts.TotalHours.ToString("f1");
                tNumberUnit.Text = "小时";
            }
        }
        public void DisplayUI()
        {
            switch (m.State)
            {
                case Main.WorkingState.Study:
                    btnStop.Content = "停止学习";
                    switch (DisplayType)
                    {
                        default:
                        case 0:
                            tNow.Text = "当前已学习";
                            break;
                        case 1:
                            tNow.Text = "剩余学习时间";
                            break;
                        case 2:
                            tNow.Text = "获得经验值";
                            break;
                    }
                    break;
                case Main.WorkingState.WorkONE:
                    workdisplay(m.Core.Graph.GraphConfig.Str[(gstr)"work1"]);
                    break;
                case Main.WorkingState.WorkTWO:
                    workdisplay(m.Core.Graph.GraphConfig.Str[(gstr)"work2"]);
                    break;
            }
            M_TimeUIHandle(m);
        }
        private void workdisplay(string workname)
        {
            btnStop.Content = "停止" + workname;
            switch (DisplayType)
            {
                default:
                case 0:
                    tNow.Text = "当前已" + workname;
                    break;
                case 1:
                    tNow.Text = $"剩余{workname}时间";
                    break;
                case 2:
                    tNow.Text = "累计金钱收益";
                    break;
            }
        }
        private void SwitchState_Click(object sender, RoutedEventArgs e)
        {
            DisplayType++;
            if (DisplayType >= 3)
                DisplayType = 0;

            DisplayUI();
        }
        public void Start(Main.WorkingState state)
        {
            //if (state == Main.WorkingState.Nomal)
            //    return;
            Visibility = Visibility.Visible;
            m.State = state;
            StartTime = DateTime.Now;
            GetCount = 0;
            switch (state)
            {
                case Main.WorkingState.Study:
                    m.Core.Graph.GraphConfig.UIStyleStudy.SetStyle(this);
                    MaxTime = 45;
                    m.DisplayStudy();
                    break;
                case Main.WorkingState.WorkONE:
                    m.Core.Graph.GraphConfig.UIStyleWork1.SetStyle(this);
                    MaxTime = 60;
                    m.DisplayWorkONE();
                    break;
                case Main.WorkingState.WorkTWO:
                    m.Core.Graph.GraphConfig.UIStyleWork2.SetStyle(this);
                    MaxTime = 180;
                    m.DisplayWorkTWO();
                    break;
                default:
                    return;
            }
            PBLeft.Maximum = MaxTime;
            DisplayUI();
        }
        public void Stop(Action @then = null)
        {
            Visibility = Visibility.Collapsed;
            switch (m.State)
            {
                case Main.WorkingState.Study:
                    m.State = Main.WorkingState.Nomal;
                    m.Display(GraphCore.GraphType.Study_C_End, then ?? m.DisplayNomal);
                    return;
                case Main.WorkingState.WorkONE:
                    m.State = Main.WorkingState.Nomal;
                    m.Display(GraphCore.GraphType.WorkONE_C_End, then ?? m.DisplayNomal);
                    return;
                case Main.WorkingState.WorkTWO:
                    m.State = Main.WorkingState.Nomal;
                    m.Display(GraphCore.GraphType.WorkTWO_C_End, then ?? m.DisplayNomal);
                    break;
                default:
                    then?.Invoke();
                    return;
            }
        }
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        public class UIStyleConfig
        {
            [Line]
            public string BorderBrush = "0290D5";
            [Line]
            public string Background = "81d4fa";
            [Line]
            public string ButtonBackground = "0286C6";
            [Line]
            public string ButtonForeground = "ffffff";
            [Line]
            public string Foreground = "0286C6";
            [Line]
            public double Left = 100;
            [Line]
            public double Top = 160;
            [Line]
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
        }
    }
}
