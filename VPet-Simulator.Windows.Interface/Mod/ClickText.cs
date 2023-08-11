using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 点击桌宠时触发的乱说话
    /// </summary>
    public class ClickText
    {
        [Line(ignoreCase: true)]
        private int mode { get; set; } = 15;
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
        }
    }
}
