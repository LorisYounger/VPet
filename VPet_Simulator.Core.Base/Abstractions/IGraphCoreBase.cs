using System.Collections.Generic;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 图像核心基础契约 (平台无关)
    /// </summary>
    public interface IGraphCoreBase<TGraph> where TGraph : IGraphBase
    {
        int Resolution { get; }
        long IdleCacheTimeout { get; }
        Dictionary<GraphInfo.GraphType, HashSet<string>> GraphsName { get; }
        Dictionary<string, Dictionary<GraphInfo.AnimatType, List<TGraph>>> GraphsList { get; }
        List<TGraph> GraphsALL { get; }
        void AddGraph(TGraph graph);
        string? FindName(GraphInfo.GraphType type);
        TGraph? FindGraph(string? graphName, GraphInfo.AnimatType animat, IGameSave.ModeType mode);
        List<TGraph> FindGraphs(string? graphName, GraphInfo.AnimatType animat, IGameSave.ModeType mode);
    }
}
