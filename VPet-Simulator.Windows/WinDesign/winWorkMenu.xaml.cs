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
        imgAgency.Source = mw.ImageSources.FindImage("work_" + mw.Set.PetGraph + "_agency_job", "work_agency_job");
        tbtnAgencyTraning.IsChecked = false;
        rTaskType.Text = "抽成".Translate();
        foreach (var v in mw.SchedulePackage.FindAll(x => x.WorkType == Work.WorkType.Work))
            combTaskType.Items.Add(v);

        if (mw.Core.Save.Level > 15)
            blockTask.Visibility = Visibility.Collapsed;
        rpnDisplay(mw.ScheduleTask.PackageWork, Work.WorkType.Work);
        sliderTaskLevel.Maximum = mw.Core.Save.Level / 5 * 5;
        if (mw.Core.Save.Level > 200)
            sliderTaskLevel.TickFrequency = mw.Core.Save.Level / 100 * 5;
        else
            sliderTaskLevel.TickFrequency = 5;
        tbtnCurrentPlan.IsChecked = mw.ScheduleTask.PackageWork?.IsActive() == true;
        btnStartSchedule.IsChecked = false;

        AllowChange = true;
        combTaskType.SelectedIndex = 0;


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
                if (max > 25)
                    wDouble.TickFrequency = max / 25;
                else
                    wDouble.TickFrequency = 1;
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
                    btnAddAuto.IsEnabled = mw.ScheduleTask.PackageWork?.IsActive() == true;
                    break;
                case 1:
                    detailTypes.ItemsSource = _studyDetails;
                    btnStart.Content = "开始学习".Translate();
                    ComboBoxHelper.SetWatermark(detailTypes, "---" + "请选择".Translate() + "学习".Translate() + "---");
                    btnAddAuto.IsEnabled = mw.ScheduleTask.PackageStudy?.IsActive() == true;
                    break;
                case 2:
                    detailTypes.ItemsSource = _playDetails;
                    btnStart.Content = "开始玩耍".Translate();
                    ComboBoxHelper.SetWatermark(detailTypes, "---" + "请选择".Translate() + "玩耍".Translate() + "---");
                    btnAddAuto.IsEnabled = mw.Core.Save.Level >= 15;
                    break;
                case 3:
                    detailTypes.ItemsSource = _starDetails;
                    btnStart.Content = "开始工作".Translate();
                    ComboBoxHelper.SetWatermark(detailTypes, "---" + "请选择".Translate() + "---");
                    btnAddAuto.IsEnabled = mw.Core.Save.Level >= 15;
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
        if (btnStartSchedule.IsChecked == mw.ScheduleTask.IsOn)
            return;
        if (mw.ScheduleTask.IsOn)
        {
            mw.ScheduleTask.Stop();
        }
        else
        {
            if (sworkTime / (double)(sworkTime + srestTime) > 0.71)
            {
                MessageBoxX.Show("工作时间过长,请添加更多的休息时间".Translate(), "工作时间过长".Translate());
                btnStartSchedule.IsChecked = false;
                return;
            }
            mw.ScheduleTask.Start();
        }
    }

    private void NumberInput_ValueChanged(object sender, SelectedValueChangedRoutedEventArgs<double?> e)
    {
        CalculateSceduleTime();
    }
    int sworkTime = 0;
    int srestTime = 0;
    private void CalculateSceduleTime()
    {
        sworkTime = 0;
        srestTime = 0;
        for (int i = 0; i < _schedules.Count; i++)
        {
            var item = _schedules[i];
            var lastItem = i == 0 ? null : _schedules[i - 1];
            if (item is WorkScheduleItem workItem)
            {
                workItem.IsPreviousIsRest = lastItem is RestScheduleItem;
            }
            sworkTime += item.WorkTime;
            srestTime += item.RestTime;
        }
        rpgbSchedule.Maximum = sworkTime + srestTime;
        rpgbSchedule.Value = sworkTime;

        runScheduleWork.Text = sworkTime.ToString();
        runScheduleRest.Text = srestTime.ToString();

        double ps = sworkTime / (double)(sworkTime + srestTime);
        runSchedulePercentage.Text = ps.ToString("p0");
        if (ps > 0.71)
            runSchedulePercentage.Foreground = new SolidColorBrush(Colors.OrangeRed);
        else
            runSchedulePercentage.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimary);
        rpgbSchedule.Foreground = runSchedulePercentage.Foreground;
    }
    private void rpnDisplay(Package package, Work.WorkType type)
    {
        if (package == null)
        {
            rpnDescribe.Text = rpnName.Text = "暂无签署套餐".Translate();
            rpnEndDate.Text = rpnPrice.Text = rpnCommissions.Text = "-";
            rpnEndDay.Text = "0";
            return;
        }
        rpnName.Text = package.NameTrans;
        if (Work.WorkType.Work == type)
        {
            rpnCommissions.Text = package.Commissions.ToString("p0");
        }
        else
        {
            rpnCommissions.Text = (1 - package.Commissions).ToString("p0");
        }
        rpnDescribe.Text = package.Describe;
        rpnPrice.Text = package.Price.ToString("N0");
        rpnEndDate.Text = package.EndTime.ToString("MM/dd");
        rpnLevelInNeed.Text = package.Level.ToString();
        var totalhour = (package.EndTime - DateTime.Now).TotalHours;
        if (totalhour <= 0)
            rpnEndDay.Text = "0";
        else if (totalhour <= 24)
            rpnEndDay.Text = totalhour.ToString("f1");
        else
            rpnEndDay.Text = totalhour.ToString("f0");

    }


    private void tbtn_Agency_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (imgAgency == null)
        {
            return;
        }
        sliderTaskLevel.Maximum = mw.Core.Save.Level / 5 * 5;
        if (mw.Core.Save.Level > 200)
            sliderTaskLevel.TickFrequency = mw.Core.Save.Level / 100 * 5;
        else
            sliderTaskLevel.TickFrequency = 5;
        if (sender == tbtnAgencyJob)
        {
            imgAgency.Source = mw.ImageSources.FindImage("work_" + mw.Set.PetGraph + "_agency_job", "work_agency_job");
            tbtnAgencyTraning.IsChecked = false;
            //加载套餐combTaskType
            rTaskType.Text = "抽成".Translate();
            combTaskType.Items.Clear();
            foreach (var v in mw.SchedulePackage.FindAll(x => x.WorkType == Work.WorkType.Work))
                combTaskType.Items.Add(v);
            combTaskType.SelectedIndex = 0;
            //加载现有套餐
            rpnDisplay(mw.ScheduleTask.PackageWork, nowselefull.WorkType);
            tbtnCurrentPlan.IsChecked = mw.ScheduleTask.PackageWork?.IsActive() == true;
        }
        else if (sender == tbtnAgencyTraning)
        {
            imgAgency.Source = mw.ImageSources.FindImage("work_" + mw.Set.PetGraph + "_agency_training", "work_agency_training");
            tbtnAgencyJob.IsChecked = false;

            rTaskType.Text = "效率".Translate();
            combTaskType.Items.Clear();
            foreach (var v in mw.SchedulePackage.FindAll(x => x.WorkType == Work.WorkType.Study))
                combTaskType.Items.Add(v);
            combTaskType.SelectedIndex = 0;
            //加载现有套餐
            rpnDisplay(mw.ScheduleTask.PackageStudy, nowselefull.WorkType);
            tbtnCurrentPlan.IsChecked = mw.ScheduleTask.PackageStudy?.IsActive() == true;
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
        if (nowselefull == null) return;
        Package package = new Package(nowselefull, (int)sliderTaskLevel.Value);
        if (package.Price > mw.Core.Save.Money)
        {
            MessageBoxX.Show("金钱不足".Translate(), "签署失败".Translate());
            return;
        }
        if (nowselefull.WorkType == Work.WorkType.Work)
        {
            if (mw.ScheduleTask.PackageWork?.IsActive() == true
                && MessageBoxX.Show("工作套餐已激活,是否替换?".Translate(), "套餐已激活".Translate(), MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            mw.ScheduleTask.PackageWork = package;
            rpnDisplay(mw.ScheduleTask.PackageWork, nowselefull.WorkType);
        }
        else
        {
            if (mw.ScheduleTask.PackageStudy?.IsActive() == true
                && MessageBoxX.Show("学习套餐已激活,是否替换?".Translate(), "套餐已激活".Translate(), MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            mw.ScheduleTask.PackageStudy = package;
            rpnDisplay(mw.ScheduleTask.PackageStudy, nowselefull.WorkType);
        }
        mw.Core.Save.Money -= package.Price;
        MessageBoxX.Show("套餐 {0} 签署成功".Translate(package.NameTrans), "签署成功".Translate());
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
                mw.ScheduleTask.AddRest(30);
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

    private void btnAddAuto_Click(object sender, RoutedEventArgs e)
    {
        //看看套餐
        switch (nowwork.Type)
        {
            case Work.WorkType.Work:
                if (mw.ScheduleTask.PackageWork?.IsActive() != true)
                {
                    MessageBoxX.Show("工作套餐未激活,请前往日程表签署工作中介套餐".Translate(), "套餐未激活".Translate());
                    return;
                }
                else if (mw.ScheduleTask.PackageWork.Level < nowworkdisplay.LevelLimit)
                {
                    MessageBoxX.Show("工作套餐等级不足({0}/{1}),\n请选择更低等级要求/倍率的工作或前往日程表签署新的工作中介套餐".Translate(mw.ScheduleTask.PackageWork.Level,
                        nowworkdisplay.LevelLimit), "套餐等级不足".Translate());
                    return;
                }
                mw.ScheduleTask.AddWork(nowwork, 30);
                break;
            case Work.WorkType.Study:
                if (mw.ScheduleTask.PackageStudy?.IsActive() != true)
                {
                    MessageBoxX.Show("学习套餐未激活,请前往日程表签署培训机构套餐".Translate(), "套餐未激活".Translate());
                    return;
                }
                else if (mw.ScheduleTask.PackageStudy.Level < nowworkdisplay.LevelLimit)
                {
                    MessageBoxX.Show("学习套餐等级不足({0}/{1}),\n请选择更低等级要求/倍率的学习或前往日程表签署新的培训机构套餐".Translate(mw.ScheduleTask.PackageStudy.Level,
                        nowworkdisplay.LevelLimit), "套餐等级不足".Translate());
                    return;
                }
                mw.ScheduleTask.AddStudy(nowwork, 30);
                break;
            case Work.WorkType.Play:
                if (mw.Core.Save.Level < 15)
                {
                    MessageBoxX.Show("等级不足15级,无法使用日程表".Translate(), "等级不足".Translate());
                    return;
                }
                mw.ScheduleTask.AddPlay(nowwork, 30);
                break;
        }
    }
    PackageFull nowselefull;
    private void combTaskType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!AllowChange) return;
        if (combTaskType.SelectedItem is not PackageFull sf)
        {
            return;
        }
        nowselefull = sf;
        nowselefullDisplay();
    }
    private void nowselefullDisplay()
    {
        if (nowselefull.WorkType == Work.WorkType.Work)
        {
            rCommissions.Text = nowselefull.Commissions.ToString("p0");
        }
        else
        {
            rCommissions.Text = (1 - nowselefull.Commissions).ToString("p0");
        }
        int level = (int)sliderTaskLevel.Value;
        rLevelNeed.Text = ((int)(sliderTaskLevel.Value / nowselefull.LevelInNeed)).ToString();
        rDuration.Text = nowselefull.Duration.ToString();
        rpPrice.Text = ((200 * level - 100) * nowselefull.Price).ToString("N0");
        rDescribe.Text = nowselefull.Describe;
    }

    private void sliderTaskLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!AllowChange) return;
        nowselefullDisplay();
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