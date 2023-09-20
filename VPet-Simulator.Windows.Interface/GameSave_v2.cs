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
    /// 游戏存档 最新版
    /// </summary>
    public class GameSave_v2
    {
        /// <summary>
        /// 新存档
        /// </summary>
        public GameSave_v2()
        {
            GameSave = new GameSave();
            Statistics = new Statistics();
        }
        protected void load(ILPS lps, Statistics oldStatistics = null, GameSave oldGameSave = null, ILPS olddata = null)
        {
            if (lps.FindLine("statistics") == null)
            {//尝试从老存档加载
                Statistics = oldStatistics;
            }
            else
            {
                Statistics = new Statistics(lps["statistics"].ToList());
            }
            if (lps.FindLine("vpet") != null)
            {
                GameSave = GameSave.Load(lps.FindLine("vpet"));
            }
            else if (oldGameSave != null)
            {
                GameSave = oldGameSave;
            }
            if (olddata != null)
                Data.AddRange(olddata);
            Data.AddRange(lps);
        }
        /// <summary>
        /// 读存档, 带入老数据
        /// </summary>
        /// <param name="lps">数据</param>
        /// <param name="oldStatistics">老统计</param>
        /// <param name="oldGameSave">老存档</param>
        /// <param name="olddata">老数据</param>
        public GameSave_v2(ILPS lps, Statistics oldStatistics = null, GameSave oldGameSave = null, ILPS olddata = null)
        {
            load(lps, oldStatistics, oldGameSave, olddata);
        }
        /// <summary>
        /// 读存档, 带入老存档
        /// </summary>
        /// <param name="lps"></param>
        /// <param name="oldSave"></param>
        public GameSave_v2(ILPS lps, GameSave_v2 oldSave)
        {
            load(lps, oldSave.Statistics,oldSave.GameSave,oldSave.Data);
        }

        /// <summary>
        /// 游戏相关数据
        /// </summary>
        public LPS_D Data = new LPS_D();
        /// <summary>
        /// 游戏存档
        /// </summary>
        public GameSave GameSave;
        /// <summary>
        /// 统计
        /// </summary>
        public Statistics Statistics = null;

        public ILPS Save()
        {
            var lps = new LPS_D();
            lps.AddRange(Data);
            lps.AddLine(GameSave.ToLine());
            lps.Add(new Line("statistics", "", Statistics.ToSubs()));
            return lps;
        }
    }
}
