using System;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Windows.Interface
{
    public partial class ScheduleTask
    {
        /// <summary>
        /// 套餐的 Windows 半
        /// </summary>
        /// 只剩这个构造: 它要 PackageFull, 而 PackageFull 有个 WorkType 属性 ——
        /// GraphHelper.Work 在 Core.dll 和跨平台 Core 里是两个不同的类型, 共享不了.
        public partial class Package
        {
            public Package(PackageFull packageFull, int level)
            {
                Name = packageFull.Name;
                Describe = packageFull.Describe;
                Commissions = packageFull.Commissions;
                //定价、到期时间、可用等级的算式都在共享后端里
                Price = SchedulePackageRules.SignPrice(packageFull.Price, level);
                EndTime = SchedulePackageRules.EndTime(DateTime.Now, packageFull.Duration);
                Level = SchedulePackageRules.GrantedLevel(level, packageFull.LevelInNeed);
            }
        }
    }
}
