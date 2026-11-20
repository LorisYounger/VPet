using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Windows.Interface;

#if VPET_AVALONIA
namespace VPet_Simulator.MutiPlatform;
#else
namespace VPet_Simulator.Windows;
#endif

internal sealed partial class UnifiedItemRegistry
{
    // 所有窗口共用 Item.Creators。读档时按窗口查找该类型的创建器。
    private sealed class CreatorsForType(string itemType, Func<IMainWindow, ILine, Item?>? fallback)
    {
        internal readonly ConditionalWeakTable<IMainWindow, UnifiedItemRegistry> Windows = new();

        internal Item? Create(IMainWindow window, ILine line)
        {
            if (Windows.TryGetValue(window, out var registry))
            {
                var item = registry.creators[itemType](line);
                return item == null ? null : Materialize(item);
            }
            return fallback != null ? fallback(window, line) : LPSConvert.DeserializeObject<Item>(line);
        }
    }

    private readonly Dictionary<string, Func<ILine, UnifiedItem?>> creators = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Type, Func<IMainWindow, Item, bool> Action)> registeredUseActions = new();

    public void RegisterCreator(string itemType, Func<ILine, UnifiedItem?> creator)
    {
        creators[itemType] = creator;
        Item.Creators.TryGetValue(itemType, out var previous);
        if (previous?.Target is not CreatorsForType entries)
        {
            entries = new CreatorsForType(itemType, previous);
            Item.Creators[itemType] = entries.Create;
        }
        entries.Windows.AddOrUpdate(mw, this);

        // 读档前注册时背包为空。读档后注册时，用新创建器重建背包中同类型的普通 Item。
        for (int i = 0; i < mw.Items.Count; i++)
        {
            var item = mw.Items[i];
            if (item.GetType() != typeof(Item) || !string.Equals(item.ItemType, itemType, StringComparison.OrdinalIgnoreCase))
                continue;
            var made = creator(LPSConvert.SerializeObjectToLine<Line>(item, "item"));
            if (made == null)
                continue;
            var native = Materialize(made);
            LoadItemSource(native);
            mw.Items[i] = native;
        }
    }

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

    private void UnregisterItems()
    {
        foreach (string type in creators.Keys)
            if (Item.Creators.TryGetValue(type, out var create) && create.Target is CreatorsForType entries
                && entries.Windows.TryGetValue(mw, out var owner) && owner == this)
                entries.Windows.Remove(mw);

        foreach (var registration in registeredUseActions)
        {
            if (Item.UseAction.TryGetValue(registration.Type, out var actions))
                actions.Remove(registration.Action);
#if !VPET_AVALONIA
            pendingUseActions.Remove(registration);
#endif
        }
        registeredUseActions.Clear();
    }
}
