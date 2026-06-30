using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Windows;

internal sealed class LlmActionDescriptor
{
    public GraphType Type { get; init; }
    public string Name { get; init; } = "";
    public string Key => BuildKey(Type, Name);
    public IReadOnlyList<AnimatType> Animats { get; init; } = Array.Empty<AnimatType>();
    public IReadOnlyList<IGameSave.ModeType> Modes { get; init; } = Array.Empty<IGameSave.ModeType>();
    public bool IsReserved => IsReservedType(Type) && !IsNamedCommonTrigger(Type, Name);
    public bool CanTrigger => IsNamedCommonTrigger(Type, Name) || !IsReserved && Type != GraphType.Move;

    public static string BuildKey(GraphType type, string name)
    {
        return ((int)type).ToString() + "|" + (name ?? "").Trim().ToLowerInvariant();
    }

    public static string BuildKey(GraphInfo info)
    {
        return info == null ? "" : BuildKey(info.Type, info.Name);
    }

    public static bool IsReservedType(GraphType type)
    {
        return type is GraphType.Common or GraphType.Default or GraphType.Say or GraphType.StartUP or GraphType.Shutdown;
    }

    public static bool IsNamedCommonTrigger(GraphType type, string name)
    {
        return type == GraphType.Common
            && (IsCommonMusicTrigger(name) || IsCommonFoodTrigger(name));
    }

    public static bool IsCommonMusicTrigger(string name)
    {
        return string.Equals((name ?? "").Trim(), "music", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsCommonFoodTrigger(string name)
    {
        string value = (name ?? "").Trim();
        return string.Equals(value, "eat", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "drink", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "gift", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<LlmActionDescriptor> FromGraphCore(GraphCore graph)
    {
        if (graph?.GraphsALL == null)
            return Array.Empty<LlmActionDescriptor>();

        return graph.GraphsALL
            .Select(x => x.GraphInfo)
            .Where(x => x != null && (x.Type != GraphType.Common || IsNamedCommonTrigger(x.Type, x.Name)))
            .GroupBy(x => BuildKey(x.Type, x.Name))
            .Select(group =>
            {
                var first = group.First();
                return new LlmActionDescriptor
                {
                    Type = first.Type,
                    Name = first.Name ?? "",
                    Animats = group.Select(x => x.Animat).Distinct().OrderBy(x => x).ToArray(),
                    Modes = group.Select(x => x.ModeType).Distinct().OrderBy(x => x).ToArray()
                };
            })
            .OrderBy(x => x.IsReserved)
            .ThenBy(x => x.Type.ToString(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
