using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public static bool IsOverLoad(Work work)
        {//判断这个工作是否超模
            var spend = (Math.Pow(work.StrengthFood * 2 + 1, 2) / 6 + Math.Pow(work.StrengthDrink * 2 + 1, 2) / 9 +
                Math.Pow(work.Feeling * 2 + 1, 2) / 12) * (Math.Pow(work.LevelLimit / 2 + 1, 0.5) / 4 + 1) - 0.5;
            var get = (work.MoneyBase + work.MoneyLevel * 10) * (work.MoneyLevel + 1) * (1 + work.FinishBonus / 2);
            if(work.Type != Work.WorkType.Work)
            {
                get /= 12;//经验值换算
            }
            var rel = get / spend;
            return rel > 2; // 推荐rel为1.0-1.4之间 超过2.0就是超模
        }
    }
}
