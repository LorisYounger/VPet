using LinePutScript;
using LinePutScript.Dictionary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 游戏存档 最新版
    /// </summary>
    public class GameSave_v2 : IGetOBJ<ILine>
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
            ILine vpet = lps.FindLine("vpet");
            bool nohashcheck = true;
            long hash;
            if (vpet != null)
            {
                GameSave = GameSave.Load(vpet);
                hash = vpet.GetInt64("hash");
                if (vpet.Remove("hash"))
                {
                    HashCheck = vpet.GetLongHashCode() == hash;
                    nohashcheck = false;
                }
            }
            else if (oldGameSave != null)
            {
                GameSave = oldGameSave;
            }

            if (nohashcheck)
            {
                hash = lps.GetInt64("hash");
                if (lps.Remove("hash"))
                {
                    HashCheck = lps.GetLongHashCode() == hash;
                }
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
            load(lps, oldSave.Statistics, oldSave.GameSave, oldSave.Data);
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

        public ILPS ToLPS()
        {
            var lps = new LPS_D();
            lps.AddRange(Data);
            lps.AddLine(GameSave.ToLine());
            lps.Add(new Line("statistics", "", Statistics.ToSubs()));
            lps.Remove("hash");
            if (HashCheck)
            {
                lps[(gi64)"hash"] = new LPS(lps).GetLongHashCode();
            }
            else
                lps[(gint)"hash"] = -1;
            return lps;
        }
        /// <summary>
        /// Hash检查
        /// </summary>
        public bool HashCheck { get; private set; } = true;



        /// <summary>
        /// 关闭该玩家的HashCheck检查
        /// 请使用imw中的HashCheckOff
        /// </summary>
        public void HashCheckOff()
        {
            HashCheck = false;
        }

        #region GETOBJ
        public DateTime this[gdat subName] { get => Data[subName]; set => Data[subName] = value; }
        public double this[gflt subName] { get => Data[subName]; set => Data[subName] = value; }
        public double this[gdbe subName] { get => Data[subName]; set => Data[subName] = value; }
        public long this[gi64 subName] { get => Data[subName]; set => Data[subName] = value; }
        public int this[gint subName] { get => Data[subName]; set => Data[subName] = value; }
        public bool this[gbol subName] { get => Data[subName]; set => Data[subName] = value; }
        public string this[gstr subName] { get => Data[subName]; set => Data[subName] = value; }
        public ILine this[string subName] { get => Data[subName]; set => Data[subName] = value; }

        public bool GetBool(string subName)
        {
            return Data.GetBool(subName);
        }

        public void SetBool(string subName, bool value)
        {
            Data.SetBool(subName, value);
        }

        public int GetInt(string subName, int defaultvalue = 0)
        {
            return Data.GetInt(subName, defaultvalue);
        }

        public void SetInt(string subName, int value)
        {
            Data.SetInt(subName, value);
        }

        public long GetInt64(string subName, long defaultvalue = 0)
        {
            return Data.GetInt64(subName, defaultvalue);
        }

        public void SetInt64(string subName, long value)
        {
            Data.SetInt64(subName, value);
        }

        public double GetFloat(string subName, double defaultvalue = 0)
        {
            return Data.GetFloat(subName, defaultvalue);
        }

        public void SetFloat(string subName, double value)
        {
            Data.SetFloat(subName, value);
        }

        public DateTime GetDateTime(string subName, DateTime defaultvalue = default)
        {
            return Data.GetDateTime(subName, defaultvalue);
        }

        public void SetDateTime(string subName, DateTime value)
        {
            Data.SetDateTime(subName, value);
        }

        public string GetString(string subName, string defaultvalue = null)
        {
            return Data.GetString(subName, defaultvalue);
        }

        public void SetString(string subName, string value)
        {
            Data.SetString(subName, value);
        }

        public double GetDouble(string subName, double defaultvalue = 0)
        {
            return Data.GetDouble(subName, defaultvalue);
        }

        public void SetDouble(string subName, double value)
        {
            Data.SetDouble(subName, value);
        }
        #endregion
    }
}
