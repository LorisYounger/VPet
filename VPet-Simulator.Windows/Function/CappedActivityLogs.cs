using System.Collections.ObjectModel;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// 带上限的活动日志集合 (#451: 原ObservableCollection无限追加导致后台内存恒速增长)
    /// 超过上限时淘汰最旧的日志
    /// </summary>
    public class CappedActivityLogs : ObservableCollection<ActivityLog>
    {
        /// <summary>
        /// 日志最大保留条数
        /// </summary>
        public int MaxCount { get; set; } = 1000;

        public CappedActivityLogs() : base() { }
        public CappedActivityLogs(int maxCount) : base() { MaxCount = maxCount; }

        protected override void InsertItem(int index, ActivityLog item)
        {
            base.InsertItem(index, item);
            while (Count > MaxCount)
                RemoveItem(0);
        }
    }
}
