using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VPet_Simulator.Core.Main;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 所有可以检查的文本格式
    /// </summary>
    public class ICheckText
    {
        [Line(ignoreCase: true)]
        public int mode { get; set; } = 7;
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
        /// 好感度要求:最小值
        /// </summary>
        [Line(IgnoreCase = true)]
        public double LikeMin { get; set; } = 0;
        /// <summary>
        /// 好感度要求:最大值
        /// </summary>
        [Line(IgnoreCase = true)]
        public double LikeMax { get; set; } = int.MaxValue;
        /// <summary>
        /// 健康度要求:最小值
        /// </summary>
        [Line(IgnoreCase = true)]
        public double HealthMin { get; set; } = 0;
        /// <summary>
        /// 健康度要求:最大值
        /// </summary>
        [Line(IgnoreCase = true)]
        public double HealthMax { get; set; } = int.MaxValue;
        /// <summary>
        /// 等级要求:最小值
        /// </summary>
        [Line(IgnoreCase = true)] public double LevelMin { get; set; } = 0;
        /// <summary>
        /// 等级要求:最大值
        /// </summary>
        [Line(IgnoreCase = true)] public double LevelMax { get; set; } = int.MaxValue;
        /// <summary>
        /// 金钱要求:最小值
        /// </summary>
        [Line(IgnoreCase = true)] public double MoneyMin { get; set; } = int.MinValue;
        /// <summary>
        /// 金钱要求:最大值
        /// </summary>
        [Line(IgnoreCase = true)] public double MoneyMax { get; set; } = int.MaxValue;
        /// <summary>
        /// 食物要求:最小值
        /// </summary>
        [Line(IgnoreCase = true)] public double FoodMin { get; set; } = 0;
        /// <summary>
        /// 食物要求:最大值
        /// </summary>
        [Line(IgnoreCase = true)] public double FoodMax { get; set; } = int.MaxValue;
        /// <summary>
        /// 口渴要求:最小值
        /// </summary>
        [Line(IgnoreCase = true)] public double DrinkMin { get; set; } = 0;
        /// <summary>
        /// 口渴要求:最大值
        /// </summary>
        [Line(IgnoreCase = true)] public double DrinkMax { get; set; } = int.MaxValue;
        /// <summary>
        /// 心情要求:最小值
        /// </summary>
        [Line(IgnoreCase = true)] public double FeelMin { get; set; } = 0;
        /// <summary>
        /// 心情要求:最大值
        /// </summary>
        [Line(IgnoreCase = true)] public double FeelMax { get; set; } = int.MaxValue;
        /// <summary>
        /// 体力要求:最小值
        /// </summary>
        [Line(IgnoreCase = true)] public double StrengthMin { get; set; } = 0;
        /// <summary>
        /// 体力要求:最大值
        /// </summary>
        [Line(IgnoreCase = true)] public double StrengthMax { get; set; } = int.MaxValue;

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
        public virtual bool CheckState(IGameSave save)
        {
            if (save.Likability < LikeMin || save.Likability > LikeMax)
                return false;
            if (save.Health < HealthMin || save.Health > HealthMax)
                return false;
            if (save.Level < LevelMin || save.Level > LevelMax)
                return false;
            if (save.Money < MoneyMin || save.Money > MoneyMax)
                return false;
            if (save.StrengthFood < FoodMin || save.StrengthFood > FoodMax)
                return false;
            if (save.StrengthDrink < DrinkMin || save.StrengthDrink > DrinkMax)
                return false;
            if (save.Feeling < FeelMin || save.Feeling > FeelMax)
                return false;
            if (save.Strength < StrengthMin || save.Strength > StrengthMax)
                return false;
            return true;
        }
        /// <summary>
        /// 检查部分状态是否满足需求
        /// </summary>之所以不是全部的,是因为挨个取效率太差了
        public virtual bool CheckState(Main m) => CheckState(m.Core.Save);

        /// <summary>
        /// 将文本转换成实际值
        /// </summary>
        public string ConverText(Main m) => ConverText(TranslateText, m);
        /// <summary>
        /// 将文本转换成实际值
        /// </summary>
        public static string ConverText(string text, Main m)
        {
            if (text.Contains('{') && text.Contains('}'))
            {
                return text.Replace("{name}", m.Core.Save.Name).Replace("{food}", m.Core.Save.StrengthFood.ToString("f0"))
                    .Replace("{drink}", m.Core.Save.StrengthDrink.ToString("f0")).Replace("{feel}", m.Core.Save.Feeling.ToString("f0")).
                    Replace("{strength}", m.Core.Save.Strength.ToString("f0")).Replace("{money}", m.Core.Save.Money.ToString("f0"))
                    .Replace("{level}", m.Core.Save.Level.ToString("f0")).Replace("{health}", m.Core.Save.Health.ToString("f0"));
            }
            else
                return text;
        }
    }
}
