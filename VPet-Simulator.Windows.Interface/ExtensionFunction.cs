using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphHelper;

namespace VPet_Simulator.Windows.Interface
{
    public static class ExtensionFunction
    {
        /// <summary>
        /// 工作计算等级
        /// </summary>
        public static readonly int[] WorkCalLevel = new int[] { 1, 5, 10, 20, 30, 40, 50, 75, 100, 200 };
        /// <summary>
        /// 判断这个工作是否超模
        /// </summary>
        /// <param name="work">工作</param>
        /// <returns>是否超模</returns>
        public static bool IsOverLoad(this Work work)
        {//判断这个工作是否超模
            if (work.FinishBonus < 0)
                return true;
            var spend = ((work.StrengthFood >= 0 ? 1 : -1) * Math.Pow(work.StrengthFood * 2 + 1, 2) / 6 +
                (work.StrengthDrink >= 0 ? 1 : -1) * Math.Pow(work.StrengthDrink * 2 + 1, 2) / 9 +
               (work.Feeling >= 0 ? 1 : -1) * Math.Pow((work.Type == Work.WorkType.Play ? -1 : 1) * work.Feeling * 2 + 1, 2) / 12) *
                (Math.Pow(work.LevelLimit / 2 + 1, 0.5) / 4 + 1) - 0.5;
            double get = 0;
            foreach (var lv in WorkCalLevel)
            {
                get += (work.MoneyBase + Math.Sqrt(lv) * work.MoneyLevel) * (1 + work.FinishBonus / 2);
            }
            get /= WorkCalLevel.Length;
            if (work.Type != Work.WorkType.Work)
            {
                get /= 12;//经验值换算
            }
            var rel = get / spend;
            if (rel < 0)
                return true;
            if (Math.Abs(get) > (work.LevelLimit + 4) * 3) //等级获取速率限制
                return true;
            return rel > 0.75; // 推荐rel为0.5左右 超过0.75就是超模
        }

        public static string FoodToDescription(this IFood food)
        {
            var dic = new List<Tuple<string, double, string>>()
            {
                    new Tuple<string, double, string>(LocalizeCore.Translate("经验值"), food.Exp, ValueToPlusPlus(food.Exp, 1 / 4, 5)),
                     new Tuple<string, double, string>(LocalizeCore.Translate("饱腹度"),food.StrengthFood, ValueToPlusPlus(food.StrengthFood, 1 / 2, 5)) ,
                     new Tuple<string, double, string>(LocalizeCore.Translate("口渴度"), food.StrengthDrink, ValueToPlusPlus(food.StrengthDrink, 1 / 2.5, 5)),
                     new Tuple<string, double, string>(LocalizeCore.Translate("体力"),food.Strength, ValueToPlusPlus(food.Strength, 1 / 4, 5)),
                     new Tuple<string, double, string>(LocalizeCore.Translate("心情"), food.Feeling, ValueToPlusPlus(food.Feeling, 1 / 3, 5)),
                    new Tuple<string, double, string>(LocalizeCore.Translate("健康"),food.Health, ValueToPlusPlus(food.Health, 1, 5)) ,
                     new Tuple<string, double, string>(LocalizeCore.Translate("好感度"),food.Likability, ValueToPlusPlus(food.Likability, 1.5, 5))
                };
            var dic2 = dic.Where(kv => kv.Item2 != 0)
                         .Select(x => x.Item1 + x.Item3);
            return string.Join("\n", dic2);
        }
        /// <summary>
        /// 把值变成++
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="magnification">倍率</param>
        /// <returns></returns>
        public static string ValueToPlusPlus(double value, double magnification, int max = 10)
        {
            int v = (int)Math.Abs(value);
            v = (int)(Math.Pow(v, magnification));
            v = Math.Min(Math.Max(v, 0), max);
            if (value < 0)
                return new string('-', v);

            else
                return new string('+', v);
        }

    }
    public static class ExtensionValue
    {
        /// <summary>
        /// 当前运行目录
        /// </summary>
        public static string BaseDirectory = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;
    }

}
