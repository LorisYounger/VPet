using LinePutScript;
using LinePutScript.Dictionary;
using System;
using System.Security.Cryptography;
using System.Text;
using VPet_Simulator.Core;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 游戏存档 最新版
    /// </summary>
    public partial class GameSave_v2 : IGetOBJ<ILine>
    {
        /// <summary>
        /// 新存档
        /// </summary>
        public GameSave_v2(string petname)
        {
            GameSave = new GameSave_VPet(petname);
        }

        protected void load(ILPS lps, Statistics? oldStatistics = null, GameSave_VPet? oldGameSave = null, ILPS? olddata = null)
        {
            if (lps.FindLine("statistics") == null)
            {//尝试从老存档加载
                Statistics = oldStatistics ?? new Statistics();
            }
            else
            {
                Statistics = new Statistics(lps["statistics"].ToList());
            }
            ILine? vpet = lps.FindLine("vpet");
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
                // 哈希算法搬到了 VPet-Simulator.Unified 的 SaveHash:
                // Windows 版和跨平台版加载同一个 dll, 存档在哪边算出来的结果都一样.
                // 算法本身(含 ver2 之前那条 MD5 回退)一字未改.
                try
                {
                    HashCheck = SaveHash.Verify(lps, out _);
                }
                catch (Exception e)
                {
                    HashCheck = false;
                    ReportHashCheckError(e);
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
        public GameSave_v2(ILPS lps, Statistics? oldStatistics = null, GameSave_VPet? oldGameSave = null, ILPS? olddata = null)
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
        public GameSave_VPet GameSave = new GameSave_VPet();
        /// <summary>
        /// 统计
        /// </summary>
        public Statistics Statistics = new Statistics();

        public ILPS ToLPS()
        {
            var lps = new LPS_D();
            lps.AddRange(Data);
            lps.AddLine(GameSave.ToLine());
            lps.Add(new Line("statistics", "", Statistics.ToSubs()));
            SaveHash.Write(lps, HashCheck);
            return lps;
        }

        /// <summary>
        /// 老存档的哈希校验炸了时怎么告诉玩家
        /// </summary>
        /// 分部方法, 由各平台自己实现: Windows 版弹 MessageBoxX, 跨平台版走
        /// 自己的对话框. 分部方法是 private 的, 不进公开 API 表面.
        partial void ReportHashCheckError(Exception e);

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
        public string? this[gstr subName] { get => Data[subName]; set => Data[subName] = value; }
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

        public string? GetString(string subName, string? defaultvalue = null)
        {
            return Data.GetString(subName, defaultvalue);
        }

        public void SetString(string subName, string? value)
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
