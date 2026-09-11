using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 与界面无关的那半扩展方法
    /// </summary>
    /// 这些方法只碰数字和字符串, 两个平台都要用. 留在 Windows 那半的是要 Work
    /// 类型的那几个(Work 在 Core.dll 和 Base.dll 里是两个类型)。
    public static partial class ExtensionFunction
    {
        /// <summary>
        /// 求幂(带符号)
        /// </summary>
        public static double MathPow(double value, double pow)
        {
            return Math.Pow(Math.Abs(value), pow) * Math.Sign(value);
        }

        /// <summary>
        /// 把食物的数值变成一段给玩家看的描述
        /// </summary>
        /// 只列不为零的那几项 —— 一份饮料把"饱腹度"也列出来只是噪音
        public static string FoodToDescription(this IFood food)
        {
            var dic = new List<Tuple<string, double, string>>()
            {
                    new Tuple<string, double, string>("经验值".Translate(), food.Exp, ValueToPlusPlus(food.Exp, 1 / 4, 5)),
                     new Tuple<string, double, string>("饱腹度".Translate(),food.StrengthFood, ValueToPlusPlus(food.StrengthFood, 1 / 2, 5)) ,
                     new Tuple<string, double, string>("口渴度".Translate(), food.StrengthDrink, ValueToPlusPlus(food.StrengthDrink, 1 / 2.5, 5)),
                     new Tuple<string, double, string>("体力".Translate(),food.Strength, ValueToPlusPlus(food.Strength, 1 / 4, 5)),
                     new Tuple<string, double, string>("心情".Translate(), food.Feeling, ValueToPlusPlus(food.Feeling, 1 / 3, 5)),
                    new Tuple<string, double, string>("健康".Translate(),food.Health, ValueToPlusPlus(food.Health, 1, 5)) ,
                     new Tuple<string, double, string>("好感度".Translate(),food.Likability, ValueToPlusPlus(food.Likability, 1.5, 5))
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
        /// UseShellExecute 在三个平台上都能用(Linux 走 xdg-open, macOS 走 open),
        /// 所以主路径是共通的. 只有兜底那一步要分平台 —— 原来写死的 explorer.exe
        /// 在非 Windows 上必定失败。
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
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = FallbackOpener();
                    startInfo.UseShellExecute = false;
                    startInfo.Arguments = url;
                    Process.Start(startInfo);
                }
                catch
                {
                    // 连兜底都不行就算了: 打不开一个链接不该让程序崩掉
                }
            }
        }

        /// <summary>
        /// 各平台用来打开链接的那个程序
        /// </summary>
        private static string FallbackOpener()
        {
            if (OperatingSystem.IsWindows())
                return "explorer.exe";
            if (OperatingSystem.IsMacOS())
                return "open";
            return "xdg-open";
        }
    }
}
