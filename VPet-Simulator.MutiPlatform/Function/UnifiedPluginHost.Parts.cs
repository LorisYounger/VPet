//跨平台: 原文复制自 VPet-Simulator.Windows/Function/UnifiedPluginHost.Parts.cs; BitmapImage 换成 Avalonia 的 Bitmap
using LinePutScript;
using LinePutScript.Converter;
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
/// 统一契约的物品注册表 (Windows 侧)
/// </summary>
/// 这里解决了 Windows 版原有的一个时序坑: Item.Creators 的文档说在 LoadPlugin 里
/// 注册, 而存档反序列化(MainWindow.cs:976)其实跑在 LoadPlugin(:1937)之前, 那时还
/// 没有创建器, MOD 的自定义物品会被静默降级成普通 Item —— 数据不丢, 但类型不对,
/// 使用处理器也就找不到它了.
///
/// 统一契约不复制这个行为: 存档里认不出主人的物品行先挂起, 谁注册了对应类型就当场
/// 把它们补出来. 所以在构造函数里注册(推荐)和在 LoadPlugin 里注册都能拿到东西.
internal sealed class UnifiedItemRegistry : IItemRegistry
{
    private readonly UnifiedPluginHost host;
    private readonly MainWindow mw;
    private readonly Dictionary<string, Func<ILine, UnifiedItem?>> creators
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 内置四种物品类型的使用处理器要等宿主先注册完
    /// </summary>
    /// MainWindow.xaml.cs:346-426 用的是 Dictionary.Add, 重复键会抛异常.
    /// 插件构造函数跑在那之前, 所以先排队, 等宿主注册完再冲刷.
    private static readonly List<(string Type, Func<IMainWindow, Item, bool> Action)> pendingUseActions = new();
    private static bool useActionsReady;

    internal UnifiedItemRegistry(UnifiedPluginHost host, MainWindow mw)
    {
        this.host = host;
        this.mw = mw;
    }

    public IReadOnlyList<IItemInfo> All => mw.Items.Select(UnifiedWrap.Of).ToList();

    public void Add(UnifiedItem item)
    {
        var native = Materialize(item);
        host.RunOnUI(() => native.LoadSource(mw));
        mw.ItemsAdd(native);
    }

    public void RegisterCreator(string itemType, Func<ILine, UnifiedItem?> creator)
    {
        creators[itemType] = creator;
        // 用索引器而不是 Add: 与现有 Windows MOD 的做法一致, 允许覆盖
        Item.Creators[itemType] = (imw, line) =>
        {
            var made = creator(line);
            return made == null ? null : Materialize(made);
        };
        // 已经读过档了(比如插件是在 LoadPlugin 里才注册的), 把当时认不出主人的
        // 那些物品原地换成正确的类型
        if (host.Loaded)
            Rematerialize(itemType);
    }

    public void RegisterUseAction(string itemType, Func<IItemInfo, bool> action)
    {
        //Item.UseAction 是进程级静态的, 多开时每个窗口的插件实例都会往同一个列表里挂.
        //不认窗口的话, 一件物品会被所有窗口的实例各处理一遍. 这里只认自己那个窗口,
        //别人的交回去让列表里下一个处理器接.
        bool Wrapped(IMainWindow imw, Item item)
            => ReferenceEquals(imw, mw) && action(UnifiedWrap.Of(item));

        if (Item.UseAction.TryGetValue(itemType, out var list))
        {
            list.Add(Wrapped);
            return;
        }
        // 内置四种类型的列表要等宿主建好, 否则会和它的 Dictionary.Add 撞车
        if (!useActionsReady && Item.ItemTypes.Contains(itemType))
        {
            pendingUseActions.Add((itemType, Wrapped));
            return;
        }
        Item.UseAction[itemType] = new List<Func<IMainWindow, Item, bool>> { Wrapped };
    }

    /// <summary>
    /// 宿主的内置使用处理器已经注册完了
    /// </summary>
    /// 由 MainWindow.xaml.cs 在建完内置四种之后调一次
    internal static void UseActionsReady()
    {
        useActionsReady = true;
        foreach (var (type, action) in pendingUseActions)
        {
            if (Item.UseAction.TryGetValue(type, out var list))
                list.Add(action);
            else
                Item.UseAction[type] = new List<Func<IMainWindow, Item, bool>> { action };
        }
        pendingUseActions.Clear();
    }

    /// <summary>
    /// 把存档里挂起的物品行补出来
    /// </summary>
    internal void FlushPending()
    {
        foreach (var type in creators.Keys.ToList())
            Rematerialize(type);
    }

    /// <summary>
    /// 把降级成普通 Item 的那些原地换回正确的类型
    /// </summary>
    /// 只碰本插件自己注册的那个 itemType —— 那个类型在此之前没有主人, 不可能有别人
    /// 持有它的引用.
    private void Rematerialize(string itemType)
    {
        if (!creators.TryGetValue(itemType, out var creator))
            return;
        for (int i = 0; i < mw.Items.Count; i++)
        {
            var item = mw.Items[i];
            // 只换"类型名对得上但对象是普通 Item"的, 已经是正确类型的不动
            if (item.GetType() != typeof(Item) || !string.Equals(item.ItemType, itemType, StringComparison.OrdinalIgnoreCase))
                continue;
            var line = LPSConvert.SerializeObjectToLine<Line>(item, "item");
            var made = creator(line);
            if (made == null)
                continue;
            var native = Materialize(made);
            host.RunOnUI(() => native.LoadSource(mw));
            mw.Items[i] = native;
        }
    }

