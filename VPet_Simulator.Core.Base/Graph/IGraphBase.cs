using System;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 图像接口基础契约 (平台无关)
    /// </summary>
    public interface IGraphBase : IEquatable<object>, IDisposable
    {
        bool IsLoop { get; set; }
        bool IsReady { get; }
        bool IsFail { get; }
        string FailMessage { get; }
        GraphInfo GraphInfo { get; }
        object? Control { get; }
        string? Path { get; }
        long LastUseTimeTicks { get; }
        void Stop(bool stopEndAction);
        void SetContinue();
        void Touch();
        void CleanupIdleCache(long nowTicks);
    }
}
