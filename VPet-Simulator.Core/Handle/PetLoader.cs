using LinePutScript;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 宠物加载器
    /// </summary>
    public class PetLoader
    {
        /// <summary>
        /// 宠物图像
        /// </summary>
        public GraphCore Graph()
        {
            var g = new GraphCore();
            foreach (var p in path)
                LoadGraph(g, new DirectoryInfo(p), "");
            g.GraphConfig = Config;
            return g;
        }
        /// <summary>
        /// 图像位置
        /// </summary>
        public List<string> path = new List<string>();
        /// <summary>
        /// 宠物名字
        /// </summary>
        public string Name;
        /// <summary>
        /// 宠物介绍
        /// </summary>
        public string Intor;
        public GraphCore.Config Config;
        public PetLoader(LpsDocument lps, DirectoryInfo directory)
        {
            Name = lps.First().Info;
            Intor = lps.First()["intor"].Info;
            path.Add(directory.FullName + "\\" + lps.First()["path"].Info);
            Config = new Config(lps);
        }
        public delegate void LoadGraphDelegate(GraphCore graph, FileSystemInfo path, ILine info);
        /// <summary>
        /// 自定义图片加载方法
        /// </summary>
        public static Dictionary<string, LoadGraphDelegate> IGraphConvert = new Dictionary<string, LoadGraphDelegate>()
        {
            { "pnganimation", PNGAnimation.LoadGraph},
            { "picture", Picture.LoadGraph },
            { "foodanimation", FoodAnimation.LoadGraph },
        };
        public static void LoadGraph(GraphCore graph, DirectoryInfo di, string path_name)
        {
            var list = di.EnumerateDirectories();
            if (File.Exists(di.FullName + @"\info.lps"))
            {
                //如果自带描述信息,则手动加载
                LpsDocument lps = new LpsDocument(File.ReadAllText(di.FullName + @"\info.lps"));
                foreach (ILine line in lps)
                {
                    if (IGraphConvert.TryGetValue(line.Name.ToLower(), out var func))
                    {
                        var str = line.GetString("path");
                        if (!string.IsNullOrEmpty(str))
                        {
                            var p = Path.Combine(di.FullName, str);
                            if (Directory.Exists(p))
                                func.Invoke(graph, new DirectoryInfo(p), line);
                            else if (File.Exists(p))
                                func.Invoke(graph, new FileInfo(p), line);
                            else
                                MessageBox.Show("未知的图像位置: " + p);
                        }
                        else
                            func.Invoke(graph, di, line);

                    }
                    else
                    {
                        MessageBox.Show("未知的图像类型: " + line.Name.ToLower());
                    }
                }
            }
            else if (list.Count() == 0)
            {
                //自动加载 PNG ANMIN
                path_name = path_name.Trim('_').ToLower();
                for (int i = 0; i < GraphTypeValue.Length; i++)
                {
                    if (path_name.StartsWith(GraphTypeValue[i]))
                    {
                        bool isused = false;
                        if (path_name.Contains("happy"))
                        {
                            graph.AddGraph(di.FullName, GameSave.ModeType.Happy, (GraphType)i);
                            isused = true;
                        }
                        if (path_name.Contains("nomal"))
                        {
                            graph.AddGraph(di.FullName, GameSave.ModeType.Nomal, (GraphType)i);
                            isused = true;
                        }
                        if (path_name.Contains("poorcondition"))
                        {
                            graph.AddGraph(di.FullName, GameSave.ModeType.PoorCondition, (GraphType)i);
                            isused = true;
                        }
                        if (path_name.Contains("ill"))
                        {
                            graph.AddGraph(di.FullName, GameSave.ModeType.Ill, (GraphType)i);
                            isused = true;
                        }
                        if (!isused)
                        {
                            graph.AddGraph(di.FullName, GameSave.ModeType.Nomal, (GraphType)i);
                        }
                        return;
                    }
                }
#if DEBUG
                MessageBox.Show("未知的图像类型: " + path_name);
#endif
            }
            else
                foreach (var p in list)
                {
                    LoadGraph(graph, p, path_name + "_" + p.Name);
                }
        }
    }
}
