using LinePutScript;
using LinePutScript.Localization.WPF;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Threading;
using static VPet_Simulator.Core.GraphCore;


namespace VPet_Simulator.Core
{
    /// <summary>
    /// 宠物加载器
    /// </summary>
    public class PetLoader
    {
        /// <summary>
        /// 动画数量
        /// </summary>
        public int GraphCount { get; private set; }
        /// <summary>
        /// 宠物图像
        /// </summary>
        public GraphCore Graph(int Resolution, Dispatcher dispatcher)
        {
            GraphCount = 0;
            var g = new GraphCore(Resolution, dispatcher);
            foreach (var p in path)
                GraphCount += LoadGraph(g, new DirectoryInfo(p), p);
            g.GraphConfig = Config;
            return g;
        }
        /// <summary>
        /// 图像位置
        /// </summary>
        public List<string> path = new List<string>();
        /// <summary>
        /// 宠物介绍名字
        /// </summary>
        public string Name;
        /// <summary>
        /// 宠物介绍
        /// </summary>
        public string Intor;
        /// <summary>
        /// 宠物默认名字
        /// </summary>
        public string PetName;
        public GraphCore.Config Config;
        public PetLoader(LpsDocument lps, DirectoryInfo directory)
        {
            Name = lps.First().Info;
            Intor = lps.First()["intor"].Info;
            PetName = lps.First()["petname"].Info;
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
        /// <summary>
        /// 加载图像动画
        /// </summary>
        /// <param name="graph">要加载的动画核心</param>
        /// <param name="di">当前历遍的目录</param>
        /// <param name="startuppath">起始目录</param>
        public static int LoadGraph(GraphCore graph, DirectoryInfo di, string startuppath)
        {
            if(!di.Exists)
                return 0;
            int GraphCount = 0;
            var list = di.EnumerateDirectories();
            if (File.Exists(di.FullName + @"\info.lps"))
            {
                //如果自带描述信息,则手动加载
                LpsDocument lps = new LpsDocument(File.ReadAllText(di.FullName + @"\info.lps"));
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
                                MessageBox.Show(LocalizeCore.Translate("未知的图像位置: ") + p);
                        }
                        else
                            func.Invoke(graph, di, line);
                        GraphCount++;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(line.Name))
                            MessageBox.Show(LocalizeCore.Translate("未知的图像类型: ") + line.Name.ToLowerInvariant());
                    }
                }
            }
            else if (list.Count() == 0)
            {//开始自动生成
                var paths = di.GetFiles();
                if (paths.Length == 0)
                    return GraphCount;
                if (paths.Length == 1)
                    Picture.LoadGraph(graph, paths[0], new Line("picture", "", "", new Sub("startuppath", startuppath)));
                else
                    PNGAnimation.LoadGraph(graph, di, new Line("pnganimation", "", "", new Sub("startuppath", startuppath)));
                GraphCount++;
            }
            else
                foreach (var p in list)
                {
                    GraphCount += LoadGraph(graph, p, startuppath);
                }
            return GraphCount;
        }
    }
}
