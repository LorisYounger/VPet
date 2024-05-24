using Panuon.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static VPet_Simulator.Core.GraphHelper;

namespace VPet_Simulator.Windows.Interface;
/// <summary>
/// 日程表基础
/// </summary>
public class ScheduleItemBase : NotifyPropertyChangedBase
{
    /// <summary>
    /// 工作日程表
    /// </summary>
    public class WorkScheduleItem
        : ScheduleItemBase
    {
        public Work work { get; set; }
        public WorkScheduleItem()
        {
        }

        public WorkScheduleItem(ImageSource image,
            string workName,
            int workTime)
        {
            Image = image;
            WorkName = workName;
            WorkTime = workTime;
        }

        public ImageSource Image { get; set; }

        public string WorkName { get; set; }

        public int WorkTime { get; set; }

        public bool IsPreviousIsRest { get => _isPreviousIsRest; set => Set(ref _isPreviousIsRest, value); }
        private bool _isPreviousIsRest;
    }

    public class RestScheduleItem
        : ScheduleItemBase
    {
        public RestScheduleItem()
        {
        }

        public RestScheduleItem(int restTime)
        {
            RestTime = restTime;
        }

        public int RestTime { get => _restTime; set => Set(ref _restTime, value); }
        private int _restTime;
    }
}
