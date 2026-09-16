using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VPet_Simulator.Unified.Interface;

/// <summary>
/// Steam 能做的事
/// </summary>
/// Facepunch.Steamworks 的托管 API 在三个平台上是一样的, 换的只是底下那个原生绑定
/// (Win64 / Win32 / Posix). 所以这里抽的不是"平台差异", 而是"有没有 Steam":
/// 玩家可能是从别处下载的、可能没开 Steam、可能在没登录的机器上跑。
///
/// **每一项能力都要能降级**. VPet 的成就、排行榜、云存档、联机都建立在 Steam 上,
/// 但缺了它们游戏本身照样能玩 —— 所以这里的约定是"不可用时安静地什么都不做",
/// 而不是抛异常. 调用方先看 IsAvailable, 想细分就看各自的 Can* 。
public interface ISteamServices
{
    /// <summary>
    /// Steam 接上了吗
    /// </summary>
    /// 为 false 时下面所有操作都是空转
    bool IsAvailable { get; }

    /// <summary>
    /// 不可用的原因, 可用时为 null
    /// </summary>
    /// 给日志和设置界面看的 —— "Steam 功能不可用"这句话本身没法帮玩家解决问题
    string? UnavailableReason { get; }

    /// <summary>
    /// 玩家昵称; 取不到时返回系统用户名
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// 玩家的 SteamId; 取不到时为 0
    /// </summary>
    ulong UserId { get; }

    /// <summary>
    /// 驱动一次回调
    /// </summary>
    /// Steam 的异步结果要靠定时调它才回得来, 由宿主放在自己的心跳里
    void RunCallbacks();

    // ---- 统计与成就 ----

    /// <summary>统计与排行榜可用吗</summary>
    bool CanStats { get; }

    /// <summary>读一个 Steam 统计项</summary>
    int GetStat(string name, int defaultValue = 0);

    /// <summary>写一个 Steam 统计项</summary>
    void SetStat(string name, int value);

    /// <summary>把统计提交上去</summary>
    void StoreStats();

    /// <summary>
    /// 把成绩报到排行榜, 并取回自己的百分位
    /// </summary>
    /// <param name="leaderboard">排行榜名</param>
    /// <param name="score">成绩</param>
    /// <returns>0~1, 越大越靠前; 不可用或没上榜时为 0</returns>
    /// 直接给百分位而不是名次: 调用方(年度报告)要的就是这个, 名次和总人数
    /// 拿出来还得再算一遍, 算错了两端就不一样了
    Task<double> SubmitScoreAsync(string leaderboard, int score);

    // ---- 云存档 ----

    /// <summary>云存档可用吗</summary>
    bool CanCloud { get; }

    /// <summary>列出云端的文件名</summary>
    IReadOnlyList<string> CloudFiles { get; }

    /// <summary>读一个云端文件; 读不到返回 null</summary>
    byte[]? CloudRead(string path);

    /// <summary>写一个云端文件</summary>
    bool CloudWrite(string path, byte[] data);

    /// <summary>删一个云端文件</summary>
    bool CloudDelete(string path);

    // ---- 好友与在线状态 ----

    /// <summary>联机可用吗</summary>
    bool CanMultiplayer { get; }

    /// <summary>设置在线状态里显示的文字</summary>
    void SetRichPresence(string key, string value);
}
