using LinePutScript.Converter;
using Panuon.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
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
        /// <summary>
        /// 描述(ToBetterBuy)
        /// </summary>
        public string Description
        {
            get
            {
                StringBuilder sb = new StringBuilder(Desc);
                return sb.ToString();
            }
        }
        /// <summary>
        /// 显示的图片
        /// </summary>
        public ImageSource ImageSource { get; set; }
        /// <summary>
        /// 是否已收藏
        /// </summary>
        public bool Star { get; set; }
        /// <summary>
        /// 物品图片
        /// </summary>
        [Line(ignoreCase: true)]
        public string Image;
        /// <summary>
        /// 加载物品图片
        /// </summary>
        public void LoadImageSource(IMainWindow imw)
        {
            ImageSource = imw.ImageSources.FindImage(Image ?? Name, "food");
            Star = imw.Set["betterbuy"]["star"].GetInfos().Contains(Name);
        }
    }
}
