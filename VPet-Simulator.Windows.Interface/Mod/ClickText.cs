using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.Main;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 点击桌宠时触发的乱说话
    /// </summary>
    public class ClickText : ICheckText
    {
        public ClickText()
        {

        }
        public ClickText(string text)
        {
            Text = text;
        }

      
        /// <summary>
        /// 指定干活时说, 空为任意, sleep 为睡觉时
        /// </summary>
        [Line(ignoreCase: true)]
        public string Working { get; set; } = null;

        /// <summary>
        /// 日期区间
        /// </summary>
        [Flags]
        public enum DayTime
        {
            Morning = 1,
            Afternoon = 2,
            Night = 4,
            Midnight = 8,
        }
        /// <summary>
        /// 当前时间
        /// </summary>
        [Line(ignoreCase: true)]
        private int dayTime { get; set; } = 15;
        /// <summary>
        /// 日期区间
        /// </summary>      
        public DayTime DaiTime
        {
            get => (DayTime)dayTime;
            set => dayTime = (int)value;
        }
     
        /// <summary>
        /// 工作状态
        /// </summary>
        [Line(IgnoreCase = true)]
        public WorkingState State { get; set; } = WorkingState.Nomal;
      
        /// <summary>
        /// 检查部分状态是否满足需求
        /// </summary>之所以不是全部的,是因为挨个取效率太差了      
        public override bool CheckState(Main m)
        {
            if (!base.CheckState(m))
                return false;

            if (string.IsNullOrWhiteSpace(Working))
            {
                if (State != m.State)
                    return false;
            }
            else
            {
                if (m.State != WorkingState.Work)
                    return false;
                if (m.nowWork.Name != Working)
                    return false;
            }
            return true;
        }
    }
}
