//跨平台: 原文复制自 VPet-Simulator.Windows/Function/UnifiedPluginHost.Wrappers.cs; ImageSource/BitmapImage 换成 Avalonia 的 Bitmap
using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.MutiPlatform.GraphHelper;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 统一契约的包装器
/// </summary>
/// 把 Windows 版的真对象包成契约里的接口. 关键是**实时投影**而不是快照:
/// 宿主在 MOD 加载之后还会改这些对象(价格钳制、收藏状态、吃腻度), MOD 读到的必须是
/// 当前值, MOD 改的也必须传得回去.
///
/// 包装器保留对象身份；物品和照片的操作还绑定所属窗口，读档后也不转交给其他窗口。
internal static class UnifiedWrap
{
    private static readonly ConditionalWeakTable<Food, FoodView> foods = new();
    private static readonly ConditionalWeakTable<Work, WorkView> works = new();

    public static IFoodInfo Of(Food food) => foods.GetValue(food, x => new FoodView(x));
    public static IItemInfo Of(Item item, MainWindow mw) => mw.UnifiedItems.GetValue(item, x => new ItemView(x, mw));
    public static IPhotoInfo Of(Photo photo, MainWindow mw) => mw.UnifiedPhotos.GetValue(photo, x => new PhotoView(x, mw));
    public static IWorkInfo Of(Work work) => works.GetValue(work, x => new WorkView(x));

    /// <summary>
    /// 从契约接口取回底下那个真对象
    /// </summary>
    /// 拿不回来(比如 MOD 自己实现了接口)时返回 null, 调用方得自己兜底
    public static Food? Unwrap(IFoodInfo food) => (food as FoodView)?.Target;

    private sealed class FoodView : IFoodInfo
    {
        public Food Target { get; }
        public FoodView(Food target) { Target = target; }

        public string Name => Target.Name;
        public string TranslateName => Target.TranslateName;
        public PetFoodType Type { get => (PetFoodType)(int)Target.Type; set => Target.Type = (Food.FoodType)(int)value; }
        public int Exp { get => Target.Exp; set => Target.Exp = value; }
        public double Strength { get => Target.Strength; set => Target.Strength = value; }
        public double StrengthFood { get => Target.StrengthFood; set => Target.StrengthFood = value; }
        public double StrengthDrink { get => Target.StrengthDrink; set => Target.StrengthDrink = value; }
        public double Feeling { get => Target.Feeling; set => Target.Feeling = value; }
        public double Health { get => Target.Health; set => Target.Health = value; }
        public double Likability { get => Target.Likability; set => Target.Likability = value; }
        public double Price { get => Target.Price; set => Target.Price = value; }
        public string Desc { get => Target.Desc; set => Target.Desc = value; }
        public string? Graph { get => Target.Graph; set => Target.Graph = value; }
        public string? Image => Target.Image;
        // Windows 版的图片是解码好的 Avalonia.Media.Imaging.Bitmap, 契约只给路径 —— 从它的 UriSource 取
        public string? ImagePath => Target.ImagePath;
        public bool Star { get => Target.Star; set => Target.Star = value; }
        public string GetGraph() => Target.GetGraph();
        public double RealPrice => Target.RealPrice;
        public bool IsOverLoad() => Target.IsOverLoad();
        public ILine ToLine() => LPSConvert.SerializeObjectToLine<Line>(Target, "food");
    }

    private sealed class ItemView : IItemInfo
    {
        private readonly Item target;
        private readonly MainWindow mw;
        public ItemView(Item target, MainWindow mw) { this.target = target; this.mw = mw; }

