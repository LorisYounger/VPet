//跨平台: 原文复制自 VPet-Simulator.Windows/Function/SteamCapability.cs, 只换了命名空间. Facepunch.Steamworks 的托管 API 三个平台一样,
//这边按目标 RID 引用官方 Facepunch.Steamworks 2.5.2: Windows x64 用 Win64, Linux/macOS 用 Posix.
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPet_Simulator.Unified.Interface;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// Steam 能力 (Windows 侧)
/// </summary>
/// Facepunch.Steamworks 的托管 API 三个平台一样, 换的只是原生绑定
/// (Win64 / Win32 / Posix), 所以这份实现搬到跨平台侧几乎是逐字的 —— 换掉包引用
/// 就行. 先在 Windows 上做出来是为了拿真的 Steam 把接口验一遍: 接口设计错了,
/// 在没有 Steam 的机器上是看不出来的。
///
/// 每个方法都吞异常: Steam 的调用会在客户端退出、网络断开、家庭共享受限等等
/// 情况下抛, 而这些都不该让桌宠崩掉。
internal sealed class SteamCapability : ISteamServices
{
    private readonly MainWindow mw;

    internal SteamCapability(MainWindow mw)
    {
        this.mw = mw;
    }

    public bool IsAvailable => mw.IsSteamUser;

    public string? UnavailableReason => IsAvailable ? null : "Steam 未连接";

    public string UserName
    {
        get
        {
            try { return IsAvailable ? SteamClient.Name : Environment.UserName; }
            catch (Exception) { return Environment.UserName; }
        }
    }

    public ulong UserId
    {
        get
        {
            try { return IsAvailable ? SteamClient.SteamId.Value : 0; }
            catch (Exception) { return 0; }
        }
    }

    public void RunCallbacks()
    {
        if (!IsAvailable)
            return;
        try { SteamClient.RunCallbacks(); }
        catch (Exception) { }
    }

    // ---- 统计与成就 ----

    public bool CanStats => IsAvailable;

    public int GetStat(string name, int defaultValue = 0)
    {
        if (!CanStats)
            return defaultValue;
        try { return SteamUserStats.GetStatInt(name); }
        catch (Exception) { return defaultValue; }
    }

    public void SetStat(string name, int value)
    {
        if (!CanStats)
            return;
        try { SteamUserStats.SetStat(name, value); }
        catch (Exception) { }
    }

    public void StoreStats()
    {
        if (!CanStats)
            return;
        try { SteamUserStats.StoreStats(); }
        catch (Exception) { }
    }

    public async Task<double> SubmitScoreAsync(string leaderboard, int score)
    {
        if (!CanStats)
            return 0;
        try
        {
            var board = await SteamUserStats.FindOrCreateLeaderboardAsync(
                leaderboard, LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
            if (!board.HasValue)
                return 0;
            var result = await board.Value.ReplaceScore(score);
            //百分位的算式在共享后端里, 与年度报告里用的是同一个
            return VPet_Simulator.Unified.Services.StatsSummary.RankPercentile(
                result?.NewGlobalRank, board.Value.EntryCount);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    // ---- 云存档 ----

    public bool CanCloud => IsAvailable;

    public IReadOnlyList<string> CloudFiles
    {
        get
        {
            if (!CanCloud)
                return Array.Empty<string>();
            try { return SteamRemoteStorage.Files.ToList(); }
            catch (Exception) { return Array.Empty<string>(); }
        }
    }

    public byte[]? CloudRead(string path)
    {
        if (!CanCloud)
            return null;
        try
        {
            var data = SteamRemoteStorage.FileRead(path);
            return data == null || data.Length == 0 ? null : data;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public bool CloudWrite(string path, byte[] data)
    {
        if (!CanCloud)
            return false;
        try { return SteamRemoteStorage.FileWrite(path, data); }
        catch (Exception) { return false; }
    }

    public bool CloudDelete(string path)
    {
        if (!CanCloud)
            return false;
        try { return SteamRemoteStorage.FileDelete(path); }
        catch (Exception) { return false; }
    }

    // ---- 好友与在线状态 ----

    public bool CanMultiplayer => IsAvailable;

    public void SetRichPresence(string key, string value)
    {
        if (!CanMultiplayer)
            return;
        try { SteamFriends.SetRichPresence(key, value); }
        catch (Exception) { }
    }
}
