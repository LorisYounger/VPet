using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;
using static LinePutScript.Converter.LPSConvert;

namespace VPet_Simulator.Windows.Interface
{
    public class Food : NotifyPropertyChangedBase, IFood
    {

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
            /// 礼品 (没做)
            /// </summary>
            Gift,
            /// <summary>
            /// 不超模的好食物
            /// </summary>
            NoOverLoad,
        }
        /// <summary>
        /// 食物类型
        /// </summary>
        [Line(type: ConvertType.ToEnum, ignoreCase: true)]
        public FoodType Type { get; set; } = FoodType.Food;
        /// <summary>
        /// 食物名字
        /// </summary>
        [Line(name: "name")]
        public string Name { get; set; }
        private string transname = null;
        /// <summary>
        /// 食物名字 (翻译)
        /// </summary>
        public string TranslateName
        {
            get
            {
                if (transname == null)
                {
                    transname = LocalizeCore.Translate(Name);
                }
                return transname;
            }
        }
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
        /// 食物价格
        /// </summary>
        [Line(ignoreCase: true)]
        public double Price { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        [Line(ignoreCase: true)]
        public string Desc { get; set; }
        private string desc = null;
        private string descs = null;
        /// <summary>
        /// 描述(ToBetterBuy)
        /// </summary>
        public string Description
        {
            get
            {
                if (desc == null)
                {
                    StringBuilder sb = new StringBuilder();
                    if (Exp != 0)
                        sb.Append(LocalizeCore.Translate("经验值") + ":\t").Append(Exp > 0 ? "+" : "").Append(Exp.ToString("f2")).AppendLine();
                    if (StrengthFood != 0) sb.Append(LocalizeCore.Translate("饱腹度") + ":\t").Append(StrengthFood > 0 ? "+" : "").Append(StrengthFood.ToString("f2")).Append("\t\t");
                    if (StrengthDrink != 0) sb.Append(LocalizeCore.Translate("口渴度") + ":\t").Append(StrengthDrink > 0 ? "+" : "").Append(StrengthDrink.ToString("f2")).AppendLine();
                    if (Strength != 0) sb.Append(LocalizeCore.Translate("体力") + ":\t").Append(Strength > 0 ? "+" : "").Append(Strength.ToString("f2")).Append("\t\t");
                    if (Feeling != 0)
                        sb.Append("心情".Translate() + ":\t").Append(Feeling > 0 ? "+" : "").Append(Feeling.ToString("f2")).AppendLine();
                    if (Health != 0)
                        sb.Append("健康".Translate() + ":\t").Append(Health > 0 ? "+" : "").Append(Health.ToString("f2")).Append("\t\t");
                    if (Likability != 0)
                        sb.Append("好感度".Translate() + ":\t").Append(Likability > 0 ? "+" : "").Append(Likability.ToString("f2"));
                    desc = sb.ToString();
                }

                return desc + '\n' + descs + '\n' + Desc.Translate();
            }
        }
        /// <summary>
        /// 显示的图片
        /// </summary>
        public BitmapImage ImageSource { get; set; }
        /// <summary>
        /// 是否已收藏
        /// </summary>
        public bool Star { get; set; }
        /// <summary>
        /// 物品图片
        /// </summary>
        [Line(ignoreCase: true)]
        public string Image;
        private bool? isoverload = null;
        /// <summary>
        /// 当前物品推荐价格
        /// </summary>
        public double RealPrice => ((Exp / 4 + Strength / 5 + StrengthDrink / 3 + StrengthFood / 2 + Feeling / 5) / 3 + Health + Likability * 10);
        /// <summary>
        /// 该食物是否超模
        /// </summary>
        public bool IsOverLoad()
        {
            if (isoverload == null)
            {
                double relp = RealPrice;
                isoverload = Price < (relp - 10) * 0.7;// Price > (relp + 10) * 1.3;// || Price < (relp - 10) * 0.7;//30%容错
            }
            return isoverload.Value;
        }
        /// <summary>
        /// 加载物品图片
        /// </summary>
        public void LoadImageSource(IMainWindow imw)
        {
            ImageSource = imw.ImageSources.FindImage(Image ?? Name, "food");
            Star = imw.Set["betterbuy"]["star"].GetInfos().Contains(Name);
            LoadEatTimeSource(imw);
        }
        public void LoadEatTimeSource(IMainWindow imw)
        {
            DateTime now = DateTime.Now;
            DateTime eattime = imw.Set.PetData.GetDateTime("buytime_" + Name, now);
            if (eattime <= now)
            {
                if (Type == FoodType.Meal || Type == FoodType.Snack || Type == FoodType.Drink)
                    descs = "喜好度".Translate();
                else
                    descs = "有效度".Translate();
                descs += ":\t100%";
            }
            else
            {
                if (Type == FoodType.Meal || Type == FoodType.Snack || Type == FoodType.Drink)
                    descs = "喜好度".Translate();
                else
                    descs = "有效度".Translate();
                descs += ":\t" + Math.Max(0.5, 1 - Math.Pow((eattime - now).TotalHours, 2) * 0.01).ToString("p0");
                descs += "\t\t" + "恢复".Translate() + ":\t" + (eattime).ToString("MM/dd HH");
            }
        }
    }
}
