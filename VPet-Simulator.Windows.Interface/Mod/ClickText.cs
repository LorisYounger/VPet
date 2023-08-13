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
    public class ClickText
    {
        public ClickText()
        {

        }
        public ClickText(string text)
        {
            Text = text;
        }

        [Line(ignoreCase: true)]
        private int mode { get; set; } = 7;
        /// <summary>
        /// 需求状态模式
        /// </summary>      
        public ModeType Mode
        {
            get => (ModeType)mode;
            set => mode = (int)value;
        }
        /// <summary>
        /// 宠物状态模式
        /// </summary>
        [Flags]
        public enum ModeType
        {
            /// <summary>
            /// 高兴
            /// </summary>
            Happy = 1,
            /// <summary>
            /// 普通
            /// </summary>
            Nomal = 2,
            /// <summary>
            /// 状态不佳
            /// </summary>
            PoorCondition = 4,
            /// <summary>
            /// 生病(躺床)
            /// </summary>
            Ill = 8
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
        /// 好感度要求:最小值
        /// </summary>
        [Line(IgnoreCase = true)]
        public int LikeMin = 0;
        /// <summary>
        /// 好感度要求:最大值
        /// </summary>
        [Line(IgnoreCase = true)]
        public int LikeMax = int.MaxValue;
        /// <summary>
        /// 工作状态
        /// </summary>
        [Line(IgnoreCase = true)]
        public WorkingState State { get; set; } = WorkingState.Nomal;
        /// <summary>
        /// 说话的内容
        /// </summary>
        [Line(IgnoreCase = true)] public string Text { get; set; }

        private string transText = null;
        /// <summary>
        /// 说话的内容 (翻译)
        /// </summary>
        public string TranslateText
        {
            get
            {
                if (transText == null)
                {
                    transText = LocalizeCore.Translate(Text);
                }
                return transText;
            }
            set
            {
                transText = value;
            }
        }
        /// <summary>
        /// 检查部分状态是否满足需求
        /// </summary>之所以不是全部的,是因为挨个取效率太差了      
        public bool CheckState(Main m)
        {
            if (m.Core.Save.Likability < LikeMin || m.Core.Save.Likability > LikeMax)
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
