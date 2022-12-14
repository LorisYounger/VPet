using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// 宠物
    /// </summary>
    public class CorePet
    {
        /// <summary>
        /// 宠物图像
        /// </summary>
        public GraphCore Graph
        {
            get
            {
                var g = new GraphCore();
                foreach (var p in path)
                    LoadGraph(g, new DirectoryInfo(p), "");
                return g;
            }
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
        public Line GraphSetting;
        public CorePet(LpsDocument lps, DirectoryInfo directory)
        {
            Name = lps.First().Info;
            Intor = lps.First()["intor"].Info;
            path.Add(directory.FullName + "\\" + lps.First()["path"].Info);
            GraphSetting = lps["graph"];
        }

        public static void LoadGraph(GraphCore graph, DirectoryInfo di, string path_name)
        {
            var list = di.EnumerateDirectories();
            if (list.Count() == 0)
            {
                if (File.Exists(di.FullName + @"\info.lps"))
                {//如果自带描述信息,则手动加载
                    //TODO:
                }
                else
                {//自动加载 PNG ANMIN
                    path_name = path_name.Trim('_').ToLower();
                    for (int i = 0; i < GraphTypeValue.Length; i++)
                    {
                        if (path_name.StartsWith(GraphTypeValue[i][0]))
                        {

                            if (path_name.Contains("happy"))
                            {
                                graph.AddGraph(new PNGAnimation(di.FullName, Save.ModeType.Happy, (GraphType)i, GraphTypeValue[i][1], GraphTypeValue[i][2]), (GraphType)i);
                            }
                            if (path_name.Contains("nomal"))
                            {
                                graph.AddGraph(new PNGAnimation(di.FullName, Save.ModeType.Nomal, (GraphType)i, GraphTypeValue[i][1], GraphTypeValue[i][2]), (GraphType)i);
                            }
                            if (path_name.Contains("poorcondition"))
                            {
                                graph.AddGraph(new PNGAnimation(di.FullName, Save.ModeType.PoorCondition, (GraphType)i, GraphTypeValue[i][1], GraphTypeValue[i][2]), (GraphType)i);
                            }
                            if (path_name.Contains("ill"))
                            {
                                graph.AddGraph(new PNGAnimation(di.FullName, Save.ModeType.Ill, (GraphType)i, GraphTypeValue[i][1], GraphTypeValue[i][2]), (GraphType)i);
                            }
                            return;
                        }
                    }
#if DEBUG
                    throw new Exception("未知的图像类型: " + path_name);
#endif
                }
            }
            else
                foreach (var p in list)
                {
                    LoadGraph(graph, p, path_name + "_" + p.Name);
                }
        }
    }
}
