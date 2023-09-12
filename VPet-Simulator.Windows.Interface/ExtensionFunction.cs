using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static VPet_Simulator.Core.GraphHelper;

namespace VPet_Simulator.Windows.Interface
{
    public static class ExtensionFunction
    {
        /// <summary>
        /// 判断这个工作是否超模
        /// </summary>
        /// <param name="work">工作</param>
        /// <returns>是否超模</returns>
        public static bool IsOverLoad(this Work work)
        {//判断这个工作是否超模
            var spend = ((work.StrengthFood >= 0 ? 1 : -1) * Math.Pow(work.StrengthFood * 2 + 1, 2) / 6 +
                (work.StrengthDrink >= 0 ? 1 : -1) * Math.Pow(work.StrengthDrink * 2 + 1, 2) / 9 +
               (work.Feeling >= 0 ? 1 : -1) * Math.Pow((work.Type == Work.WorkType.Play ? -1 : 1) * work.Feeling * 2 + 1, 2) / 12) *
                (Math.Pow(work.LevelLimit / 2 + 1, 0.5) / 4 + 1) - 0.5;        
            var get = (work.MoneyBase + work.MoneyLevel * 10) * (work.MoneyLevel + 1) * (1 + work.FinishBonus / 2);
            if (work.Type != Work.WorkType.Work)
            {
                get /= 12;//经验值换算
            }
            var rel = get / spend;
            if (rel < 0)
                return true;
            return rel > 2; // 推荐rel为1.0-1.4之间 超过2.0就是超模
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
