using LinePutScript.Localization.WPF;
using Microsoft.VisualBasic.Devices;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphHelper;

namespace VPet_Simulator.Windows;
/// <summary>
/// winWorkMenu.xaml 的交互逻辑
/// </summary>
public partial class winWorkMenu : WindowX
{
    MainWindow mw;
    List<Work> ws;
    List<Work> ss;
    List<Work> ps;

    private readonly List<string> _workDetails = new List<string>();
    private readonly List<string> _studyDetails = new List<string>();
    private readonly List<string> _playDetails = new List<string>();

    public void ShowImageDefault(Work.WorkType type)
    {
        Dispatcher.BeginInvoke(() =>
        {
            WorkViewImage.Source = mw.ImageSources.FindImage(mw.Set.PetGraph + "_" + type.ToString(), "work");
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }
    public winWorkMenu(MainWindow mw, Work.WorkType type)
    {
        InitializeComponent();
        this.mw = mw;

        mw.Main.WorkList(out ws, out ss, out ps);
        if (ws.Count == 0)
            LsbCategory.Items.Remove(LsbiWork);
        else
            foreach (var v in ws)
            {
                _workDetails.Add(v.NameTrans);
            }
        if (ss.Count == 0)
            LsbCategory.Items.Remove(LsbiStudy);
        else
            foreach (var v in ss)
            {
                _studyDetails.Add(v.NameTrans);
            }
        if (ps.Count == 0)
            LsbCategory.Items.Remove(LsbiPlay);
        else
            foreach (var v in ps)
            {
                _playDetails.Add(v.NameTrans);
            }

        LsbCategory.SelectedIndex = (int)type;
        ShowImageDefault(type);
    }
    private bool AllowChange = false;
    Work nowwork;
    Work nowworkdisplay;
    public void ShowWork()
    {
        AllowChange = false;
        btnStart.IsEnabled = true;
        //判断倍率
        if (nowwork.LevelLimit > mw.GameSavesData.GameSave.Level)
        {
            wDouble.IsEnabled = false;
            wDouble.Value = 1;
        }
        else
        {
            int max = Math.Min(4000, mw.GameSavesData.GameSave.Level) / (nowwork.LevelLimit + 10);
            if (max <= 1)
            {
                wDouble.IsEnabled = false;
                wDouble.Value = 1;
            }
            else
            {
                wDouble.IsEnabled = true;
                wDouble.Maximum = max;
                wDouble.Value = mw.Set.GameData.GetInt("workmenu_" + nowwork.Name, 1);
            }
        }
        if (wDouble.Value == 1)
            ShowWork(nowwork);
        else
            ShowWork(nowwork.Double((int)wDouble.Value));
        AllowChange = true;
    }
    public void ShowWork(Work work)
    {
        nowworkdisplay = work;
        //显示图像
        string source = mw.ImageSources.FindSource("work_" + work.Graph) ?? mw.ImageSources.FindSource("work_" + work.Name);
        if (source == null)
        {
            //尝试显示默认图像
            ShowImageDefault(work.Type);
        }
        else
        {
            WorkViewImage.Source = Interface.ImageResources.NewSafeBitmapImage(source);
        }
        if (work.Type == Work.WorkType.Work)
            tbGain.Text = $"${"金钱".Translate()}";
        else
            tbGain.Text = $"Exp{"经验".Translate()}";
        tbSpeed.Text = "+" + work.Get().ToString("f2");
        tbFood.Text = "-" + work.StrengthFood.ToString("f2");
        tbDrink.Text = "-" + work.StrengthDrink.ToString("f2");
        tbSpirit.Text = "-" + work.Feeling.ToString("f2");
        tbLvLimit.Text = work.LevelLimit.ToString("f0");
        if (work.Time > 100)
            tbTime.Text = (work.Time / 60).ToString("f2") + 'h';
        else
            tbTime.Text =work.Time.ToString() + 'm';
        tbBonus.Text ='x' + (1 + work.FinishBonus).ToString("f2");
        tbRatio.Text ='x' + wDouble.Value.ToString("f0");

    }

    private void LsbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            ShowImageDefault((Work.WorkType)LsbCategory.SelectedIndex);
            switch (LsbCategory.SelectedIndex)
            {
                case 0:
                    detailTypes.ItemsSource = _workDetails;
                    detailTypes.SelectedIndex = 0;
                    btnStart.Content = "开始工作".Translate();
                    break;
                case 1:
                    detailTypes.ItemsSource = _studyDetails;
                    detailTypes.SelectedIndex = 0;
                    btnStart.Content = "开始学习".Translate();
                    break;
                case 2:
                    detailTypes.ItemsSource = _playDetails;
                    detailTypes.SelectedIndex = 0;
                    btnStart.Content = "开始玩耍".Translate();
                    break;
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void wDouble_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!AllowChange) return;
        mw.Set.GameData.SetInt("workmenu_" + nowwork.Name, (int)wDouble.Value);
        ShowWork(nowwork.Double((int)wDouble.Value));
    }



    private void detailTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            switch (LsbCategory.SelectedIndex)
            {
                case 0:
                    nowwork = (ws[detailTypes.SelectedIndex]);
                    //btnStart.Content = "开始工作".Translate();
                    break;
                case 1:
                    nowwork = (ss[detailTypes.SelectedIndex]);
                    //btnStart.Content = "开始学习".Translate();
                    break;
                case 2:
                    nowwork = (ps[detailTypes.SelectedIndex]);
                    //btnStart.Content = "开始玩耍".Translate();
                    break;
            }
            ShowWork();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        mw.winWorkMenu = null;
    }

    private void btnStart_Click(object sender, RoutedEventArgs e)
    {
        if (nowworkdisplay != null)
        {
            if (mw.Main.StartWork(nowworkdisplay))
                Close();
        }
    }
}