        public Item Target => target;
        public string Name => target.Name;
        public string TranslateName => target.TranslateName;
        public string ItemType { get => target.ItemType; set => target.ItemType = value; }
        public string Desc { get => target.Desc; set => target.Desc = value; }
        public string Description => target.Description;
        public double Price { get => target.Price; set => target.Price = value; }
        public int Count { get => target.Count; set => target.Count = value; }
        public string Data { get => target.Data; set => target.Data = value; }
        public bool CanUse { get => target.CanUse; set => target.CanUse = value; }
        public bool Star { get => target.Star; set => target.Star = value; }
        public bool IsSingle { get => target.IsSingle; set => target.IsSingle = value; }
        public bool Visibility { get => target.Visibility; set => target.Visibility = value; }
        public string? Image => target.Image;
        public string? ImagePath => target.ImagePath;
        public IFoodInfo? AsFood => target is Food food ? Of(food) : null;
        public void Use() => target.Use(mw);
        public void Consume(int count = 1) => target.Consume(mw, count);
        public ILine ToLine() => LPSConvert.SerializeObjectToLine<Line>(target, "item");
    }

    private sealed class PhotoView : IPhotoInfo
    {
        private readonly Photo target;
        private readonly MainWindow mw;
        public PhotoView(Photo target, MainWindow mw) { this.target = target; this.mw = mw; }

        public string Name => target.Name;
        public string TranslateName => target.TranslateName;
        public string Description => target.Description;
        public string Tags => string.Join(",", target.Tags);
        public PetPhotoType Type => (PetPhotoType)(int)target.Type;
        public bool IsUnlock => target.IsUnlock;
        public bool IsStar { get => target.IsStar; set => target.IsStar = value; }
        public void Unlock() => target.Unlock(mw);
    }

    private sealed class WorkView : IWorkInfo
    {
        private readonly Work target;
        public WorkView(Work target) { this.target = target; }

        public string Name => target.Name;
        public string TranslateName => target.NameTrans;
        public string Type => target.Type.ToString();
        public double MoneyBase => target.MoneyBase;
        public int Time => target.Time;
        public int LevelLimit => target.LevelLimit;
    }

    /// <summary>
    /// 说话文本的包装
    /// </summary>
    /// 三种文本(LowText / ClickText / SelectText)没有共同基类里的统一入口, 所以按
    /// 种类分别包一下
    internal sealed class TextView : ITextInfo
    {
        private readonly IText target;
        public TextView(IText target, PetTextKind kind) { this.target = target; Kind = kind; }

        public PetTextKind Kind { get; }
        public string Text => target.Text;
        public string TranslateText => target.TranslateText;
        public string Tag => target.Tag;
        public ILine ToLine() => LPSConvert.SerializeObjectToLine<Line>(target, Kind.ToString().ToLowerInvariant() + "text");
    }

    /// <summary>
    /// 桌宠数值的包装
    /// </summary>
    internal sealed class SaveView : IPetSave
    {
        private readonly MainWindow mw;
        /// 每次都重新取: GameSavesData 会在读档时被整个替换掉, 缓存住就指向老对象了
        public SaveView(MainWindow mw) { this.mw = mw; }
        private IGameSave S => mw.GameSavesData.GameSave;

