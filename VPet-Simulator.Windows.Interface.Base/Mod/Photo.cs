using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Windows.Interface;

public partial class Photo
{
    public Photo() { }

    public Photo(Line line)
    {
        Zip = line[(gstr)"zip"]!;
        Path = line[(gstr)"path"]!;
        if (Enum.TryParse<PhotoType>(line[(gstr)"type"], true, out var tp))
            Type = tp;
        Name = line[(gstr)"name"]!;
        Description = line[(gstr)"desc"]!;
        var tags = line.Find("tags");
        if (tags != null)
            Tags = tags.GetInfos().ToList();

        UnlockAble = new UnlockCondition(line);
    }
    /// <summary>
    /// 图片所在ZIP
    /// </summary>
    public string Zip { get; set; } = string.Empty;
    /// <summary>
    /// 图片所在位置
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 图片类型
    /// </summary>
    public enum PhotoType
    {
        /// <summary>
        /// 默认类型
        /// </summary>
        ALL,
        /// <summary>
        /// 插图 
        /// </summary>
        Illustration,
        /// <summary>
        /// 小图 (表情包,头像等)
        /// </summary>
        Thumbnail
    }
    /// <summary>
    /// 图片类型
    /// </summary>
    public PhotoType Type { get; set; } = PhotoType.ALL;
    /// <summary>
    /// 图片名字
    /// </summary>
    public string Name { get; set; } = string.Empty;
    private string? transname = null;
    /// <summary>
    /// 图片名字 (翻译)
    /// </summary>
    public string TranslateName
    {
        get
        {
            if (transname == null)
            {
                transname = Name.Translate();
            }
            return transname;
        }
    }
    /// <summary>
    /// 标签
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();
    private List<string>? tagstrans = null;
    /// <summary>
    /// 标签 (翻译)
    /// </summary>
    public List<string> TagsTrans
    {
        get
        {
            if (tagstrans == null)
            {
                tagstrans = Tags.Select(x => x.Translate()).ToList();
            }
            return tagstrans;
        }
    }
    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 解锁条件
    /// </summary>
    public class UnlockCondition
    {
        public UnlockCondition() { }

        public UnlockCondition(Line line)
        {
            var sub = line.Find("llockstring");
            if (sub != null)
                LockString = sub.Info;
            sub = line.Find("llock");
            if (sub != null)
                Lock = sub.GetBoolean();
            sub = line.Find("lnone");
            if (sub != null)
                None = sub.GetBoolean();
            sub = line.Find("lsellprice");
            if (sub != null)
                SellPrice = sub.GetInteger();
            sub = line.Find("lsellboth");
            if (sub != null)
                SellBoth = sub.GetBoolean();
            sub = line.Find("llevel");
            if (sub != null)
                Level = sub.GetInteger();
            sub = line.Find("llevelmax");
            if (sub != null)
                LevelMax = sub.GetInteger();
            sub = line.Find("lmoney");
            if (sub != null)
                Money = sub.GetInteger();
            sub = line.Find("llikability");
            if (sub != null)
                Likability = sub.GetInteger();
            sub = line.Find("lfeeling");
            if (sub != null)
                Feeling = sub.GetInteger();
            sub = line.Find("ldate");
            if (sub != null)
            {
                var dt = sub.Info.Split('/', '-');
                try
                {
                    Date = new DateOnly(DateTime.Now.Year, int.Parse(dt[0]), int.Parse(dt[1]));
                }
                catch
                {
                    Date = new DateOnly(DateTime.Now.Year, 1, 1);
                }
            }

            sub = line.Find("ltime");
            if (sub != null)
                Time = TimeOnly.Parse(sub.Info);
            sub = line.Find("ldateoffset");
            if (sub != null)
                DateOffset = sub.GetInteger();
            sub = line.Find("ltimeoffset");
            if (sub != null)
                TimeOffset = sub.GetInteger();
            sub = line.Find("lholiday");
            if (sub != null)
                Holiday = Enum.Parse<HolidayType>(sub.Info);

            foreach (var sub2 in line.Subs.FindAll(x => x.Name.StartsWith("ls_")))
            {
                StatCheck.Add((sub2.Name.Substring(3), sub2.GetInteger()));
            }
        }

