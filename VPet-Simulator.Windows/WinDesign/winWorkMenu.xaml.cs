using LinePutScript.Localization.WPF;
using Panuon.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Windows.Interface.ScheduleTask;

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

    private readonly ObservableCollection<string> _workDetails = new ObservableCollection<string>();
    private readonly ObservableCollection<string> _studyDetails = new ObservableCollection<string>();
    private readonly ObservableCollection<string> _playDetails = new ObservableCollection<string>();
    private readonly ObservableCollection<string> _starDetails = new ObservableCollection<string>();
    private readonly ObservableCollection<ScheduleItemBase> _schedules;
    public void ShowImageDefault(Work.WorkType type)
    {
        Dispatcher.BeginInvoke(() =>
        {
            WorkViewImage.Source = mw.ImageSources.FindImage("work_" + mw.Set.PetGraph + "_t_" + type.ToString(), "work_" + type.ToString());
        }, DispatcherPriority.Loaded);

        _schedules.CollectionChanged += Schedules_CollectionChanged;
        icSchedule.ItemsSource = _schedules;
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
        foreach (var v in mw.WorkStar())
        {
            _starDetails.Add(v.NameTrans);
        }
        LsbCategory.SelectedIndex = (int)type;
        _schedules = mw.ScheduleTask.ScheduleItems;
        ShowImageDefault(type);
        CalculateSceduleTime();
        if (mw.Core.Save.Level > 15)
            blockTask.Visibility = Visibility.Collapsed;
        AllowChange = true;


        ComboBoxHelper.SetWatermark(detailTypes, "---" + "请选择".Translate() + "---");

        ComboBoxHelper.SetWatermark(combTaskType, "---" + "请选择".Translate() + "套餐".Translate() + "---");

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
        if (!mw.Set["gameconfig"].GetBool("noAutoCal") && work.IsOverLoad())
        {
            work.FixOverLoad();
        }
        nowworkdisplay = work;

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
        if (work.Type == Work.WorkType.Work)
            tbGain.Text = "金钱".Translate();
        else
            tbGain.Text = "经验".Translate();
        tbSpeed.Text = work.Get().ToString("f2");
        tbFood.Text = work.StrengthFood.ToString("f2");
        tbDrink.Text = work.StrengthDrink.ToString("f2");
        tbSpirit.Text = work.Feeling.ToString("f2");
        tbLvLimit.Text = work.LevelLimit.ToString("f0");
        if (work.Time > 100)
            tbTime.Text = (work.Time / 60).ToString("f2") + 'h';
        else
            tbTime.Text = work.Time.ToString() + 'm';
        tbBonus.Text = 'x' + (1 + work.FinishBonus).ToString("f2");
        tbRatio.Text = 'x' + wDouble.Value.ToString("f0");
        tbtn_star.IsChecked = IsWorkStar(work);
    }

    public void LoadSchedule()
    {

    }

    private void LsbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var lastIndex = detailTypes.SelectedIndex;
            if (LsbCategory.SelectedIndex < 3)
                ShowImageDefault((Work.WorkType)LsbCategory.SelectedIndex);
            gdWork.Visibility = Visibility.Visible;
            gdSchedule.Visibility = Visibility.Collapsed;
            switch (LsbCategory.SelectedIndex)
            {
                case 0:
                    detailTypes.ItemsSource = _workDetails;
                    btnStart.Content = "开始工作".Translate();
                    ComboBoxHelper.SetWatermark(detailTypes, "---" + "请选择".Translate() + "工作".Translate() + "---");
                    break;
                case 1:
                    detailTypes.ItemsSource = _studyDetails;
                    btnStart.Content = "开始学习".Translate();
                    ComboBoxHelper.SetWatermark(detailTypes, "---" + "请选择".Translate() + "学习".Translate() + "---");
                    break;
                case 2:
                    detailTypes.ItemsSource = _playDetails;
                    btnStart.Content = "开始玩耍".Translate();
                    ComboBoxHelper.SetWatermark(detailTypes, "---" + "请选择".Translate() + "玩耍".Translate() + "---");
                    break;
                case 3:
                    detailTypes.ItemsSource = _starDetails;
                    btnStart.Content = "开始工作".Translate();
                    ComboBoxHelper.SetWatermark(detailTypes, "---" + "请选择".Translate() + "---");
                    break;
                case 4:
                    gdWork.Visibility = Visibility.Collapsed;
                    gdSchedule.Visibility = Visibility.Visible;
                    return;
            }
            //detailTypes.IsDropDownOpen = true;
            detailTypes_SelectionChanged(null, null);
        }, DispatcherPriority.Loaded);
    }

    private void wDouble_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!AllowChange) return;
        mw.Set["workmenu"].SetInt("double_" + nowwork.Name, (int)wDouble.Value);
        ShowWork(nowwork.Double((int)wDouble.Value));
    }

    private void detailTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (detailTypes.SelectedIndex < 0)
            {
                tbGain.Text = "??";
                tbSpeed.Text = "??";
                tbFood.Text = "??";
                tbDrink.Text = "??";
                tbSpirit.Text = "??";
                tbLvLimit.Text = "??";
                tbTime.Text = "??";
                tbBonus.Text = "??";
                tbRatio.Text = "??";
                return;
            }
            switch (LsbCategory.SelectedIndex)
            {
                case 0:
                    nowwork = (ws[detailTypes.SelectedIndex]);
                    break;
                case 1:
                    nowwork = (ss[detailTypes.SelectedIndex]);
                    break;
                case 2:
                    nowwork = (ps[detailTypes.SelectedIndex]);
                    break;
                case 3:
                    if (!AllowChange) return;
                    var works = mw.WorkStar();
                    if (works.Count <= detailTypes.SelectedIndex) return;
                    nowwork = (works[detailTypes.SelectedIndex]);
                    break;
                case 4:
                    return;
            }
            ShowWork();
        }, DispatcherPriority.Loaded);
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        mw.winWorkMenu = null;
    }

    private void Schedules_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        CalculateSceduleTime();
    }

    private void btnStart_Click(object sender, RoutedEventArgs e)
    {
        if (nowworkdisplay != null)
        {
            if (mw.Main.StartWork(nowworkdisplay))
                Close();
        }
    }

    private void tbtn_star_Click(object sender, RoutedEventArgs e)
    {
        if (nowwork == null)
            return;
        SetWorkStar(nowwork, tbtn_star.IsChecked == true);
        AllowChange = false;
        _starDetails.Clear();
        mw.WorkStarMenu.Items.Clear();
        //更新星标
        foreach (var v in mw.WorkStar())
        {
            _starDetails.Add(v.NameTrans);
            var mi = new MenuItem()
            {
                Header = v.NameTrans
            };
            mi.Click += (s, e) => mw.Main.ToolBar.StartWork(v.Double(mw.Set["workmenu"].GetInt("double_" + v.Name, 1)));
            mw.WorkStarMenu.Items.Add(mi);
        }
        if (detailTypes.ItemsSource == _starDetails)
        {
            detailTypes_SelectionChanged(null, null);
        }
        AllowChange = true;
    }

    private void btnStartSchedule_Click(object sender, RoutedEventArgs e)
    {

    }

    private void NumberInput_ValueChanged(object sender, SelectedValueChangedRoutedEventArgs<double?> e)
    {
        CalculateSceduleTime();
    }

    private void CalculateSceduleTime()
    {
        int workTime = 0;
        int restTime = 0;
        for (int i = 0; i < _schedules.Count; i++)
        {
            var item = _schedules[i];
            var lastItem = i == 0 ? null : _schedules[i - 1];
            if (item is WorkScheduleItem workItem)
            {
                workItem.IsPreviousIsRest = lastItem is RestScheduleItem;
            }
            workTime += item.WorkTime;
            restTime += item.RestTime;
        }
        rpgbSchedule.Maximum = workTime + restTime;
        rpgbSchedule.Value = workTime;

        runScheduleWork.Text = workTime.ToString();
        runScheduleRest.Text = restTime.ToString();

        double ps = workTime / (double)(workTime + restTime);
        runSchedulePercentage.Text = ps.ToString("p0");
        if (ps > 0.71)
            runSchedulePercentage.Foreground = new SolidColorBrush(Colors.OrangeRed);
        else
            runSchedulePercentage.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimary);
        rpgbSchedule.Foreground = runSchedulePercentage.Foreground;
    }

    private void tbtn_Agency_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (imgAgency == null)
        {
            return;
        }

        if (sender == tbtnAgencyJob)
        {
            imgAgency.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_agency_job.png"));
            tbtnAgencyTraning.IsChecked = false;
        }
        else if (sender == tbtnAgencyTraning)
        {
            imgAgency.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_agency_training.png"));
            tbtnAgencyJob.IsChecked = false;
        }
    }

    private void tbtnAgency_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var toggleButton = sender as ToggleButton;
        if (toggleButton.IsChecked == true)
        {
            e.Handled = true;
        }
    }

    private void btnSignAgency_Click(object sender, RoutedEventArgs e)
    {

    }

    private void btn_addRest_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var scheduleItem = button.DataContext as ScheduleItemBase;
        if (scheduleItem == null)
        {
            if (_schedules.LastOrDefault() is RestScheduleItem lastRest)
            {
                lastRest.RestTime += 30;
            }
            else
            {
                _schedules.Add(new RestScheduleItem(mw.ScheduleTask, 30));
            }
        }
        else
        {
            var index = _schedules.IndexOf(scheduleItem);
            _schedules.Insert(index, new RestScheduleItem(mw.ScheduleTask, 30));
        }
    }

    private void btn_removeSchedule_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var scheduleItem = button.DataContext as ScheduleItemBase;
        var index = _schedules.IndexOf(scheduleItem);
        var previousItem = index == 0 ? null : _schedules[index - 1];
        var nextItem = index == _schedules.Count - 1 ? null : _schedules[index + 1];

        if (previousItem is RestScheduleItem previousRest
            && nextItem is RestScheduleItem nextRest)
        {
            previousRest.RestTime += nextRest.RestTime;
            _schedules.Remove(nextRest);
        }
        _schedules.Remove(scheduleItem);
    }

    private void btn_scheduleUp_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var scheduleItem = button.DataContext as ScheduleItemBase;
        var index = _schedules.IndexOf(scheduleItem);
        if (index == 0)
        {
            return;
        }
        var previousItem = index == 0 ? null : _schedules[index - 1];
        var nextItem = index == _schedules.Count - 1 ? null : _schedules[index + 1];

        if (previousItem is RestScheduleItem previousRest
            && nextItem is RestScheduleItem nextRest)
        {
            previousRest.RestTime += nextRest.RestTime;
            _schedules.Remove(nextRest);
        }
        _schedules.Remove(scheduleItem);
        _schedules.Insert(Math.Max(index - 1, 0), scheduleItem);
    }

    private void btn_scheduleDown_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var scheduleItem = button.DataContext as ScheduleItemBase;
        var index = _schedules.IndexOf(scheduleItem);
        if (index == _schedules.Count - 1)
        {
            return;
        }
        var previousItem = index == 0 ? null : _schedules[index - 1];
        var nextItem = index == _schedules.Count - 1 ? null : _schedules[index + 1];

        if (previousItem is RestScheduleItem previousRest
            && nextItem is RestScheduleItem nextRest)
        {
            previousRest.RestTime += nextRest.RestTime;
            _schedules.Remove(nextRest);
        }
        _schedules.Remove(scheduleItem);
        _schedules.Insert(Math.Min(index + 1, _schedules.Count), scheduleItem);
    }
}


internal class ScheduleItemTemplateSelector
    : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var element = container as FrameworkElement;

        return element.FindResource(item.GetType().Name) as DataTemplate;
        //if (item is WorkScheduleItem)
        //{
        //    return element.FindResource("WorkScheduleTemplate") as DataTemplate;
        //}
        //else if (item is StudyScheduleItem)
        //{
        //    return element.FindResource("StudyScheduleItem") as DataTemplate;
        //}
        //else if (item is PlayScheduleItem)
        //{
        //    return element.FindResource("PlayScheduleItem") as DataTemplate;
        //}
        //else if (item is RestScheduleItem)
        //{
        //    return element.FindResource("RestScheduleTemplate") as DataTemplate;
        //}
    }
}