    /// <summary>
    /// 把契约里的 DTO 变成 Windows 版的真物品
    /// </summary>
    private static Item Materialize(UnifiedItem item) => new Item
    {
        Name = item.Name,
        ItemType = item.ItemType,
        Desc = item.Desc,
        Price = item.Price,
        Count = item.Count,
        Data = item.Data,
        CanUse = item.CanUse,
        Star = item.Star,
        IsSingle = item.IsSingle,
        Visibility = item.Visibility,
        Image = item.Image,
    };

    public void Take(IFoodInfo food)
    {
        var native = UnifiedWrap.Unwrap(food);
        if (native != null)
            mw.TakeItem(native);
    }

    public void TakeHandle(IFoodInfo food, int count, string from)
    {
        var native = UnifiedWrap.Unwrap(food);
        if (native != null)
            mw.TakeItemHandle(native, count, from);
    }

    public event Action<IFoodInfo>? TakeItem
    {
        add { if (value != null) mw.Event_TakeItem += food => value(UnifiedWrap.Of(food)); }
        // Windows 版自己也从不退订 Event_TakeItem; 包装之后没法精确摘掉某一个
        remove { }
    }

    public DateTime LastTakeItemTime => mw.LastTakeItemTime;
}

/// <summary>
/// 统一契约的桌宠视图 (Windows 侧)
/// </summary>
/// 事件要等 Main 建好才能挂 —— 插件构造期它还是 null.
internal sealed class UnifiedPetView : IPetView
{
    private readonly MainWindow mw;
    private Main M => mw.Main;

    private Action? touchHead;
    private Action? touchBody;
    private Action<string>? onSay;
    private Action<IWorkInfo>? workStart;
    private Action<IWorkInfo, double, double>? workEnd;
    private Action? tick;

    internal UnifiedPetView(MainWindow mw) { this.mw = mw; }

    /// <summary>
    /// 把事件挂到 Main 上
    /// </summary>
    internal void Attach()
    {
        M.Event_TouchHead += OnTouchHead;
        M.Event_TouchBody += OnTouchBody;
        M.SayProcess.Add(OnSayRaised);
        M.Event_WorkStart += OnWorkStart;
        M.Event_WorkEnd += OnWorkEnd;
        M.EventTimer.Elapsed += OnTick;
    }

    internal void Detach()
    {
        if (mw.Main == null)
            return;
        M.Event_TouchHead -= OnTouchHead;
        M.Event_TouchBody -= OnTouchBody;
        M.SayProcess.Remove(OnSayRaised);
        M.Event_WorkStart -= OnWorkStart;
        M.Event_WorkEnd -= OnWorkEnd;
        M.EventTimer.Elapsed -= OnTick;
    }

    private void OnTouchHead() => touchHead?.Invoke();
    private void OnTouchBody() => touchBody?.Invoke();
    //SayProcess 里流式说话给的是还没生成完的对象, GetSayText 会等它生成完再给全文
    private async void OnSayRaised(SayInfo info)
    {
        var handler = onSay;
        if (handler == null)
            return;
        try { handler(await info.GetSayText()); }
        catch (Exception e) { Console.WriteLine(e); }
    }
    private void OnWorkStart(Work work) => workStart?.Invoke(UnifiedWrap.Of(work));
    private void OnWorkEnd(WorkTimer.FinishWorkInfo info)
        => workEnd?.Invoke(UnifiedWrap.Of(info.work), info.count, info.spendtime);
    private void OnTick(object? sender, System.Timers.ElapsedEventArgs e) => tick?.Invoke();

    public PetWorkingState State
    {
        get => (PetWorkingState)(int)M.State;
        set => M.State = (Main.WorkingState)(int)value;
    }
    public IWorkInfo? NowWork => M.NowWork == null ? null : UnifiedWrap.Of(M.NowWork);
    public DateTime LastInteractionTime { get => M.LastInteractionTime; set => M.LastInteractionTime = value; }

    public void Display(string name, PetAnimatType animat, Action? endAction = null)
        => M.Display(name, (GraphInfo.AnimatType)(int)animat, endAction);
    public void Display(PetGraphType type, PetAnimatType animat, Action? endAction = null)
        => M.Display((GraphInfo.GraphType)(int)type, (GraphInfo.AnimatType)(int)animat, endAction);
    public void DisplayToNomal() => M.DisplayToNomal();
    public void DisplayFoodAnimation(string graphName, string imagePath)
        => mw.Dispatcher.Invoke(() => mw.DisplayFoodAnimation(graphName,
            ImageResources.NewSafeBitmapImage(imagePath)));

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

    public void PlayVoice(string path) => M.PlayVoice(path);
    public double VoiceVolume { get => M.PlayVoiceVolume; set => M.PlayVoiceVolume = value; }
    public bool PlayingVoice => M.PlayingVoice;
}
