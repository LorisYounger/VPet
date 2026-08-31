using Avalonia.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

public partial class GraphCore : IDisposable, IGraphCoreBase<IAvaloniaGraph>
{
    public int Resolution { get; set; } = 1000;
    public long IdleCacheTimeout = TimeSpan.FromMinutes(2).Ticks;
    public readonly Timer CleanTimer;

    public GraphCore(int resolution)
    {
        if (!Directory.Exists(CachePath))
        {
            Directory.CreateDirectory(CachePath);
        }

        CommConfig["Cache"] = new List<string>();
        Resolution = resolution;
        CleanTimer = new Timer(_ =>
        {
            long cleanTicks = DateTime.Now.Ticks - IdleCacheTimeout;
            for (int i = 0; i < GraphsALL.Count; i++)
            {
                GraphsALL[i].CleanupIdleCache(cleanTicks);
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public static string CachePath = Path.Combine(new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName!, "cache");
    public static readonly ConcurrentDictionary<string, SemaphoreSlim> SpriteSheetBuildLocks = new();

    public Dictionary<GraphInfo.GraphType, HashSet<string>> GraphsName { get; } = new();
    public Dictionary<string, Dictionary<GraphInfo.AnimatType, List<IAvaloniaGraph>>> GraphsList { get; } = new();
    public List<IAvaloniaGraph> GraphsALL { get; } = new();
    public Dictionary<string, Control> CommUIElements { get; } = new();
    public Dictionary<string, object> CommConfig { get; } = new();

    public void AddGraph(IAvaloniaGraph graph)
    {
        if (graph.GraphInfo.Type != GraphInfo.GraphType.Common)
        {
            if (!GraphsName.TryGetValue(graph.GraphInfo.Type, out var names))
            {
                names = new HashSet<string>();
                GraphsName.Add(graph.GraphInfo.Type, names);
            }
            names.Add(graph.GraphInfo.Name);
        }

        if (!GraphsList.TryGetValue(graph.GraphInfo.Name, out var byAnimat))
        {
            byAnimat = new Dictionary<GraphInfo.AnimatType, List<IAvaloniaGraph>>();
            GraphsList.Add(graph.GraphInfo.Name, byAnimat);
        }

        if (!byAnimat.TryGetValue(graph.GraphInfo.Animat, out var list))
        {
            list = new List<IAvaloniaGraph>();
            byAnimat.Add(graph.GraphInfo.Animat, list);
        }

        list.Add(graph);
        GraphsALL.Add(graph);
    }

    public string? FindName(GraphInfo.GraphType type)
    {
        if (GraphsName.TryGetValue(type, out var gl) && gl.Count > 0)
        {
            return gl.ElementAt(Random.Shared.Next(gl.Count));
        }
        return null;
    }

    public IAvaloniaGraph? FindGraph(string? graphName, GraphInfo.AnimatType animat, IGameSave.ModeType mode)
    {
        if (graphName == null)
            return null;

        if (GraphsList.TryGetValue(graphName, out var byAnimat) && byAnimat.TryGetValue(animat, out var graphs))
        {
            var list = graphs.FindAll(x => x.GraphInfo.ModeType == mode);
            if (list.Count > 0)
            {
                return list.Count == 1 ? list[0] : list[Random.Shared.Next(list.Count)];
            }

            if (mode == IGameSave.ModeType.Ill)
                return null;

            int down = (int)mode + 1;
            if (down < 3)
            {
                list = graphs.FindAll(x => x.GraphInfo.ModeType == (IGameSave.ModeType)down);
                if (list.Count > 0)
                    return list[Random.Shared.Next(list.Count)];
            }

            int up = (int)mode - 1;
            if (up >= 1)
            {
                list = graphs.FindAll(x => x.GraphInfo.ModeType == (IGameSave.ModeType)up);
                if (list.Count > 0)
                    return list[Random.Shared.Next(list.Count)];
            }

            list = graphs.FindAll(x => x.GraphInfo.ModeType != IGameSave.ModeType.Ill);
            if (list.Count > 0)
                return list[Random.Shared.Next(list.Count)];
        }

        return null;
    }

    public List<IAvaloniaGraph> FindGraphs(string? graphName, GraphInfo.AnimatType animat, IGameSave.ModeType mode)
    {
        if (graphName == null)
            return new List<IAvaloniaGraph>();

        if (GraphsList.TryGetValue(graphName, out var byAnimat) && byAnimat.TryGetValue(animat, out var graphs))
        {
            var list = graphs.FindAll(x => x.GraphInfo.ModeType == mode);
            if (list.Count > 0)
                return list;

            int down = (int)mode + 1;
            if (down < 3)
            {
                list = graphs.FindAll(x => x.GraphInfo.ModeType == (IGameSave.ModeType)down);
                if (list.Count > 0)
                    return list;
            }

            int up = (int)mode - 1;
            if (up >= 0)
            {
                list = graphs.FindAll(x => x.GraphInfo.ModeType == (IGameSave.ModeType)up);
                if (list.Count > 0)
                    return list;
            }

            return graphs;
        }

        return new List<IAvaloniaGraph>();
    }

    public void Dispose()
    {
        CleanTimer.Dispose();
        foreach (var graph in GraphsALL)
        {
            graph.Dispose();
        }
        GraphsALL.Clear();
        GraphsList.Clear();
        GraphsName.Clear();
        CommUIElements.Clear();
        CommConfig.Clear();
    }

    long IGraphCoreBase<IAvaloniaGraph>.IdleCacheTimeout => IdleCacheTimeout;
}
