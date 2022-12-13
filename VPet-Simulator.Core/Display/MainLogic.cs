using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace VPet_Simulator.Core
{
    public partial class Main
    {
        /// <summary>
        /// 是否在默认情况(playnoaml)
        /// </summary>
        public bool IsNomal = true;
        
        public Timer EventTimer = new Timer(15000)
        {
            AutoReset = true,
            Enabled = true
        };
        private void EventTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsNomal)
            {
                
            }
        }
    }
}
