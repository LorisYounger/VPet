using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    public class Food : IFood
    {
        [Line]
        public int Exp { get; set; }
        [Line]
        public double Strength { get; set; }
        [Line]
        public double StrengthFood { get; set; }
        [Line]
        public double StrengthDrink { get; set; }
        [Line]
        public double Feeling { get; set; }
        [Line]
        public double Health { get; set; }
        [Line]
        public double Likability { get; set; }
        /// <summary>
        /// 食物价格
        /// </summary>
        [Line]
        public double Price { get; set; }
    }
}
