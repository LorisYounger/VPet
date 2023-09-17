using LinePutScript;
using LinePutScript.Dictionary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 游戏存档 修改版
    /// </summary>
    public class GameSave_v2 : GameSave
    {
        public GameSave_v2(ILPS lps, GameSave_v2 oldsave = null)
        {
            if (lps.FindLine("statistics") == null)
            {//尝试从老存档加载
                Statistics = oldsave?.Statistics;
            }
            else
            {
                Statistics = new Statistics(lps["statistics"].ToList());
            }
            if (lps.FindLine("vpet") == null)
            {

            }
        }
        public LPS_D Data;
        public Statistics Statistics = null;

    }
}
