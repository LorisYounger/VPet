using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VPet_Simulator.Windows.Interface;
public class Photo
{
    public Photo() { }

    public Photo(Line line)
    {
        Zip = line[(gstr)"zip"];
        Path = line[(gstr)"path"];
        if (Enum.TryParse<PhotoType>(line[(gstr)"type"], true, out var tp))
            Type = tp;
        Name = line[(gstr)"name"];
        Description = line[(gstr)"desc"];
        var tags = line.Find("tags");
        if (tags != null)
            Tags = tags.GetInfos().ToList();

        UnlockAble = new UnlockCondition(line);
    }
    /// <summary>
    /// 图片所在ZIP
    /// </summary>
    public string Zip { get; set; }
    /// <summary>
    /// 图片所在位置
    /// </summary>
    public string Path { get; set; }

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
    public string Name { get; set; }
    private string transname = null;
    /// <summary>
    /// 图片名字 (翻译)
    /// </summary>
    public string TranslateName
    {
        get
        {
            if (transname == null)
            {
                transname = LocalizeCore.Translate(Name);
            }
            return transname;
        }
    }
    /// <summary>
    /// 标签
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();
    private List<string> tagstrans = null;
    /// <summary>
    /// 标签 (翻译)
    /// </summary>
    public List<string> TagsTrans
    {
        get
        {
            if (tagstrans == null)
            {
                tagstrans = Tags.Select(x => LocalizeCore.Translate(x)).ToList();
            }
            return tagstrans;
        }
    }
    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

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
                Date = DateOnly.Parse(sub.Info);
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
            if (Level > save.GameSave.Level)
                return false;
            if (LevelMax > save.GameSave.LevelMax)
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
                if (CheckDate(date)) return false;
            }
            if (Time != null)
            {
                var time = new DateTime(now.Year, now.Month, now.Day, Time.Value.Hour, Time.Value.Minute, Time.Value.Second);
                if (time > now || time.AddMinutes(TimeOffset) < now)
                {
                    return false;
                }
            }
            if (Holiday != HolidayType.None)
            {
                switch (Holiday)
                {
                    case HolidayType.Mid_Autumn_Festival:
                        if (CheckDate(GetLunarDate(8, 15)))
                            return false;
                        break;
                    case HolidayType.Dragon_Boat_Festival:
                        if (CheckDate(GetLunarDate(5, 5)))
                            return false;
                        break;
                    case HolidayType.New_Years_Day:
                        if (CheckDate(new DateTime(now.Year, 1, 1)))
                            return false;
                        break;
                    case HolidayType.Spring_Festival:
                        if (CheckDate(GetLunarDate(8, 15)))
                            return false;
                        break;
                    case HolidayType.Christmas:
                        if (CheckDate(new DateTime(now.Year, 12, 15)))
                            return false;
                        break;
                        //case HolidayType.Player_Birthday: //TODO: 玩家生日
                        //    if (now.Month != save.GameSave.Birthday.Month || now.Day != save.GameSave.Birthday.Day)
                        //        return false;
                        //    break;
                }
            }
            //统计数据检查
            foreach (var (stat, value) in StatCheck)
            {
                var statvalue = save.Statistics.GetInt(stat, -1);
                if (statvalue < value)
                    return false;
            }
            return true;
        }
        public string CheckReason(GameSave_v2 gamesave)
        {
            if (None) return string.Empty;
            if (Lock)
            {
                return LocalizeCore.Translate(LockString);
            }
            StringBuilder sb = new StringBuilder();

            if (SellPrice > 0)
                if (SellBoth)
                    sb.AppendLine("花费${0} 并 满足以下条件:".Translate(SellPrice));
                else
                    sb.AppendLine("花费${0} 或 满足以下条件:".Translate(SellPrice));

            //基础条件
            if (Level > 0)
                sb.AppendLine("等级要求: {0}".Translate(Level));
            if (LevelMax > 0)
                sb.AppendLine("等级突破要求: {0}".Translate(LevelMax));
            if (Money > 0)
                sb.AppendLine("金钱要求: ${0}".Translate(Money));
            if (Likability > 0)
                sb.AppendLine("好感度要求: {0}".Translate(Likability));
            if (Feeling > 0)
                sb.AppendLine("心情要求: {0}".Translate(Feeling));
            if (Date != null)
                sb.AppendLine("解锁日期: {0}".Translate(Date.Value.ToString("yyyy-MM-dd")));
            if (Time != null)
                sb.AppendLine("解锁时间: {0}".Translate(Time.Value.ToString("HH:mm")));
            if (Holiday != HolidayType.None)
                sb.AppendLine("解锁节日: {0}".Translate(Holiday.ToString().Translate()));

            //统计数据
            foreach (var (stat, value) in StatCheck)
            {
                var statvalue = gamesave.Statistics.GetInt(stat, -1);
                if (statvalue < value)
                    sb.AppendLine("{0}要求: {1}".Translate(stat.Translate(), value));
            }

            return sb.ToString();
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
        public int Money { get; set; } = 0;
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
        public int DateOffset { get; set; } = 2;
        /// <summary>
        /// 时间偏移容错(分钟)
        /// </summary>
        public int TimeOffset { get; set; } = 60;
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
        }
        /// <summary>
        /// 节假日
        /// </summary>
        public HolidayType Holiday { get; set; } = HolidayType.None;

        /// <summary>
        /// 检查日期是否符合
        /// </summary>
        public bool CheckDate(DateTime date)
        {
            var now = DateTime.Now;
            return date < now && date.AddDays(DateOffset) > now;
        }
        /// <summary>
        /// 检查农历日期偏差
        /// </summary>
        public static DateTime GetLunarDate(int month, int day)
        {
            ChineseLunisolarCalendar lunarCalendar = new ChineseLunisolarCalendar();
            DateTime lunarDate = lunarCalendar.ToDateTime(DateTime.Now.Year, month, day, 0, 0, 0, 0);
            return lunarDate;
        }
    }
    /// <summary>
    /// 解锁条件
    /// </summary>
    public UnlockCondition UnlockAble { get; set; }

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
    public Info PlayerInfo { get; set; } = null;
    /// <summary>
    /// 是否收藏
    /// </summary>
    public bool IsStar => PlayerInfo?.Star ?? false;
    /// <summary>
    /// 是否解锁
    /// </summary>
    public bool IsUnlock => PlayerInfo != null;
    /// <summary>
    /// 解锁这张图片
    /// </summary>
    public void Unlock(IMainWindow imw)
    {
        ISub sub = imw.Set["betterbuy"][Name];
        PlayerInfo = new Info(new Sub());
        PlayerInfo.UnlockTime = DateTime.Now;
    }

    /// <summary>
    /// 创建缩略图 (以最小的为准)
    /// </summary>   
    /// <param name="originalImage">原图</param>
    /// <param name="width">长度</param>
    /// <param name="height">高度</param>
    /// <returns></returns>
    public static BitmapSource ConvertToThumbnail(BitmapImage originalImage, int width, int height)
    {
        // 创建一个 RenderTargetBitmap
        RenderTargetBitmap renderBitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
        DrawingVisual visual = new DrawingVisual();

        using (DrawingContext drawingContext = visual.RenderOpen())
        {
            // 计算缩放比例
            double scaleX = (double)width / originalImage.PixelWidth;
            double scaleY = (double)height / originalImage.PixelHeight;
            double scale = Math.Min(scaleX, scaleY); // 选择较小的比例以保持纵横比

            // 计算缩放后的尺寸
            double scaledWidth = originalImage.PixelWidth * scale;
            double scaledHeight = originalImage.PixelHeight * scale;

            // 绘制图像
            drawingContext.DrawImage(originalImage, new Rect(0, 0, scaledWidth, scaledHeight));
        }

        renderBitmap.Render(visual);
        return renderBitmap;
    }
    /// <summary>
    /// 创建灰度图 (未解锁)
    /// </summary>
    /// <param name="originalImage">原图</param>
    /// <returns></returns>
    private BitmapSource ConvertToGrayScale(BitmapSource originalImage)
    {
        // 创建 WriteableBitmap
        WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);
        int width = writeableBitmap.PixelWidth;
        int height = writeableBitmap.PixelHeight;

        // 获取像素数据
        int[] pixels = new int[width * height];
        writeableBitmap.CopyPixels(pixels, width * 4, 0);

        // 转换为灰度
        for (int i = 0; i < pixels.Length; i++)
        {
            // 获取 ARGB 颜色
            byte a = (byte)((pixels[i] >> 24) & 0xff); // Alpha
            byte r = (byte)((pixels[i] >> 16) & 0xff); // Red
            byte g = (byte)((pixels[i] >> 8) & 0xff);  // Green
            byte b = (byte)(pixels[i] & 0xff);         // Blue

            // 计算灰度值
            byte gray = (byte)((r + g + b) / 3); // 可以使用其他公式来计算灰度

            // 设置新的灰度像素值
            pixels[i] = (a << 24) | (gray << 16) | (gray << 8) | gray; // ARGB
        }

        // 创建新的 WriteableBitmap
        WriteableBitmap grayBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
        grayBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);

        return grayBitmap;
    }
    /// <summary>
    /// 获得当前图片图片
    /// </summary>
    public BitmapImage GetImage(IMainWindow imw)
    {
        //解压zip
        string zippath = imw.FileSources.FindSource(Zip);
        if (zippath == null)
        {
            return ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/img/error.png");
        }
        using (ZipArchive archive = ZipFile.OpenRead(zippath))
        {
            // 找到指定的文件
            ZipArchiveEntry entry = archive.GetEntry(Path);
            if (entry != null)
            {
                using (Stream stream = entry.Open())
                {
                    // 创建 BitmapImage
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // 立即加载
                    bitmap.EndInit();
                    bitmap.Freeze(); // 使 BitmapImage 可以在不同线程中使用
                    return bitmap;
                }
            }
            else
            {
                return ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/img/error.png");
            }
        }
    }

}

