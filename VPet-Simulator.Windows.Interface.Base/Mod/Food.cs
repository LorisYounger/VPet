using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core;
using static LinePutScript.Converter.LPSConvert;

namespace VPet_Simulator.Windows.Interface
{
    public partial class Food : Item, IFood
    {
        public override string ItemType => "Food";
        /// <summary>
        /// 食物类型
        /// </summary>
        public enum FoodType
        {
            /// <summary>
            /// 食物 (默认)
            /// </summary>
            Food,
            /// <summary>
            /// 收藏 (自定义)
            /// </summary>
            Star,
            /// <summary>
            /// 正餐
            /// </summary>
            Meal,
            /// <summary>
            /// 零食
            /// </summary>
            Snack,
            /// <summary>
            /// 饮料
            /// </summary>
            Drink,
            /// <summary>
            /// 功能性
            /// </summary>
            Functional,
            /// <summary>
            /// 药品
            /// </summary>
            Drug,
            /// <summary>
            /// 礼品
            /// </summary>
            Gift,
        }
        /// <summary>
        /// 食物类型
        /// </summary>
        [Line(type: ConvertType.ToEnum, ignoreCase: true)]
        public FoodType Type { get; set; } = FoodType.Food;
      
        [Line(ignoreCase: true)]
        public int Exp { get; set; }
        [Line(ignoreCase: true)]
        public double Strength { get; set; }
        [Line(ignoreCase: true)]
        public double StrengthFood { get; set; }
        [Line(ignoreCase: true)]
        public double StrengthDrink { get; set; }
        [Line(ignoreCase: true)]
        public double Feeling { get; set; }
        [Line(ignoreCase: true)]
        public double Health { get; set; }
        [Line(ignoreCase: true)]
        public double Likability { get; set; }

        /// <summary>
        /// 描述(ToBetterBuy)
        /// </summary>

        public override string Description
        {
            get
            {
                return Data + '\n' + Desc.Translate();
            }
        }

        public IDictionary<string, string> DescriptionValues
        {
            get
            {
                var dic = new Dictionary<string, double>()
                {
                    { "经验值".Translate(), (double)Exp },
                    { "饱腹度".Translate(), StrengthFood },
                    { "口渴度".Translate(), StrengthDrink },
                    { "体力".Translate(), Strength },
                    { "心情".Translate(), Feeling },
                    { "健康".Translate(), Health },
                    { "好感度".Translate(), Likability },
                };
                return dic.Where(kv => kv.Value != 0)
                    .ToDictionary(kv => kv.Key, kv => $"{(kv.Value > 0 ? "+" : "")}{kv.Value.ToString("f2")}");
            }
        }

       
        /// <summary>
        /// 是否已收藏
        /// </summary>
        public override bool Star { get; set; }
       
        public bool? isoverload = null;
        /// <summary>
        /// 当前物品推荐价格
        /// </summary>
        public double RealPrice => VPet_Simulator.Unified.Services.FoodPricing.RealPrice(
            Exp, Strength, StrengthFood, StrengthDrink, Feeling, Health, Likability);
        /// <summary>
        /// 该食物是否超模
        /// </summary>
        public bool IsOverLoad()
        {
            if (isoverload == null)
            {
                double relp = RealPrice;
                isoverload = VPet_Simulator.Unified.Services.FoodPricing.IsOverLoad(Price, relp);//30%容错
            }
            return isoverload.Value;
        }
        /// <summary>
        /// 食用时显示的动画
        /// </summary>
        [Line(ignoreCase: true)]
        public string? Graph { get; set; } = null;
        /// <summary>
        /// 获取食用时显示的动画
        /// </summary>
        public string GetGraph()
        {
            if (string.IsNullOrEmpty(Graph))
                switch (Type)
                {
                    default:
                        return "eat";
                    case Food.FoodType.Drink:
                        return "drink";
                    case Food.FoodType.Gift:
                        return "gift";
                }
            else
                return Graph;
        }
        /// <summary>
        /// 克隆食物对象
        /// </summary>
        public Food Clone()
        {
            return (Food)MemberwiseClone();
        }
    }
}
