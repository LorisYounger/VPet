using LinePutScript;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.MutiPlatform.GraphHelper;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 桌宠数值的包装
/// </summary>
/// 每次都重新取: 读档时 GameSave_v2 会被整个换掉, 缓存住就指向老对象了
internal sealed class HostSaveView : IPetSave
{
    private readonly Func<IGameSave> resolve;
    internal HostSaveView(Func<IGameSave> resolve) { this.resolve = resolve; }
    private IGameSave S => resolve();

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
internal sealed class HostStatisticsView : IPetStatistics
{
    private readonly Func<Statistics> resolve;
    internal HostStatisticsView(Func<Statistics> resolve) { this.resolve = resolve; }
    private Statistics S => resolve();

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

    public event Action<string>? Changed
    {
        add
        {
            if (value != null)
                S.StatisticChanged += (sender, name, obj) => value(name);
        }
        // 与 Windows 侧一致: 包装之后没法精确摘掉某一个, 宿主自己也从不退订
        remove { }
    }
}

/// <summary>
/// 统一契约的物品注册表 (跨平台侧)
/// </summary>
/// 直接架在 ItemStore 上, 没有中间层
internal sealed class UnifiedItemRegistry : IItemRegistry
{
    private readonly PetWindow window;
    private Action<IFoodInfo>? takeItem;

    internal UnifiedItemRegistry(PetWindow window)
    {
        this.window = window;
        window.HostFoodTaken += food => takeItem?.Invoke(food);
    }

    private ItemStore Store => window.HostItems;

    public IReadOnlyList<IItemInfo> All => Store.Items.Cast<IItemInfo>().ToList();
    public void Add(UnifiedItem item) => window.HostAddItem(item);
    public void RegisterCreator(string itemType, Func<ILine, UnifiedItem?> creator)
        => Store.RegisterCreator(itemType, creator);
    public void RegisterUseAction(string itemType, Func<IItemInfo, bool> action)
        => Store.RegisterUseAction(itemType, action);
    public void Take(IFoodInfo food) => window.HostTakeFood(food);
    public void TakeHandle(IFoodInfo food, int count, string from) => window.HostTakeFoodHandle(food, count, from);

    public event Action<IFoodInfo>? TakeItem { add => takeItem += value; remove => takeItem -= value; }

    public DateTime LastTakeItemTime => window.HostLastTakeItemTime;
}

/// <summary>
/// 统一契约的桌宠视图 (跨平台侧)
/// </summary>
/// 事件要等 PetMain 建好才能挂 —— 插件构造期它还是 null
internal sealed class UnifiedPetView : IPetView
{
    private readonly PetWindow window;
    private PetMain M => window.HostPet!;

    private Action? touchHead;
    private Action? touchBody;
    private Action<string>? onSay;
    private Action<IWorkInfo>? workStart;
    private Action<IWorkInfo, double, double>? workEnd;
    private Action? tick;

    internal UnifiedPetView(PetWindow window) { this.window = window; }

    internal void Attach()
    {
        if (window.HostPet == null)
            return;
        M.Event_TouchHead += OnTouchHead;
        M.Event_TouchBody += OnTouchBody;
        M.SayProcess.Add(OnSayRaised);
        M.Event_WorkStart += OnWorkStart;
        M.Event_WorkEnd += OnWorkEnd;
        M.TimeHandle += OnTick;
    }

    internal void Detach()
    {
        if (window.HostPet == null)
            return;
        M.Event_TouchHead -= OnTouchHead;
        M.Event_TouchBody -= OnTouchBody;
        M.SayProcess.Remove(OnSayRaised);
        M.Event_WorkStart -= OnWorkStart;
        M.Event_WorkEnd -= OnWorkEnd;
        M.TimeHandle -= OnTick;
    }

    private void OnTouchHead() => touchHead?.Invoke();
    private void OnTouchBody() => touchBody?.Invoke();
    //流式说话给的是还没生成完的对象, GetSayText 会等它生成完再给全文
    private async void OnSayRaised(SayInfo info)
    {
        var handler = onSay;
        if (handler == null)
            return;
        try { handler(await info.GetSayText()); }
        catch (Exception e) { PetWindow.Log(e.ToString()); }
    }
    private void OnWorkStart(Work work) => workStart?.Invoke(new HostWorkView(work));
    private void OnWorkEnd(WorkTimer.FinishWorkInfo info)
        => workEnd?.Invoke(new HostWorkView(info.work), info.count, info.spendtime);
    private void OnTick(PetMain _) => tick?.Invoke();

    public PetWorkingState State
    {
        get => (PetWorkingState)(int)M.State;
        set => M.State = (PetMain.WorkingState)(int)value;
    }
    public IWorkInfo? NowWork => M.NowWork == null ? null : new HostWorkView(M.NowWork);
    public DateTime LastInteractionTime { get => M.LastInteractionTime; set => M.LastInteractionTime = value; }

    public void Display(string name, PetAnimatType animat, Action? endAction = null)
        => M.Display(name, (GraphInfo.AnimatType)(int)animat, endAction);
    public void Display(PetGraphType type, PetAnimatType animat, Action? endAction = null)
        => M.Display((GraphInfo.GraphType)(int)type, (GraphInfo.AnimatType)(int)animat, endAction);
    public void DisplayToNomal() => M.DisplayToNomal();
    public void DisplayFoodAnimation(string graphName, string imagePath)
        => window.HostDisplayFoodAnimation(graphName, imagePath);

    public void Say(string text, string? graphName = null, bool force = false, string? desc = null)
        => M.Say(text, graphName, force, desc);
    public void SayRnd(string text, bool force = false, string? desc = null)
        => M.SayRnd(text, force, desc);
    public void LabelDisplayShow(string text, int time = 2000) => M.LabelDisplayShow(text, time);

    public event Action? TouchHead { add => touchHead += value; remove => touchHead -= value; }
    public event Action? TouchBody { add => touchBody += value; remove => touchBody -= value; }
    public event Action<string>? OnSay { add => onSay += value; remove => onSay -= value; }
    public event Action<IWorkInfo>? WorkStart { add => workStart += value; remove => workStart -= value; }
    public event Action<IWorkInfo, double, double>? WorkEnd { add => workEnd += value; remove => workEnd -= value; }
    public event Action? Tick { add => tick += value; remove => tick -= value; }

    public void PlayVoice(string path) => window.HostPlayVoice(path);
    public double VoiceVolume { get => window.HostVoiceVolume; set => window.HostVoiceVolume = value; }
    public bool PlayingVoice => window.HostPlayingVoice;
}

/// <summary>
/// 一份工作的包装
/// </summary>
internal sealed class HostWorkView : IWorkInfo
{
    private readonly Work target;
    internal HostWorkView(Work target) { this.target = target; }

    public string Name => target.Name;
    public string TranslateName => target.NameTrans;
    public string Type => target.Type.ToString();
    public double MoneyBase => target.MoneyBase;
    public int Time => target.Time;
    public int LevelLimit => target.LevelLimit;
}
