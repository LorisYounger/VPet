using LinePutScript;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Security.Cryptography;
using System.Text;
using static VPet_Simulator.Core.GraphHelper;
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
        public GameSave_v2(string petname)
        {
            GameSave = new GameSave_VPet(petname);
            Statistics = new Statistics();
        }
        
        protected void load(ILPS lps, Statistics oldStatistics = null, GameSave_VPet oldGameSave = null, ILPS olddata = null)
        {
            if (lps.FindLine("statistics") == null)
            {//尝试从老存档加载
                Statistics = oldStatistics ?? new Statistics();
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
                GameSave = GameSave_VPet.Load(vpet);
                hash = vpet.GetInt64("hash");
                if (vpet.Remove("hash"))
                {
                    nohashcheck = false;
                    try
                    {
                        using (MD5 md5 = MD5.Create())
                        {
                            long hs = BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(vpet.Name)), 0)
                                * 2 + BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(vpet.info)), 0)
                                * 3 + BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(vpet.text)), 0) * 4;
                            foreach (ISub su in vpet.ToList())
                            {
                                hs += BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(su.Name)), 0) * 2
                                    + BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(su.Info)), 0) * 3;
                            }
                            HashCheck = hs == hash;
                        }
                    }
                    catch
                    {
                        nohashcheck = true;
                    }
                }
            }
            else if (oldGameSave != null)
            {
                GameSave = oldGameSave;
            }

            if (nohashcheck)
            {
                hash = lps.GetInt64("hash");
                int ver = lps["hash"].GetInt("ver");
                if (lps.Remove("hash"))
                {
                    if (ver == 2)
                        HashCheck = Sub.GetHashCode(lps.ToString()) == hash;
                    else
                    {
                        try
                        {
                            using (MD5 md5 = MD5.Create())
                            {
                                HashCheck = BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(lps.ToString())), 0) == hash;
                            }
                            if (!HashCheck)
                                HashCheck = Sub.GetHashCode(lps.ToString()) == hash;
                        }
                        catch (Exception e)
                        {
                            HashCheck = false;
                            MessageBoxX.Show(e.ToString(), "当前存档Hash验证信息".Translate() + ":" + "失败".Translate());
                        }
                    }
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
        public GameSave_v2(ILPS lps, Statistics oldStatistics = null, GameSave_VPet oldGameSave = null, ILPS olddata = null)
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
        public GameSave_VPet GameSave;
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
                lps[(gi64)"hash"] = Sub.GetHashCode(lps.ToString());
                lps["hash"][(gint)"ver"] = 2;
            }
            else
            {
                lps[(gint)"hash"] = -1;
                lps["hash"][(gint)"ver"] = 2;
            }
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
        public FInt64 this[gflt subName] { get => Data[subName]; set => Data[subName] = value; }
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

        public FInt64 GetFloat(string subName, FInt64 defaultvalue = default)
        {
            return Data.GetFloat(subName, defaultvalue);
        }

        public void SetFloat(string subName, FInt64 value)
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
