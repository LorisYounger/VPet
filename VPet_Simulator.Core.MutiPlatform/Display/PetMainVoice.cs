using System;
using VPet_Simulator.Unified.Interface;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 语音
/// </summary>
/// 桌宠自己不带音频后端: Avalonia 没有内置的, 而引一个原生音频库是个不该由迁移
/// 工作单方面替项目做的决定(选哪个库、怎么按平台摆原生依赖、许可证如何, 都是
/// 长期成本). 所以定成统一契约里的 IVoicePlayer, 由跨平台语音 MOD 来实现,
/// 这边只负责转发和口型同步。
///
/// 没装语音 MOD 时一切照常, 只是不出声 —— 不该因为缺个可选功能就报错。
public partial class PetMain
{
    /// <summary>
    /// MOD 提供的语音播放器, 没有就是 null
    /// </summary>
    public IVoicePlayer? VoicePlayer { get; set; }

    /// <summary>
    /// 正在放语音吗
    /// </summary>
    public bool PlayingVoice => VoicePlayer?.IsPlaying == true;

    /// <summary>
    /// 语音还剩多久播完
    /// </summary>
    /// 消息栏靠它决定气泡什么时候收起 —— 只有一个"播完了"的事件是不够的,
    /// 气泡得在话说完之前就一直挂着
    public TimeSpan VoiceRemaining => VoicePlayer?.Remaining ?? TimeSpan.Zero;

    private double playVoiceVolume = 1;

    /// <summary>
    /// 语音音量 0~1
    /// </summary>
    public double PlayVoiceVolume
    {
        get => playVoiceVolume;
        set
        {
            playVoiceVolume = value;
            if (VoicePlayer != null)
                VoicePlayer.Volume = value;
        }
    }

    /// <summary>
    /// 播放一段语音
    /// </summary>
    /// <param name="path">音频文件路径</param>
    public void PlayVoice(string path)
    {
        var player = VoicePlayer;
        if (player == null)
            return;
        try
        {
            player.Volume = playVoiceVolume;
            player.Play(path);
        }
        catch (Exception)
        {
            // 一句语音放不出来不该把说话整个打断
        }
    }

    /// <summary>
    /// 停止语音
    /// </summary>
    public void StopVoice()
    {
        try { VoicePlayer?.Stop(); }
        catch (Exception) { }
    }
}