        /// <summary>
        /// 解锁条件 (仅程序锁定显示)
        /// </summary>
        public string LockString { get; set; } = "由程序锁定";
        /// <summary>
        /// 是否强制锁定不可解锁, 仅限于程序手动解锁
        /// </summary>
        public bool Lock { get; set; } = false;
        /// <summary>
        /// 是否不需要任何条件直接解锁
        /// </summary>
        public bool None { get; set; } = false;
        /// <summary>
        /// 可花钱解锁
        /// </summary>
        public int SellPrice { get; set; } = -1;
        /// <summary>
        /// 是否要满足条件后才能花钱解锁
        /// </summary>
        public bool SellBoth { get; set; } = false;
        /// <summary>
        /// 判断统计内容条件
        /// </summary>
        public List<(string, int)> StatCheck { get; set; } = new List<(string, int)>();
        /// <summary>
        /// 判断是否满足解锁条件 (不包括花钱)
        /// </summary>
        /// <param name="save">游戏存档</param>
        /// <returns>是否满足解锁条件</returns>
        public bool Check(GameSave_v2 save)
        {
            if (None)
            {
                return true;
            }
            if (Lock)
            {
                return false;
            }

            //先判断基础的           
            if (LevelMax > save.GameSave.LevelMax)
                return false;
            if (save.GameSave.LevelMax != 0 && Level > save.GameSave.Level)
                return false;
            if (Money > save.GameSave.Money)
                return false;
            if (Likability > save.GameSave.Likability)
                return false;
            if (Feeling > save.GameSave.Feeling)
                return false;
            DateTime now = DateTime.Now;
            if (Date != null)
            {
                var date = new DateTime(now.Year, Date.Value.Month, Date.Value.Day);
                if (!CheckDate(date)) return false;
            }
            if (Time != null)
            {
                if (!GalleryUnlockRules.InTimeWindow(Time.Value, now, TimeOffset))
                {
                    return false;
                }
            }
            if (Holiday != HolidayType.None)
            {
                //节日日期表在共享后端里, 农历那几个也是
                if (Holiday == HolidayType.Player_Birthday)
                {
                    if (!GalleryUnlockRules.IsBirthday(save.GetDateTime("HostBDay"), now))
                        return false;
                }
                else
                {
                    var holidayDate = GalleryUnlockRules.HolidayDate((GalleryUnlockRules.HolidayKind)(int)Holiday, now);
                    if (holidayDate != null && !CheckDate(holidayDate.Value))
                        return false;
                }
            }
            //统计数据检查
            foreach (var (stat, value) in StatCheck)
            {
                var statvalue = save.Statistics!.GetInt(stat, GalleryUnlockRules.MissingStatValue);
                if (!GalleryUnlockRules.StatSatisfied(statvalue, value))
                    return false;
            }
            return true;
        }
        public string CheckReason(GameSave_v2 gamesave)
        {
            if (None) return string.Empty;
            if (Lock)
            {
                return LockString.Translate();
            }
            StringBuilder sb = new StringBuilder();

            //if (SellPrice > 0)
            //    if (SellBoth)
            //        sb.AppendLine("花费${0} 并 满足以下条件:".Translate(SellPrice));
            //    else
            //        sb.AppendLine("花费${0} 或 满足以下条件:".Translate(SellPrice));

            //基础条件

            if (LevelMax > 0)
                if (gamesave.GameSave.LevelMax == 0)//玩家不知道LevelMax, 用通俗易懂的方式解释下
                    sb.AppendLine("等级要求: {0}".Translate(1000 + (LevelMax - 1) * 100));
                else
                {
                    sb.AppendLine("等级突破要求: {0}".Translate(LevelMax));
                    if (Level > 0)
                        sb.AppendLine("等级要求: {0}".Translate(Level));
                }

            else if (Level > 0)
                sb.AppendLine("等级要求: {0}".Translate(Level));
            if (Money > 0)
                sb.AppendLine("金钱要求: ${0}".Translate(Money));
            if (Likability > 0)
                sb.AppendLine("好感度要求: {0}".Translate(Likability));
            if (Feeling > 0)
                sb.AppendLine("心情要求: {0}".Translate(Feeling));
            if (Date != null)
                sb.AppendLine("解锁日期: {0}".Translate(Date.Value.ToString("MM-dd")));
            if (Time != null)
                sb.AppendLine("解锁时间: {0}".Translate(Time.Value.ToString("HH:mm")));
            if (Holiday != HolidayType.None)
                sb.AppendLine("解锁节日: {0}".Translate(Holiday.ToString().Translate()));

            //统计数据
            foreach (var (stat, value) in StatCheck)
            {
                sb.AppendLine("{0}要求: {1}".Translate(
                   (stat.StartsWith("stat_") ? stat.Substring(5) : stat)
                    .Translate(), value));
            }

            return sb.ToString().Trim('\n', '\r');
        }
        /// <summary>
        /// 需求等级
        /// </summary>
        public int Level { get; set; } = 0;
        /// <summary>
        /// 需求突破次数
        /// </summary>
        public int LevelMax { get; set; } = 0;
        /// <summary>
        /// 需求金钱(数量/并非消耗)
        /// </summary>
        public int Money { get; set; } = int.MinValue;
        /// <summary>
        /// 需求好感度
        /// </summary>
        public int Likability { get; set; } = 0;
        /// <summary>
        /// 需求心情
        /// </summary>
        public int Feeling { get; set; } = 0;
        /// <summary>
        /// 解锁需求日期
        /// </summary>
        public DateOnly? Date { get; set; } = null;
        /// <summary>
        /// 解锁需求时间
        /// </summary>
        public TimeOnly? Time { get; set; } = null;
        /// <summary>
        /// 日期偏移容错(天)
        /// </summary>
        public int DateOffset { get; set; } = GalleryUnlockRules.DefaultDateOffset;
        /// <summary>
        /// 时间偏移容错(分钟)
        /// </summary>
        public int TimeOffset { get; set; } = GalleryUnlockRules.DefaultTimeOffset;
        /// <summary>
        /// 节假日解锁
        /// </summary>
        public enum HolidayType
        {
            /// <summary>
            /// 不启用
            /// </summary>
            None,
            /// <summary>
            /// 中秋
            /// </summary>
            Mid_Autumn_Festival,
            /// <summary>
            /// 端午
            /// </summary>
            Dragon_Boat_Festival,
            /// <summary>
            /// 新年
            /// </summary>
            New_Years_Day,
            /// <summary>
            /// 春节
            /// </summary>
            Spring_Festival,
            /// <summary>
            /// 圣诞
            /// </summary>
            Christmas,
            /// <summary>
            /// 生日(玩家)
            /// </summary>
            Player_Birthday,
            /// <summary>
            /// 七夕
            /// </summary>
            Qixi_Festival
        }
        /// <summary>
        /// 节假日
        /// </summary>
        public HolidayType Holiday { get; set; } = HolidayType.None;

