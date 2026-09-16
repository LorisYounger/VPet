using LinePutScript.Converter;
using System;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 日程表的共享半
    /// </summary>
    /// 这里只放嵌套的 Package: 它是要存进存档的数据, 两个平台必须读得出同一份.
    /// 日程表本体和 PackageFull 留在 Windows 半 —— 前者通篇拿着 IMainWindow,
    /// 后者有个 WorkType 属性, 而 GraphHelper.Work 在两个 Core 里是两个类型.
    ///
    /// 类型必须保持嵌套: ScheduleTask.Package 是 MOD 能直接写出来的全名,
    /// 挪成顶层类型等于改名字, 会破坏已编译的 MOD.
    public partial class ScheduleTask
    {
        /// <summary>
        /// 套餐信息
        /// </summary>
        public partial class Package
        {
            public Package()
            {
            }
            /// <summary>
            /// 套餐名称
            /// </summary>
            [Line] public string Name { get; set; } = string.Empty;
            /// <summary>
            /// 协议名称 (已翻译)
            /// </summary>
            public string NameTrans
            {
                get
                {
                    if (string.IsNullOrEmpty(nametrans))
                    {
                        nametrans = string.IsNullOrEmpty(Name) ? "" : Name.Translate();
                    }
                    return nametrans;
                }
                set => nametrans = value;
            }
            private string? nametrans;
            /// <summary>
            /// 描述
            /// </summary>
            [Line] public string Describe { get; set; } = string.Empty;
            /// <summary>
            /// 描述 已翻译
            /// </summary>
            public string DescribeTrans
            {
                get
                {
                    if (string.IsNullOrEmpty(describetrans))
                    {
                        describetrans = string.IsNullOrEmpty(Describe) ? "" : Describe.Translate();
                    }
                    return describetrans;
                }
                set => describetrans = value;
            }
            private string? describetrans;
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
            [Line] public DateTime EndTime { get; set; } = DateTime.MinValue;
            /// <summary>
            /// 是否自动续费
            /// </summary>
            [Line] public bool AutoRenew { get; set; } = false;
            /// <summary>
            /// 可用等级
            /// </summary>
            [Line] public int Level { get; set; }
            /// <summary>
            /// 是否生效
            /// </summary>
            /// <returns>判断套餐是否生效</returns>
            public bool IsActive() => SchedulePackageRules.IsActive(EndTime, DateTime.Now);

        }
    }
}
