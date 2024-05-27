using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphHelper.Work;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 日程表功能
/// </summary>
public class ScheduleTask
{
    public ObservableCollection<ScheduleItemBase> ScheduleItems { get; set; } = [];
    private IMainWindow mw;
    public int NowIndex { get; set; } = 0;
    public bool IsOn { get; set; } = false;
    /// <summary>
    /// 根据设置获取日程表
    /// </summary>
    public ScheduleTask(IMainWindow imw)
    {
        this.mw = imw;
        if (mw.GameSavesData.Data.ContainsLine("schedule"))
        {
            int i = 0;
            var schedule = mw.GameSavesData.Data["schedule"];
            while (schedule.Contains(i.ToString()))
            {
                var sub = schedule[(gstr)i.ToString()].Split(',');
                if (sub[0] == "rest")
                {
                    ScheduleItems.Add(new RestScheduleItem(this, int.Parse(sub[1])));
                }
                else
                {
                    Work work = mw.Core.Graph.GraphConfig.Works.Find(w => w.Name == sub[0]);
                    if (work != null)
                    {
                        int dbl = int.Parse(sub[1]);
                        switch (work.Type)
                        {
                            case WorkType.Work:
                                ScheduleItems.Add(new WorkScheduleItem(this, work, dbl));
                                break;
                            case WorkType.Study:
                                ScheduleItems.Add(new StudyScheduleItem(this, work, dbl));
                                break;
                            case WorkType.Play:
                                ScheduleItems.Add(new PlayScheduleItem(this, work, dbl));
                                break;
                        }
                    }
                }
                i++;
            }
            if (mw.GameSavesData.Data.ContainsLine("schedule_work"))
            {
                PackageWork = LPSConvert.DeserializeObject<Package>(mw.GameSavesData.Data["schedule_work"]);
            }
            if (mw.GameSavesData.Data.ContainsLine("schedule_study"))
            {
                PackageStudy = LPSConvert.DeserializeObject<Package>(mw.GameSavesData.Data["schedule_study"]);
            }
            NowIndex = schedule[(gint)"now"];
            IsOn = schedule[(gbol)"ison"];
        }
        imw.Main.WorkTimer.E_FinishWork += WorkTimer_E_FinishWork;
        RestTimer.Elapsed += RestTimer_Elapsed;
        if (IsOn)
            RestTimer.Start();
    }
    public void Save()
    {
        mw.GameSavesData.Data["schedule"].Clear();
        mw.GameSavesData.Data["schedule"][(gint)"now"] = NowIndex;
        mw.GameSavesData.Data["schedule"][(gbol)"ison"] = IsOn;
        for (int i = 0; i < ScheduleItems.Count; i++)
        {
            if (ScheduleItems[i] is RestScheduleItem rsi)
            {
                mw.GameSavesData.Data["schedule"][(gstr)i.ToString()] = $"rest,{rsi.RestTime}";
            }
            else if (ScheduleItems[i] is WorkScheduleItem wsi)
            {
                mw.GameSavesData.Data["schedule"][(gstr)i.ToString()] = $"{wsi.Work.Name},{wsi.DBL}";
            }
        }
        if (PackageWork == null)
        {
            mw.GameSavesData.Data.Remove("schedule_work");
        }
        else
        {
            mw.GameSavesData.Data["schedule_work"] = LPSConvert.SerializeObject(PackageWork, "schedule_work");
        }
        if (PackageStudy == null)
        {
            mw.GameSavesData.Data.Remove("schedule_study");
        }
        else
        {
            mw.GameSavesData.Data["schedule_study"] = LPSConvert.SerializeObject(PackageStudy, "schedule_study");
        }
    }