        /// <summary>
        /// 检查日期是否符合
        /// </summary>
        public bool CheckDate(DateTime date)
            => GalleryUnlockRules.InDateWindow(date, DateTime.Now, DateOffset);
        /// <summary>
        /// 检查农历日期偏差
        /// </summary>
        public static DateTime GetLunarDate(int month, int day)
            => GalleryUnlockRules.LunarDate(month, day);
    }
    /// <summary>
    /// 解锁条件
    /// </summary>
    public UnlockCondition UnlockAble { get; set; } = new UnlockCondition();

    /// <summary>
    /// 玩家数据
    /// </summary>
    public class Info
    {
        private ISub sub;
        public Info(ISub sub) { this.sub = sub; }
        public DateTime UnlockTime
        {
            get => sub.Infos[(gdat)"time"];
            set => sub.Infos[(gdat)"time"] = value;
        }
        public bool Star
        {
            get => sub.Infos[(gbol)"star"];
            set => sub.Infos[(gbol)"star"] = value;
        }
    }
    /// <summary>
    /// 玩家数据
    /// </summary>
    public Info? PlayerInfo { get; set; } = null;
    /// <summary>
    /// 是否收藏
    /// </summary>
    public bool IsStar
    {
        get => PlayerInfo?.Star ?? false;
        set { if (PlayerInfo != null) PlayerInfo.Star = value; }
    }
    /// <summary>
    /// 是否解锁
    /// </summary>
    public bool IsUnlock => PlayerInfo != null;

    /// <summary>
    /// 保存到文件夹时自动转换
    /// </summary>
    /// <param name="savedir">文件夹</param>
    public string FilePath(string savedir) => savedir + '\\' + FilePath();
    /// <summary>
    /// 保存到文件夹时自动转换
    /// </summary>
    public string FilePath()
    {
        var filepath = TranslateName;
        // 不允许的符号列表
        char[] illegalChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
        foreach (char c in illegalChars)
        {
            filepath = filepath.Replace(c.ToString(), "");
        }
        return filepath.Replace(' ', '_') + '.' + Path.Split('.').Last();
    }
}

