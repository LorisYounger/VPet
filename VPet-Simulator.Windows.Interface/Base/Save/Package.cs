using System;
using LinePutScript.Converter;

namespace VPet_Simulator.Windows.Interface;

public partial class ScheduleTask
{
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
        /// 扣除比例，工作金钱和学习经验的实际收益倍率为 1 - Commissions，限制在 10%～1000%。
        /// </summary>
        private double commissions;
        [Line]
        public double Commissions
        {
            get => commissions;
            set => commissions = Math.Clamp(value, -9, 0.9);
        }
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
        public bool IsActive() => DateTime.Now < EndTime;


    }
}
