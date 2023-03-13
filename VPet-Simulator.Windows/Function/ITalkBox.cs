using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VPet_Simulator.Windows
{
    public interface ITalkBox
    {
        /// <summary>
        /// 当前UI
        /// </summary>
        UIElement This { get; }
    }
}
