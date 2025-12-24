using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// 为所有工作进行1.2倍效率修正
        /// </summary>
        /// <param name="work"></param>
        public static void FixOverLoad(this Work work)
        {
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
            if (spend > 0)
            {
                work.MoneyBase = 2 * (1.15 * Math.Pow(spend, 0.8) - 1) / (2 + work.FinishBonus);

                var lvlimit = 1.1 * work.LevelLimit + 10;
                if (work.Type != Work.WorkType.Work)
                    lvlimit *= 10;

                if (work.Type == Work.WorkType.Work)
                {
                    work.MoneyBase = Math.Round(work.MoneyBase, 1);
                }
                else
                {
                    work.MoneyBase = Math.Round(work.MoneyBase * 10, 1);
                }
                work.MoneyBase = Math.Min(work.MoneyBase, lvlimit);
            }

            // 如果仍然不合理，设定一个默认值
            if (work.IsOverLoad())
            {
                switch (work.Type)
                {
                    case Work.WorkType.Play:
                        work.FinishBonus = 0.2;
                        work.MoneyBase = 18;
                        work.StrengthFood = 1;
                        work.StrengthDrink = 1.5;
                        work.Feeling = -1;
                        work.LevelLimit = 0;
                        break;
                    case Work.WorkType.Work:
                        work.FinishBonus = 0.1;
                        work.MoneyBase = 8;
                        work.StrengthFood = 3.5;
                        work.StrengthDrink = 2.5;
                        work.Feeling = 1;
                        work.LevelLimit = 0;
                        break;
                    case Work.WorkType.Study:
                        work.FinishBonus = 0.2;
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
            w.StrengthFood *= 0.5 + 0.4 * value;
            w.StrengthDrink *= 0.5 + 0.4 * value;
            w.Feeling *= 0.5 + 0.4 * value;
            w.LevelLimit = (work.LevelLimit + 10) * value;
            FixOverLoad(w);
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

        /// <summary>
        /// 启动URL
        /// </summary>
        public static void StartURL(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "explorer.exe";
                startInfo.UseShellExecute = false;
                startInfo.Arguments = url;
                Process.Start(startInfo);
            }
        }

        /// <summary>
        /// 吃食物 附带倍率
        /// </summary>
        /// <param name="save">存档</param>
        /// <param name="food">食物</param>
        /// <param name="buff">默认1倍</param>
        public static void EatFood(this IGameSave save, IFood food, double buff)
        {
            save.Exp += food.Exp * buff;
            var tmp = food.Strength / 2 * buff;
            save.StrengthChange(tmp);
            save.StoreStrength += tmp;
            tmp = food.StrengthFood / 2 * buff;
            save.StrengthChangeFood(tmp);
            save.StoreStrengthFood += tmp;
            tmp = food.StrengthDrink / 2 * buff;
            save.StrengthChangeDrink(tmp);
            save.StoreStrengthDrink += tmp;
            save.FeelingChange(food.Feeling * buff);
            save.Health += food.Health * buff;
            save.Likability += food.Likability * buff;
        }
    }
    /// <summary>
    /// 扩展值
    /// </summary>
    public static partial class ExtensionValue
    {
        /// <summary>
        /// 当前运行目录
        /// </summary>
        public static string BaseDirectory = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;
        /// <summary>
        /// 获取MOD存储目录 (会自动创建/Steam云同步)
        /// 但是还是建议以LPS形式存在Setting/Save里 不保证完整可靠性(可能会因为切换电脑等导致数据丢失)
        /// </summary>
        /// <param name="modName">MOD名字</param>
        /// <returns>目录地址</returns>
        public static string GetMODStorage(string modName)
        {
            var path = Path.Combine(BaseDirectory, "ModData");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, modName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

}
