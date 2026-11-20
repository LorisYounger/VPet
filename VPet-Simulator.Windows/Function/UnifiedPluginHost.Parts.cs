using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphHelper;

namespace VPet_Simulator.Windows;

/// <summary>
/// Windows 宿主的物品注册表
/// </summary>
/// <remarks>宿主先读取背包，再调用 LoadPlugin。在插件构造函数中注册时，创建器直接读取存档行。读档后注册时，宿主将背包中同类型的普通 Item 序列化，再交给创建器。</remarks>
internal sealed partial class UnifiedItemRegistry : IItemRegistry
{
    private readonly MainWindow mw;

    /// <summary>
    /// 等待宿主完成内置物品注册的使用处理器
    /// </summary>
    /// <remarks>宿主用 Dictionary.Add 初始化四类内置处理器。提前注册的同类型插件处理器先在这里排队，初始化后排在内置处理器之后。</remarks>
    private static readonly List<(string Type, Func<IMainWindow, Item, bool> Action)> pendingUseActions = new();
    private static bool useActionsReady;

    internal UnifiedItemRegistry(MainWindow mw)
    {
        this.mw = mw;
    }

    public IReadOnlyList<IItemInfo> All => mw.Items.Select(x => UnifiedWrap.Of(x, mw)).ToList();

    public void Add(UnifiedItem item)
    {
        var native = Materialize(item);
        LoadItemSource(native);
        mw.ItemsAdd(native);
    }

    public void RegisterUseAction(string itemType, Func<IItemInfo, bool> action)
    {
        // Item.UseAction 由整个进程共用，每个处理器只响应所属窗口的物品。
        bool Wrapped(IMainWindow imw, Item item)
            => ReferenceEquals(imw, mw) && action(UnifiedWrap.Of(item, mw));

        registeredUseActions.Add((itemType, Wrapped));
        if (Item.UseAction.TryGetValue(itemType, out var list))
        {
            list.Add(Wrapped);
            return;
        }
        if (!useActionsReady && Item.ItemTypes.Contains(itemType))
        {
            pendingUseActions.Add((itemType, Wrapped));
            return;
        }
        Item.UseAction[itemType] = new List<Func<IMainWindow, Item, bool>> { Wrapped };
    }

    /// <summary>
    /// 将排队的处理器追加到宿主已建好的列表
    /// </summary>
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

    private void LoadItemSource(Item item) => mw.Dispatcher.Invoke(() => item.LoadSource(mw));

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

    public event Action<IFoodInfo>? TakeItem;

    internal void Attach() => mw.Event_TakeItem += OnTakeItem;

    internal void Detach()
    {
        UnregisterItems();
        mw.Event_TakeItem -= OnTakeItem;
        TakeItem = null;
    }

    private void OnTakeItem(Food food) => TakeItem?.Invoke(UnifiedWrap.Of(food));

    public DateTime LastTakeItemTime => mw.LastTakeItemTime;
}

/// <summary>
/// Windows 宿主的桌宠视图
/// </summary>
/// <remarks>Main 在插件构造后才创建，宿主在调用 LoadPlugin 前订阅桌宠事件。</remarks>
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
    /// 订阅桌宠事件
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
    // 流式对话可能仍在生成，等 GetSayText 返回全文再通知插件。
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

    public void PlayVoice(string path) => M.PlayVoice(new Uri(path));
    public double VoiceVolume { get => M.PlayVoiceVolume; set => M.PlayVoiceVolume = value; }
    public bool PlayingVoice => M.PlayingVoice;
}
