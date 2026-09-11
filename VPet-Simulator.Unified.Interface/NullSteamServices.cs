using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VPet_Simulator.Unified.Interface;

/// <summary>
/// 没有 Steam 时的那一份实现
/// </summary>
/// 每个方法都安安静静地什么都不做. 之所以要有它而不是让宿主到处判空:
/// 判空写在十几个调用点上, 迟早会漏一个, 表现是没开 Steam 的玩家点某个按钮就崩。
///
/// 放在契约里而不是后端: MOD 自己也可能要 ISteamServices, 给它一个现成的空实现
/// 比让它自己写一份好. 两个平台共用这一份 —— "Steam 不可用"的行为不该有平台差异。
public sealed class NullSteamServices : ISteamServices
{
    /// <summary>
    /// 现成的一个, 不用每处都 new
    /// </summary>
    public static readonly NullSteamServices Instance = new NullSteamServices();

    /// <summary>
    /// 为什么不可用
    /// </summary>
    public string? UnavailableReason { get; }

    public NullSteamServices(string? reason = null)
    {
        UnavailableReason = reason ?? "Steam 未连接";
    }

    public bool IsAvailable => false;
    public string UserName => Environment.UserName;
    public ulong UserId => 0;
    public void RunCallbacks() { }

    public bool CanStats => false;
    public int GetStat(string name, int defaultValue = 0) => defaultValue;
    public void SetStat(string name, int value) { }
    public void StoreStats() { }
    public Task<double> SubmitScoreAsync(string leaderboard, int score) => Task.FromResult(0d);

    public bool CanCloud => false;
    public IReadOnlyList<string> CloudFiles => Array.Empty<string>();
    public byte[]? CloudRead(string path) => null;
    public bool CloudWrite(string path, byte[] data) => false;
    public bool CloudDelete(string path) => false;

    public bool CanMultiplayer => false;
    public void SetRichPresence(string key, string value) { }
}
