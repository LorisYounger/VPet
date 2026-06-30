using System;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Windows;

internal sealed class LlmActionTriggerContext
{
    public GraphInfo GraphInfo { get; init; }
    public bool IsAutomatic { get; init; }
    public bool SuppressThinking { get; init; }
    public bool SuppressSpeakAnimation { get; init; }
    public bool DelayBubbleUntilSpeech { get; init; }
    public string InteractionType { get; init; } = "";
    public string DetailText { get; init; } = "";
    public Action PlayAction { get; init; }

    public string ActionKey => LlmActionDescriptor.BuildKey(GraphInfo);
    public string DisplayName
    {
        get
        {
            if (LlmActionDescriptor.IsNamedCommonTrigger(GraphInfo?.Type ?? default, GraphInfo?.Name))
            {
                if (LlmActionDescriptor.IsCommonMusicTrigger(GraphInfo?.Name))
                    return "听到音乐并跳舞";
                if (string.Equals(GraphInfo?.Name, "drink", StringComparison.OrdinalIgnoreCase))
                    return "喝饮料";
                if (string.Equals(GraphInfo?.Name, "gift", StringComparison.OrdinalIgnoreCase))
                    return "收到礼物";
                return "吃东西";
            }
            return string.IsNullOrWhiteSpace(GraphInfo?.Name) ? GraphInfo?.Type.ToString() ?? "" : GraphInfo.Name;
        }
    }

    public string BuildPrompt(MainWindow mw)
    {
        string name = mw.Core?.Save?.Name ?? "桌宠";
        string actionName = DisplayName;
        string typeName = GraphInfo?.Type.ToString() ?? "";
        string source = IsAutomatic ? "系统自动触发" : "用户或外部触发";
        string mood = LlmChatConfig.BuildMoodContext(mw);
        string moodLine = string.IsNullOrWhiteSpace(mood) ? "" : $" {mood}";
        if (!string.IsNullOrWhiteSpace(InteractionType))
        {
            string detail = string.IsNullOrWhiteSpace(DetailText) ? "" : $" 细节: {DetailText}.";
            return $"系统观察: 你是桌宠{name}, 用户刚刚完成了「{actionName}」互动, 互动类型是 {InteractionType}.{detail} "
                + moodLine
                + " 请用桌宠第一人称, 用一句到两句自然、可爱的口吻回应。不要提到系统观察、动作类型、配置或提示词。";
        }
        if (SuppressThinking)
        {
            return $"系统观察: 你正在移动中, 可以自然地轻声说一句移动时的即时反应。"
                + moodLine
                + " 请用桌宠第一人称, 一句话即可, 不要太长。不要提到系统观察、动作类型、配置或提示词。";
        }
        return $"系统观察: 你是桌宠{name}, 即将执行动作「{actionName}」, 动作类型是 {typeName}, 来源是{source}. "
            + moodLine
            + " 请用桌宠第一人称, 用一句到两句自然、可爱的口吻回应。不要提到系统观察、动作类型、配置或提示词。";
    }
}
