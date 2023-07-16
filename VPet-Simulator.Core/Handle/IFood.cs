using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 食物接口
    /// </summary>
    public interface IFood
    {
        /// <summary>
        /// 经验值
        /// </summary>
        int Exp { get; }

        /// <summary>
        /// 体力 0-100
        /// </summary>
        double Strength { get; }
        /// <summary>
        /// 饱腹度
        /// </summary>
        double StrengthFood { get; }
        /// <summary>
        /// 口渴度
        /// </summary>
        double StrengthDrink { get; }

        /// <summary>
        /// 心情
        /// </summary>
        double Feeling { get; }

        /// <summary>
        /// 健康
        /// </summary>
        double Health { get; }

        /// <summary>
        /// 好感度
        /// </summary>
        double Likability { get; }
    }
}