        public string Name { get => S.Name; set => S.Name = value; }
        public string HostName { get => S.HostName; set => S.HostName = value; }
        public double Money { get => S.Money; set => S.Money = value; }
        public double Exp { get => S.Exp; set => S.Exp = value; }
        public double ExpBonus => S.ExpBonus;
        public int Level => S.Level;
        public int LevelUpNeed() => S.LevelUpNeed();
        public double Strength { get => S.Strength; set => S.Strength = value; }
        public double StrengthMax => S.StrengthMax;
        public double StoreStrength { get => S.StoreStrength; set => S.StoreStrength = value; }
        public double ChangeStrength { get => S.ChangeStrength; set => S.ChangeStrength = value; }
        public void StrengthChange(double value) => S.StrengthChange(value);
        public double StrengthFood { get => S.StrengthFood; set => S.StrengthFood = value; }
        public double StoreStrengthFood { get => S.StoreStrengthFood; set => S.StoreStrengthFood = value; }
        public double ChangeStrengthFood { get => S.ChangeStrengthFood; set => S.ChangeStrengthFood = value; }
        public void StrengthChangeFood(double value) => S.StrengthChangeFood(value);
        public double StrengthDrink { get => S.StrengthDrink; set => S.StrengthDrink = value; }
        public double StoreStrengthDrink { get => S.StoreStrengthDrink; set => S.StoreStrengthDrink = value; }
        public double ChangeStrengthDrink { get => S.ChangeStrengthDrink; set => S.ChangeStrengthDrink = value; }
        public void StrengthChangeDrink(double value) => S.StrengthChangeDrink(value);
        public double Feeling { get => S.Feeling; set => S.Feeling = value; }
        public double FeelingMax => S.FeelingMax;
        public double ChangeFeeling { get => S.ChangeFeeling; set => S.ChangeFeeling = value; }
        public void FeelingChange(double value) => S.FeelingChange(value);
        public double Health { get => S.Health; set => S.Health = value; }
        public double Likability { get => S.Likability; set => S.Likability = value; }
        public double LikabilityMax => S.LikabilityMax;
        public void CleanChange() => S.CleanChange();
        public void StoreTake() => S.StoreTake();
        public PetModeType Mode { get => (PetModeType)(int)S.Mode; set => S.Mode = (IGameSave.ModeType)(int)value; }
        public PetModeType CalMode() => (PetModeType)(int)S.CalMode();
    }

    /// <summary>
    /// 统计数据的包装
    /// </summary>
    internal sealed class StatisticsView : IPetStatistics
    {
        private readonly MainWindow mw;
        public StatisticsView(MainWindow mw) { this.mw = mw; }
        private Statistics S => mw.GameSavesData.Statistics;

        public int GetInt(string name, int defaultValue = 0) => S.Find(name) == null ? defaultValue : S[(gint)name];
        public void SetInt(string name, int value) => S[(gint)name] = value;
        public long GetInt64(string name, long defaultValue = 0) => S.Find(name) == null ? defaultValue : S[(gi64)name];
        public void SetInt64(string name, long value) => S[(gi64)name] = value;
        public double GetDouble(string name, double defaultValue = 0) => S.Find(name) == null ? defaultValue : S[(gdbe)name];
        public void SetDouble(string name, double value) => S[(gdbe)name] = value;
        public bool GetBool(string name) => S.GetBool(name);
        public void SetBool(string name, bool value) => S.SetBool(name, value);
        public DateTime GetDateTime(string name, DateTime defaultValue = default) => S.Find(name) == null ? defaultValue : S[(gdat)name];
        public void SetDateTime(string name, DateTime value) => S[(gdat)name] = value;
        public string? GetString(string name, string? defaultValue = null) => S.Find(name) == null ? defaultValue : S[(gstr)name];
        public void SetString(string name, string? value) => S[(gstr)name] = value;
        public IEnumerable<string> Keys => S.Data.Keys.ToList();

        public event Action<string>? Changed;

        internal void OnChanged(Statistics sender, string name, SetObject? value)
            => Changed?.Invoke(name);

        internal void Detach()
        {
            S.StatisticChanged -= OnChanged;
            Changed = null;
        }
    }

    /// <summary>
    /// MOD 元信息的包装
    /// </summary>
    internal sealed class PluginInfoView : IPluginInfo
    {
        private readonly IModInfo target;
        private readonly MainWindow mw;
        public PluginInfoView(IModInfo target, MainWindow mw)
        {
            this.target = target;
            this.mw = mw;
        }

        public string Name => target.Name;
        public string Author => target.Author;
        public long AuthorID => target.AuthorID;
        public ulong ItemID => target.ItemID;
        public string Intro => target.Intro;
        public int GameVer => target.GameVer;
        public int Ver => target.Ver;
        public string Path => target.Path.FullName;
        public IReadOnlyCollection<string> Tag => target.Tag.ToList();
        public bool IsEnabled => mw.OnModInfo.Any(x => x.Name == target.Name);
    }
}
