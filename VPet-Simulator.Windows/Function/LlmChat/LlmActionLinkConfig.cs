using LinePutScript;
using LinePutScript.Dictionary;
using System;
using System.Collections.Generic;
using System.Linq;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Windows;

internal sealed class LlmActionLinkConfig
{
    private const string NodeName = "LLMActionLink";
    private const int CurrentDefaultVersion = 4;

    public bool Enabled { get; set; }
    public bool AutoActionsEnabled { get; set; } = true;
    public bool ManualActionsEnabled { get; set; }
    public bool SkipWhenBusy { get; set; } = true;
    public bool MoveChatterEnabled { get; set; } = true;
    public int MoveChatterMinSeconds { get; set; } = 35;
    public int MoveChatterMaxSeconds { get; set; } = 95;
    public bool DebugLogEnabled { get; set; }
    public bool ObservedAutoActionsEnabled { get; set; } = true;
    public bool Initialized { get; set; }
    public int DefaultVersion { get; set; }
    public HashSet<string> EnabledActionKeys { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static LlmActionLinkConfig Load(LPS_D set)
    {
        var line = set[NodeName];
        string savedKeys = line.GetString("enabledActionKeys", "");
        var config = new LlmActionLinkConfig
        {
            Enabled = line.GetBool("enabled"),
            AutoActionsEnabled = GetBool(line, "autoActionsEnabled", true),
            ManualActionsEnabled = line.GetBool("manualActionsEnabled"),
            SkipWhenBusy = GetBool(line, "skipWhenBusy", true),
            MoveChatterEnabled = GetBool(line, "moveChatterEnabled", true),
            MoveChatterMinSeconds = Math.Clamp(line.GetInt("moveChatterMinSeconds", 35), 5, 3600),
            MoveChatterMaxSeconds = Math.Clamp(line.GetInt("moveChatterMaxSeconds", 95), 5, 3600),
            DebugLogEnabled = line.GetBool("debugLogEnabled"),
            ObservedAutoActionsEnabled = GetBool(line, "observedAutoActionsEnabled", true),
            Initialized = GetBool(line, "initialized", !string.IsNullOrWhiteSpace(savedKeys)),
            DefaultVersion = line.GetInt("defaultVersion", 0)
        };
        foreach (string key in SplitKeys(savedKeys))
            config.EnabledActionKeys.Add(key);
        if (config.MoveChatterMaxSeconds < config.MoveChatterMinSeconds)
            config.MoveChatterMaxSeconds = config.MoveChatterMinSeconds;
        return config;
    }

    public void InitializeDefaultActions(IEnumerable<LlmActionDescriptor> actions)
    {
        if (Initialized && DefaultVersion >= CurrentDefaultVersion)
            return;
        var actionList = (actions ?? Array.Empty<LlmActionDescriptor>()).ToArray();
        var legacyTriggerableKeys = actionList
            .Where(x => x.CanTrigger && !LlmActionDescriptor.IsCommonFoodTrigger(x.Name))
            .Select(x => x.Key)
            .ToArray();
        bool resetBroadDefaults = Initialized
            && DefaultVersion < 4
            && legacyTriggerableKeys.Length > 0
            && legacyTriggerableKeys.All(x => EnabledActionKeys.Contains(x));
        if (resetBroadDefaults)
            EnabledActionKeys.Clear();

        foreach (var action in actionList)
        {
            if (ShouldEnableByDefault(action))
                EnabledActionKeys.Add(action.Key);
        }
        if (!Initialized || DefaultVersion < 4)
            ManualActionsEnabled = true;
        Initialized = true;
        DefaultVersion = CurrentDefaultVersion;
    }

    public void Save(LPS_D set)
    {
        var line = set[NodeName];
        line.SetBool("enabled", Enabled);
        line.SetBool("autoActionsEnabled", AutoActionsEnabled);
        line.SetBool("manualActionsEnabled", ManualActionsEnabled);
        line.SetBool("skipWhenBusy", SkipWhenBusy);
        line.SetBool("moveChatterEnabled", MoveChatterEnabled);
        line.SetInt("moveChatterMinSeconds", Math.Clamp(MoveChatterMinSeconds, 5, 3600));
        line.SetInt("moveChatterMaxSeconds", Math.Clamp(Math.Max(MoveChatterMaxSeconds, MoveChatterMinSeconds), 5, 3600));
        line.SetBool("debugLogEnabled", DebugLogEnabled);
        line.SetBool("observedAutoActionsEnabled", ObservedAutoActionsEnabled);
        line.SetBool("initialized", true);
        line.SetInt("defaultVersion", CurrentDefaultVersion);
        line[(gstr)"enabledActionKeys"] = string.Join(";", EnabledActionKeys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }

    public bool IsActionEnabled(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && EnabledActionKeys.Contains(key);
    }

    private static IEnumerable<string> SplitKeys(string value)
    {
        return (value ?? "")
            .Split(new[] { ';', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0);
    }

    private static bool GetBool(ILine line, string key, bool defaultValue)
    {
        return bool.TryParse(line.GetString(key, defaultValue.ToString()), out bool value) ? value : defaultValue;
    }

    private static bool ShouldEnableByDefault(LlmActionDescriptor action)
    {
        if (action?.CanTrigger != true)
            return false;
        if (LlmActionDescriptor.IsCommonMusicTrigger(action.Name)
            || LlmActionDescriptor.IsCommonFoodTrigger(action.Name))
            return true;
        return action.Type is GraphType.Touch_Head
            or GraphType.Touch_Body
            or GraphType.Raised_Static
            or GraphType.Idel
            or GraphType.Sleep
            or GraphType.StateONE
            or GraphType.StateTWO
            or GraphType.Switch_Up
            or GraphType.Switch_Down
            or GraphType.Switch_Thirsty
            or GraphType.Switch_Hunger
            or GraphType.SideHide_Left_Main
            or GraphType.SideHide_Left_Rise
            or GraphType.SideHide_Right_Main
            or GraphType.SideHide_Right_Rise
            or GraphType.Work;
    }

}
