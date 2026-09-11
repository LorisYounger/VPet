using System;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 语音与说话气泡的配合
/// </summary>
/// 只有一条规则, 但它决定了"说话看起来对不对": 字打完了而语音还在说的时候,
/// 气泡不能先收 —— 收早了就变成桌宠闭着嘴还在出声。
public static class VoiceRules
{
    /// <summary>
    /// 语音还剩这么多秒以上时, 气泡不收
    /// </summary>
    /// 留两秒是有意的: 收气泡本身有个渐隐动画, 卡着最后一刻收反而更突兀。
    /// 数值取自 Windows 版 MessageBar。
    public const double HoldSeconds = 2;

    /// <summary>
    /// 现在该不该继续挂着气泡
    /// </summary>
    /// <param name="playingVoice">正在放语音吗</param>
    /// <param name="remaining">还剩多久播完; 播放器不知道时传 Zero</param>
    /// 播放器给不出剩余时长(返回 Zero)时按"可以收"处理 —— 宁可收早也不要永远挂着
    public static bool ShouldHoldBubble(bool playingVoice, TimeSpan remaining)
        => playingVoice && remaining.TotalSeconds > HoldSeconds;
}
