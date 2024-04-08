using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphHelper;

namespace VPet_Simulator.Windows.Interface
{
    public static class ExtensionFunction
    {
        /// <summary>
        /// 工作获取效率
        /// </summary>
        /// <param name="work">工作</param>
        /// <returns>工作获取效率</returns>
        public static double Get(this Work work)
        {
            if (work.Type == Work.WorkType.Work)
                return MathPow(Math.Abs(work.MoneyBase) * (1 + work.FinishBonus / 2) + 1, 1.25);
            else
                return MathPow((Math.Abs(work.MoneyBase) * (1 + work.FinishBonus / 2) + 1) / 10, 1.25);
        }
        /// <summary>
        /// 求幂(带符号)
        /// </summary>
        public static double MathPow(double value, double pow)
        {
            return Math.Pow(Math.Abs(value), pow) * Math.Sign(value);
        }
        /// <summary>
        /// 工作花费效率
        /// </summary>
        /// <param name="work">工作</param>
        /// <returns>工作花费效率</returns>
        public static double Spend(this Work work)
        {
            return (MathPow(work.StrengthFood, 1.5) / 3 + MathPow(work.StrengthDrink, 1.5) / 4 + MathPow(work.Feeling, 1.5) / 4 +
                work.LevelLimit / 10.0 + MathPow(work.StrengthFood + work.StrengthDrink + work.Feeling, 1.5) / 10) * 3;
        }
        /// <summary>
        /// 判断这个工作是否超模
        /// </summary>
        /// <param name="work">工作</param>
        /// <returns>是否超模</returns>
        public static bool IsOverLoad(this Work work)
        {//判断这个工作是否超模
            if (work.LevelLimit < 0)
                work.LevelLimit = 0;
            if (work.FinishBonus < 0)
                work.FinishBonus = 0;
            if (work.Type == Work.WorkType.Play && work.Feeling > 0)
                work.Feeling *= -1;//旧版本代码兼容
            if (work.Time < 10)
                work.Time = 10;
            if (work.FinishBonus > 2)
                work.FinishBonus = 2;

            var spend = work.Spend();
            var get = work.Get();
            var rel = get / spend;
            if (rel < 0)
                return true;
            var lvlimit = 1.1 * work.LevelLimit + 10;
            if (work.Type != Work.WorkType.Work)
                lvlimit *= 10;
            if (Math.Abs(work.MoneyBase) > lvlimit) //等级获取速率限制
                return true;
            return rel > 1.4; // 推荐rel为1左右 超过1.3就是超模
        }
        /// <summary>
        /// 数值梯度下降法 修复超模工作
        /// </summary>
        /// <param name="work"></param>
        public static void FixOverLoad(this Work work)
        {
            // 设置梯度下降的步长和最大迭代次数
            double stepSize = 0.01;
            int maxIterations = 100;

            if (work.LevelLimit < 0)
                work.LevelLimit = 0;
            if (work.FinishBonus < 0)
                work.FinishBonus = 0;
            if (work.Type == Work.WorkType.Play && work.Feeling > 0)
                work.Feeling *= -1;//旧版本代码兼容
            if (work.Time < 10)
                work.Time = 10;
            if (work.FinishBonus > 2)
                work.FinishBonus = 2;

            for (int i = 0; i < maxIterations; i++)
            {
                while (Math.Abs(work.Get()) > 1.1 * work.LevelLimit + 10) //等级获取速率限制
                {
                    work.MoneyBase /= 2;
                }

                // 判断是否已经合理
                if (!work.IsOverLoad())
                {
                    return;
                }


                // 计算当前的Spend和Get
                double currentSpend = work.Spend();
                double currentGet = work.Get();

                // 为每个参数增加一个小的delta值，然后重新计算Spend和Get
                double delta = 0.0001;
                work.MoneyBase += delta;
                double getGradient = (work.Get() - currentGet) / delta;
                work.MoneyBase -= delta; // 还原MoneyBase的值

                work.StrengthFood += delta;
                work.StrengthDrink += delta;
                work.Feeling += delta;

                double spendGradient = (work.Spend() - currentSpend) / delta;
                // 还原所有的值
                work.StrengthFood -= delta;
                work.StrengthDrink -= delta;
                work.Feeling -= delta;

                // 根据梯度更新属性值
                work.MoneyBase += stepSize * getGradient;
                work.StrengthFood -= stepSize * spendGradient;
                work.StrengthDrink -= stepSize * spendGradient;
                work.Feeling -= stepSize * spendGradient;
            }

            // 如果仍然不合理，设定一个默认值
            if (work.IsOverLoad())
            {
                switch (work.Type)
                {
                    case Work.WorkType.Play:
                        work.MoneyBase = 18;
                        work.StrengthFood = 1;
                        work.StrengthDrink = 1.5;
                        work.Feeling = -1;
                        work.LevelLimit = 0;
                        break;
                    case Work.WorkType.Work:
                        work.MoneyBase = 8;
                        work.StrengthFood = 3.5;
                        work.StrengthDrink = 2.5;
                        work.Feeling = 1;
                        work.LevelLimit = 0;
                        break;
                    case Work.WorkType.Study:
                        work.MoneyBase = 80;
                        work.StrengthFood = 2;
                        work.StrengthDrink = 2;
                        work.Feeling = 3;
                        work.LevelLimit = 0;
                        break;
                }
            }
        }
        /// <summary>
        /// 将工作的属性值翻倍
        /// </summary>
        public static Work Double(this Work work, int value)
        {
            if (value == 1) return work;
            Work w = (Work)work.Clone();
            w.MoneyBase *= value;
            w.StrengthFood *= 0.48 + 0.6 * value;
            w.StrengthDrink *= 0.48 + 0.6 * value;
            w.Feeling *= 0.48 + 0.6 * value;
            w.LevelLimit = (work.LevelLimit + 10) * value;
            return w;
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
    public static partial class ExtensionValue
    {
        /// <summary>
        /// 当前运行目录
        /// </summary>
        public static string BaseDirectory = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;
    }

}
