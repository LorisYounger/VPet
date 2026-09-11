using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 背包
/// </summary>
/// 存档格式与 Windows 版逐字一致: 每件物品一行, 行名是 item0、item1……,
/// 子项名与 Windows 版 Item 的 [Line] 一一对应. 存档因此可以两边互拷.
///
/// 与 Windows 版的一个区别是**认不出主人的物品行不会被降级**. Windows 版里
/// Item.CreateItem 找不到对应的创建器就退回普通 Item, 类型信息当场丢失, 于是
/// 使用处理器再也找不到它(见 ABI-Compatibility.md 里那段时序坑). 这边把它们挂起,
/// 谁注册了对应类型就当场补出来.
public class ItemStore
{
    private readonly List<StoreItem> items = new List<StoreItem>();
    private readonly Dictionary<string, Func<ILine, UnifiedItem?>> creators
        = new Dictionary<string, Func<ILine, UnifiedItem?>>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Func<IItemInfo, bool>>> useActions
        = new Dictionary<string, List<Func<IItemInfo, bool>>>(StringComparer.OrdinalIgnoreCase);
    /// 存档里认不出主人的行, 等着有人来认领
    private readonly List<ILine> pending = new List<ILine>();

    /// <summary>
    /// 背包里现有的东西
    /// </summary>
    public IReadOnlyList<StoreItem> Items => items;

    /// <summary>
    /// 还没被认领的存档行数
    /// </summary>
    /// 界面上可以拿它提示"有 N 件物品来自没装的 MOD"
    public int PendingCount => pending.Count;

    /// <summary>
    /// 有东西被加进背包时触发
    /// </summary>
    public event Action<StoreItem>? Added;

    /// <summary>
    /// 注册一种自定义物品的创建方式
    /// </summary>
    /// 早注册晚注册都不丢东西: 晚注册时会把挂起的行当场补出来
    public void RegisterCreator(string itemType, Func<ILine, UnifiedItem?> creator)
    {
        creators[itemType] = creator;
        Claim(itemType);
    }

    /// <summary>
    /// 注册"用掉某类物品时干什么"
    /// </summary>
    /// 先注册的先执行, 与 Windows 版 Item.UseAction 的顺序语义一致
    public void RegisterUseAction(string itemType, Func<IItemInfo, bool> action)
    {
        if (!useActions.TryGetValue(itemType, out var list))
            useActions[itemType] = list = new List<Func<IItemInfo, bool>>();
        list.Add(action);
    }

