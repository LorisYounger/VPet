using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform.Graph;
using static VPet_Simulator.Core.MutiPlatform.Graph.GraphCore;

namespace VPet_Simulator.Core.MutiPlatform;

public class PetLoader
{
    public int GraphCount { get; private set; }

    public GraphCore Graph(int resolution)
    {
        GraphCount = 0;
        var g = new GraphCore(resolution)
        {
            GraphConfig = Config
        };
        foreach (var p in path)
            GraphCount += LoadGraph(g, new DirectoryInfo(p), p);
        return g;
    }

    public List<string> path = new();
    public string Name;
    public string Intor;
    public string PetName;
    public GraphCore.Config Config;

    public PetLoader(LpsDocument lps, DirectoryInfo directory)
    {
        Name = lps.First()!.Info;
        Intor = lps.First()!["intor"].Info;
        PetName = lps.First()!["petname"].Info;
        path.Add(directory.FullName + "\\" + lps.First()!["path"].Info);
        Config = new Config(lps);
    }

    public delegate void LoadGraphDelegate(GraphCore graph, FileSystemInfo path, ILine info);

    public static Dictionary<string, LoadGraphDelegate> IGraphConvert = new()
    {
        { "pnganimation", PNGAnimation.LoadGraph },
        { "apnganimation", APNGAnimation.LoadGraph },
        { "picture", Picture.LoadGraph },
        { "foodanimation", FoodAnimation.LoadGraph },
    };

    public static int LoadGraph(GraphCore graph, DirectoryInfo di, string startuppath)
    {
        if (!di.Exists)
            return 0;

        int graphCount = 0;
        var list = di.EnumerateDirectories();
        if (File.Exists(di.FullName + @"\info.lps"))
        {
            LpsDocument lps = new(File.ReadAllText(di.FullName + @"\info.lps"));
            foreach (ILine line in lps)
            {
                if (IGraphConvert.TryGetValue(line.Name.ToLowerInvariant(), out var func))
                {
                    line.Add(new Sub("startuppath", startuppath));
                    var str = line.GetString("path");
                    if (!string.IsNullOrEmpty(str))
                    {
                        var p = Path.Combine(di.FullName, str);
                        if (Directory.Exists(p))
                            func.Invoke(graph, new DirectoryInfo(p), line);
                        else if (File.Exists(p))
                            func.Invoke(graph, new FileInfo(p), line);
                        else
                            Console.WriteLine("Unknow Graph Type: " + p);
                    }
                    else
                    {
                        func.Invoke(graph, di, line);
                    }
                    graphCount++;
                }
                else if (!string.IsNullOrEmpty(line.Name))
                {
                    Console.WriteLine("Unknow Graph Type: " + line.Name.ToLowerInvariant());
                }
            }
        }
        else if (!list.Any())
        {
            var paths = di.GetFiles();
            if (paths.Length == 0)
                return graphCount;

            if (paths.Length == 1)
                Picture.LoadGraph(graph, paths[0], new Line("picture", "", "", new Sub("startuppath", startuppath)));
            else
                PNGAnimation.LoadGraph(graph, di, new Line("pnganimation", "", "", new Sub("startuppath", startuppath)));
            graphCount++;
        }
        else
        {
            foreach (var p in list)
            {
                graphCount += LoadGraph(graph, p, startuppath);
            }
        }

        return graphCount;
    }
}
