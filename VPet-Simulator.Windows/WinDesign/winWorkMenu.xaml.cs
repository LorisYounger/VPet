using LinePutScript.Localization.WPF;
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
public partial class winWorkMenu : Window
{
    MainWindow mw;
    List<Work> ws;
    List<Work> ss;
    List<Work> ps;
    public void ShowImageDefault(Work.WorkType type) => WorkViewImage.Source = mw.ImageSources.FindImage("work_" + mw.Set.PetGraph + "_t_" + type.ToString(), "work_" + type.ToString());
    public winWorkMenu(MainWindow mw, Work.WorkType type)
    {
        InitializeComponent();
        this.mw = mw;
        mw.Main.WorkList(out ws, out ss, out ps);
        if (ws.Count == 0)
            tbc.Items.Remove(tiw);
        else
            foreach (var v in ws)
            {
                lbWork.Items.Add(v.NameTrans);
            }
        if (ss.Count == 0)
            tbc.Items.Remove(tis);
        else
            foreach (var v in ss)
            {
                lbStudy.Items.Add(v.NameTrans);
            }
        if (ps.Count == 0)
            tbc.Items.Remove(tip);
        else
            foreach (var v in ps)
            {
                lbPlay.Items.Add(v.NameTrans);
            }
        foreach (var v in mw.WorkStar())
        {
            lbStar.Items.Add(v.NameTrans);
        }
        tbc.SelectedIndex = (int)type;
        ShowImageDefault(type);
    }
    public bool IsWorkStar(Work work) => mw.Set["work_star"].GetBool(work.Name);
    public void SetWorkStar(Work work, bool setvalue) => mw.Set["work_star"].SetBool(work.Name, setvalue);
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
                wDouble.Value = mw.Set["workmenu"].GetInt("double_" + nowwork.Name, 1);
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
        lName.Content = work.NameTrans;
        //显示图像
        string source = mw.ImageSources.FindSource("work_" + mw.Set.PetGraph + "_" + work.Graph) ?? mw.ImageSources.FindSource("work_" + mw.Set.PetGraph + "_" + work.Name);
        if (source == null)
        {
            //尝试显示默认图像
            ShowImageDefault(work.Type);
        }
        else
        {
            WorkViewImage.Source = Interface.ImageResources.NewSafeBitmapImage(source);
        }
        StringBuilder sb = new StringBuilder();
        if (work.Type == Work.WorkType.Work)
            sb.AppendLine("金钱".Translate());
        else
            sb.AppendLine("经验".Translate());
        sb.AppendLine(work.Get().ToString("f2"));
        sb.AppendLine(work.StrengthFood.ToString("f2"));
        sb.AppendLine(work.StrengthDrink.ToString("f2"));
        sb.AppendLine(work.Feeling.ToString("f2"));
        sb.AppendLine(work.LevelLimit.ToString("f0"));
        if (work.Time > 100)
            sb.AppendLine((work.Time / 60).ToString("f2") + 'h');
        else
            sb.AppendLine(work.Time.ToString() + 'm');
        sb.AppendLine('x' + (1 + work.FinishBonus).ToString("f2"));
        sb.AppendLine('x' + wDouble.Value.ToString("f0"));
        tbDisplay.Text = sb.ToString();
        tbtn_star.IsChecked = IsWorkStar(work);
    }

    private void tbc_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        switch (tbc.SelectedIndex)
        {
            case 0:
                btnStart.Content = "开始工作".Translate();
                break;
            case 1:
                btnStart.Content = "开始学习".Translate();
                break;
            case 2:
                btnStart.Content = "开始玩耍".Translate();
                break;
            case 3:
                btnStart.Content = "开始工作".Translate();
                return;
        }
        ShowImageDefault((Work.WorkType)tbc.SelectedIndex);
    }

    private void wDouble_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!AllowChange) return;
        mw.Set["workmenu"].SetInt("double_" + nowwork.Name, (int)wDouble.Value);
        ShowWork(nowwork.Double((int)wDouble.Value));
    }

    private void lbWork_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        nowwork = (ws[lbWork.SelectedIndex]);
        ShowWork();
        e.Handled = true;
    }

    private void lbStudy_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        nowwork = (ss[lbStudy.SelectedIndex]);
        ShowWork();
        e.Handled = true;
    }

    private void lbPlay_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        nowwork = (ps[lbPlay.SelectedIndex]);
        ShowWork();
        e.Handled = true;
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

    private void lbStar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!AllowChange) return;
        var works = mw.WorkStar();
        if (works.Count <= lbStar.SelectedIndex) return;
        nowwork = (works[lbStar.SelectedIndex]);
        ShowWork();
        e.Handled = true;
    }

    private void tbtn_star_Click(object sender, RoutedEventArgs e)
    {
        SetWorkStar(nowwork, tbtn_star.IsChecked == true);
        AllowChange = false;
        lbStar.Items.Clear();
        //更新星标
        foreach (var v in mw.WorkStar())
        {
            lbStar.Items.Add(v.NameTrans);
            var mi = new System.Windows.Controls.MenuItem()
            {
                Header = nowwork.NameTrans
            };
            mi.Click += (s, e) => mw.Main.ToolBar.StartWork(nowwork.Double(mw.Set["workmenu"].GetInt("double_" + nowwork.Name, 1)));
            mw.WorkStarMenu.Items.Add(mi);
        }
        AllowChange = true;
    }
}