    private void RestTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (RestTime-- < 0)
            return;
        if (RestTime == 0)
        {
            StartWork();
        }
        else
        {
            RestTimer.Start();
        }
    }

    public void StartWork()
    {
        RestTime = -100;
        if (!IsOn)
            return;
        if (ScheduleItems.Count > 0)
        {
            if (NowIndex >= ScheduleItems.Count)
            {
                NowIndex = 0;
            }
            if (ScheduleItems[NowIndex] is WorkScheduleItem wsi)
            {
                mw.Main.StartWork(wsi.Work);
                NowIndex++;
            }
            else if (ScheduleItems[NowIndex] is RestScheduleItem rsi)
            {
                NowIndex++;
                RestTime = rsi.RestTime * 2;
                RestTimer.Start();
            }
        }
    }
    private int RestTime = 2;
    private Timer RestTimer = new Timer()
    {
        Interval = 30000,
        AutoReset = false
    };

    private void WorkTimer_E_FinishWork(Core.WorkTimer.FinishWorkInfo obj) => StartWork();

    public Package PackageWork { get; set; }
    public Package PackageStudy { get; set; }
    /// <summary>
    /// 日程表日程
    /// </summary>
    public class ScheduleItemBase : NotifyPropertyChangedBase
    {
        public ScheduleItemBase(ScheduleTask task)
        {
            Task = task;
        }
        public ScheduleTask Task;
        /// <summary>
        /// 休息时间
        /// </summary>
        public virtual int RestTime { get; set; } = 0;
        /// <summary>
        /// 工作时间
        /// </summary>
        public virtual int WorkTime { get; set; } = 0;
        /// <summary>
        /// 是否是当前正在进行的日程
        /// </summary>
        public bool IsNow
        {
            get
            {
                if (Task.ScheduleItems.Count < Task.NowIndex)
                {
                    return false;
                }
                return Task.ScheduleItems[Task.NowIndex] == this;
            }
        }
    }
    /// <summary>
    /// 工作日程表日程
    /// </summary>
    public class WorkScheduleItem
        : ScheduleItemBase
    {
        /// <summary>
        /// 翻倍倍率
        /// </summary>
        public int DBL { get; set; }
        /// <summary>
        /// 当前绑定工作
        /// </summary>
        public Work Work { get; set; }
        public WorkScheduleItem(ScheduleTask task, Work work, int dbl) : base(task)
        {
            this.Work = work;
            string source = task.mw.ImageSources.FindSource("work_" + task.mw.Set.PetGraph + "_" + work.Graph) ?? task.mw.ImageSources.FindSource("work_" + task.mw.Set.PetGraph + "_" + work.Name);

            if (source == null)
            {
                //尝试显示默认图像
                Image = task.mw.ImageSources.FindImage("work_" + task.mw.Set.PetGraph + "_t_" + work.Type.ToString(), "work_" + work.Type.ToString());
            }
            else
            {
                Image = ImageResources.NewSafeBitmapImage(source);
            }

        }

        public ImageSource Image { get; set; }

        public string WorkName => Work.NameTrans;

        public override int WorkTime => Work.Time;

        public bool IsPreviousIsRest { get => _isPreviousIsRest; set => Set(ref _isPreviousIsRest, value); }
        private bool _isPreviousIsRest;
    }
    /// <summary>
    /// 学习日程表日程
    /// </summary>
    public class StudyScheduleItem : WorkScheduleItem
    {
        public StudyScheduleItem(ScheduleTask task, Work work, int dbl) : base(task, work, dbl)
        {
        }
    }
    /// <summary>
    /// 工作日程表日程
    /// </summary>
    public class PlayScheduleItem : WorkScheduleItem
    {
        public PlayScheduleItem(ScheduleTask task, Work work, int dbl) : base(task, work, dbl)
        {
        }
        public override int WorkTime => Work.Time / 2;
        public override int RestTime => Work.Time / 2;
    }
    /// <summary>
    /// 休息日程表日程
    /// </summary>
    public class RestScheduleItem
        : ScheduleItemBase
    {
        public RestScheduleItem(ScheduleTask task, int restTime) : base(task)
        {
            RestTime = restTime;
        }
        /// <summary>
        /// 休息时间
        /// </summary>
        public override int RestTime { get => _restTime; set => Set(ref _restTime, value); }
        private int _restTime;
    }
    /// <summary>
    /// 套餐信息
    /// </summary>
    public class Package
    {
        /// <summary>
        /// 协议名称 (已翻译)
        /// </summary>
        public string NameTrans { get; set; }
        /// <summary>
        /// 抽成
        /// </summary>
        [Line] public double Commissions { get; set; }
        /// <summary>
        /// 办理费用
        /// </summary>
        [Line] public double Price { get; set; }
        /// <summary>
        /// 截止时间
        /// </summary>
        [Line] public DateTime EndTime { get; set; }
        /// <summary>
        /// 可用等级
        /// </summary>
        [Line] public int Level { get; set; }
    }
    /// <summary>
    /// 套餐详细
    /// </summary>
    public class PackageFull : Package
    {
        /// <summary>
        /// 套餐名称
        /// </summary>
        [Line] public string Name { get; set; }
        /// <summary>
        /// 协议名称 (已翻译)
        /// </summary>
        public new string NameTrans
        {
            get
            {
                if (string.IsNullOrEmpty(base.NameTrans))
                {
                    base.NameTrans = Name.Translate();
                }
                return base.NameTrans;
            }
            set => base.NameTrans = value;
        }
        /// <summary>
        /// 持续时间
        /// </summary>
        [Line] public int Duration { get; set; }
        /// <summary>
        /// 等级需求
        /// </summary>
        [Line] public double LevelInNeed { get; set; }

        /// <summary>
        /// 工作类型
        /// </summary>
        [Line] public Work.WorkType WorkType { get; set; }
    }
    /// <summary>
    /// 所有可用套餐
    /// </summary>
    public List<PackageFull> PackageFulls { get; set; }
    /// <summary>
    /// 获取基本可用套餐
    /// </summary>
    public static List<PackageFull> GetBasePackageFulls()
    {
        return new List<PackageFull>
        {
            new PackageFull
            {
                Name = "work",
                Duration = 1,
                LevelInNeed = 0,
                WorkType = Work.WorkType.Work
            },
            new PackageFull
            {
                Name = "study",
                Duration = 1,
                LevelInNeed = 0,
                WorkType = Work.WorkType.Study
            },
            new PackageFull
            {
                Name = "play",
                Duration = 1,
                LevelInNeed = 0,
                WorkType = Work.WorkType.Play
            }
        };
    }
}