    /// <summary>
    /// 用掉一件物品
    /// </summary>
    /// <returns>有处理器接了返回 true</returns>
    public bool Use(StoreItem item)
    {
        if (!item.CanUse)
            return false;
        if (!useActions.TryGetValue(item.ItemType, out var list))
            return false;
        foreach (var action in list)
        {
            if (action(item))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 往背包里加一件, 同名同类型的合并数量
    /// </summary>
    public StoreItem Add(UnifiedItem item)
    {
        var exist = items.FirstOrDefault(x => x.Name == item.Name
            && string.Equals(x.ItemType, item.ItemType, StringComparison.OrdinalIgnoreCase));
        if (exist != null)
        {
            exist.Count += item.Count;
            return exist;
        }
        var made = new StoreItem(this, item);
        items.Add(made);
        Added?.Invoke(made);
        return made;
    }

    /// <summary>
    /// 拿掉一件物品
    /// </summary>
    public void Remove(StoreItem item) => items.Remove(item);

    /// <summary>
    /// 从存档里读出背包
    /// </summary>
    /// <param name="data">存档的数据部分</param>
    public void Load(ILPS data)
    {
        items.Clear();
        pending.Clear();
        foreach (var line in data.Where(x => x.Name.StartsWith("item", StringComparison.OrdinalIgnoreCase)))
        {
            var type = line[(gstr)"itemtype"] ?? "Item";
            if (creators.TryGetValue(type, out var creator))
            {
                var made = creator(line);
                if (made != null)
                {
                    items.Add(new StoreItem(this, made));
                    continue;
                }
            }
            if (IsBuiltIn(type))
            {
                items.Add(new StoreItem(this, LPSConvert.DeserializeObject<UnifiedItem>(line)));
                continue;
            }
            // 认不出主人: 先挂起, 别把它降级成普通物品
            pending.Add(line);
        }
    }

    /// <summary>
    /// 把背包写回存档
    /// </summary>
    /// 挂起的行原样写回去, 这样"暂时没装那个 MOD"不会把玩家的东西吃掉
    public void Save(ILPS data)
    {
        foreach (var line in data.Where(x => x.Name.StartsWith("item", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            data.Remove(line);
        }
        int index = 0;
        foreach (var item in items)
        {
            data.Add(LPSConvert.SerializeObjectToLine<Line>(item.ToUnified(), "item" + index));
            index++;
        }
        foreach (var line in pending)
        {
            var copy = new Line(line) { Name = "item" + index };
            data.Add(copy);
            index++;
        }
    }

    /// <summary>
    /// 内置的物品类型, 不需要创建器
    /// </summary>
    /// 与 Windows 版 Item.ItemTypes 一致
    private static bool IsBuiltIn(string type)
        => string.Equals(type, "Item", StringComparison.OrdinalIgnoreCase)
        || string.Equals(type, "Food", StringComparison.OrdinalIgnoreCase)
        || string.Equals(type, "Toy", StringComparison.OrdinalIgnoreCase)
        || string.Equals(type, "Mail", StringComparison.OrdinalIgnoreCase)
        || string.Equals(type, "Tool", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 把某个类型的挂起行补出来
    /// </summary>
    private void Claim(string itemType)
    {
        if (pending.Count == 0 || !creators.TryGetValue(itemType, out var creator))
            return;
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            var line = pending[i];
            if (!string.Equals(line[(gstr)"itemtype"] ?? "Item", itemType, StringComparison.OrdinalIgnoreCase))
                continue;
            var made = creator(line);
            if (made == null)
                continue;
            pending.RemoveAt(i);
            var item = new StoreItem(this, made);
            items.Add(item);
            Added?.Invoke(item);
        }
    }
}

/// <summary>
/// 背包里的一件物品
/// </summary>
/// 直接实现契约里的 IItemInfo: 跨平台侧没有 Windows 那种"真对象 + 包装器"的两层,
/// 存的就是契约里那份数据。
public class StoreItem : IItemInfo
{
    private readonly ItemStore owner;

    internal StoreItem(ItemStore owner, UnifiedItem source)
    {
        this.owner = owner;
        Name = source.Name;
        ItemType = source.ItemType;
        Desc = source.Desc;
        Price = source.Price;
        Count = source.Count;
        Data = source.Data;
        CanUse = source.CanUse;
        Star = source.Star;
        IsSingle = source.IsSingle;
        Visibility = source.Visibility;
        Image = source.Image;
    }

    public string Name { get; }
    public string TranslateName => LinePutScript.Localization.LocalizeCore.Translate(Name);
    public string ItemType { get; set; }
    public string Desc { get; set; }
    public string Description => LinePutScript.Localization.LocalizeCore.Translate(Desc);
    public double Price { get; set; }
    public int Count { get; set; }
    public string Data { get; set; }
    public bool CanUse { get; set; }
    public bool Star { get; set; }
    public bool IsSingle { get; set; }
    public bool Visibility { get; set; }
    public string? Image { get; }

    /// <summary>
    /// 已解析出来的图片路径, 由宿主填
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>这边的物品不兼作食物</summary>
    public IFoodInfo? AsFood => null;

    public void Use()
    {
        if (owner.Use(this) && IsSingle)
            Consume();
    }

    public void Consume(int count = 1)
    {
        Count -= count;
        if (Count <= 0)
            owner.Remove(this);
    }

    public ILine ToLine() => LPSConvert.SerializeObjectToLine<Line>(ToUnified(), "item");

    /// <summary>
    /// 转回契约里的 DTO, 存档时用
    /// </summary>
    internal UnifiedItem ToUnified() => new UnifiedItem
    {
        Name = Name,
        ItemType = ItemType,
        Desc = Desc,
        Price = Price,
        Count = Count,
        Data = Data,
        CanUse = CanUse,
        Star = Star,
        IsSingle = IsSingle,
        Visibility = Visibility,
        Image = Image,
    };
}
