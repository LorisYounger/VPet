using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 图像显示核心
    /// </summary>
    public class GraphCore
    {
        /// <summary>
        /// 桌宠图形渲染的分辨率,越高图形越清晰
        /// </summary>
        public int Resolution { get; set; } = 1000;
        public GraphCore(int resolution)
        {
            if (!Directory.Exists(CachePath))
                Directory.CreateDirectory(CachePath);
            CommConfig["Cache"] = new List<string>();
            Resolution = resolution;
        }

        public static string CachePath = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName + @"\cache";

        /// <summary>
        /// 图像名字字典: 动画类型->动画名字
        /// </summary>
        public Dictionary<GraphType, HashSet<string>> GraphsName = new Dictionary<GraphType, HashSet<string>>();
        /// <summary>
        /// 图像字典 动画名字->状态+动作->动画
        /// </summary>
        public Dictionary<string, Dictionary<AnimatType, List<IGraph>>> GraphsList = new Dictionary<string, Dictionary<AnimatType, List<IGraph>>>();
        /// <summary>
        /// 通用UI资源
        /// </summary>
        public Dictionary<string, UIElement> CommUIElements = new Dictionary<string, UIElement>();
        /// <summary>
        /// 通用设置属性/方法
        /// </summary>
        public Dictionary<string, object> CommConfig = new Dictionary<string, object>();
        /// <summary>
        /// 添加动画
        /// </summary>
        /// <param name="graph">动画</param>
        public void AddGraph(IGraph graph)
        {
            if (graph.GraphInfo.Type != GraphType.Common)
            {
                if (!GraphsName.TryGetValue(graph.GraphInfo.Type, out var d2))
                {
                    d2 = new HashSet<string>();
                    GraphsName.Add(graph.GraphInfo.Type, d2);
                }
                d2.Add(graph.GraphInfo.Name);
            }
            if (!GraphsList.TryGetValue(graph.GraphInfo.Name, out var d3))
            {
                d3 = new Dictionary<AnimatType, List<IGraph>>();
                GraphsList.Add(graph.GraphInfo.Name, d3);
            }
            if (!d3.TryGetValue(graph.GraphInfo.Animat, out var l3))
            {
                l3 = new List<IGraph>();
                d3.Add(graph.GraphInfo.Animat, l3);
            }
            l3.Add(graph);
        }

        /// <summary>
        /// 获得随机动画名字
        /// </summary>
        /// <param name="type">动画类型</param>
        /// <returns>动画名字,找不到则返回null</returns>
        public string FindName(GraphType type)
        {
            if (GraphsName.TryGetValue(type, out var gl))
            {
                return gl.ElementAt(Function.Rnd.Next(gl.Count));
            }
            return null;
        }
        /// <summary>
        /// 查找动画
        /// </summary>
        /// <param name="GraphName">动画名字</param>
        /// <param name="mode">状态类型,找不到就找相同动画类型</param>
        /// <param name="animat">动画的动作 Start Loop End</param>
        public IGraph FindGraph(string GraphName, AnimatType animat, GameSave.ModeType mode)
        {
            if (GraphName == null)
                return null;
            if (GraphsList.TryGetValue(GraphName, out var d3) && d3.TryGetValue(animat, out var gl))
            {
                var list = gl.FindAll(x => x.GraphInfo.ModeType == mode);
                if (list.Count > 0)
                {
                    if (list.Count == 1)
                        return list[0];
                    return list[Function.Rnd.Next(list.Count)];
                }
                if (mode == GameSave.ModeType.Ill)
                {
                    return null;
                }
                int i = (int)mode + 1;
                if (i < 3)
                {
                    //向下兼容的动画
                    list = gl.FindAll(x => x.GraphInfo.ModeType == (GameSave.ModeType)i);
                    if (list.Count > 0)
                        return list[Function.Rnd.Next(list.Count)];
                }
                i = (int)mode - 1;
                if (i >= 1)
                {
                    //向上兼容的动画
                    list = gl.FindAll(x => x.GraphInfo.ModeType == (GameSave.ModeType)i);
                    if (list.Count > 0)
                        return list[Function.Rnd.Next(list.Count)];
                }
                //如果实在找不到,就走随机数(无生病)
                list = gl.FindAll(x => x.GraphInfo.ModeType != GameSave.ModeType.Ill);
                if (list.Count > 0)
                    return list[Function.Rnd.Next(list.Count)];
            }
            return null;// FindGraph(GraphType.Default, mode);
        }
        /// <summary>
        /// 查找动画列表
        /// </summary>
        /// <param name="mode">状态类型,找不到就找相同动画类型</param>
        /// <param name="animat">动画的动作 Start Loop End</param>
        public List<IGraph> FindGraphs(string GraphName, AnimatType animat, GameSave.ModeType mode)
        {
            if (GraphName == null)
                return null;
            if (GraphsList.TryGetValue(GraphName, out var d3) && d3.TryGetValue(animat, out var gl))
            {
                var list = gl.FindAll(x => x.GraphInfo.ModeType == mode);
                if (list.Count > 0)
                {
                    return list;
                }
                int i = (int)mode + 1;
                if (i < 3)
                {
                    //向下兼容的动画
                    list = gl.FindAll(x => x.GraphInfo.ModeType == (GameSave.ModeType)i);
                    if (list.Count > 0)
                        return list;
                }
                i = (int)mode - 1;
                if (i >= 0)
                {
                    //向上兼容的动画
                    list = gl.FindAll(x => x.GraphInfo.ModeType == (GameSave.ModeType)i);
                    if (list.Count > 0)
                        return list;
                }
                //如果实在找不到,就走随机数
                //if (mode != GameSave.ModeType.Ill)
                //{
                list = gl;
                if (list.Count > 0)
                    return list;
                //}                
            }
            return null;// FindGraph(GraphType.Default, mode);
        }

        public Config GraphConfig;
        /// <summary>
        /// 动画设置
        /// </summary>
        public class Config
        {
            /// <summary>
            /// 摸头触发位置
            /// </summary>
            public Point TouchHeadLocate;
            /// <summary>
            /// 提起触发位置
            /// </summary>
            public Point[] TouchRaisedLocate;
            /// <summary>
            /// 摸头触发大小
            /// </summary>
            public Size TouchHeadSize;
            /// <summary>
            /// 摸身体触发位置
            /// </summary>
            public Point TouchBodyLocate;
            /// <summary>
            /// 摸身体触发大小
            /// </summary>
            public Size TouchBodySize;
            /// <summary>
            /// 提起触发大小
            /// </summary>
            public Size[] TouchRaisedSize;

            /// <summary>
            /// 提起定位点
            /// </summary>
            public Point[] RaisePoint;

            /// <summary>
            /// 所有移动
            /// </summary>
            public List<Move> Moves = new List<Move>();

            /// <summary>
            /// 所有工作/学习
            /// </summary>
            public List<Work> Works = new List<Work>();

            public Line_D Str;
            /// <summary>
            /// 持续时间
            /// </summary>
            public Line_D Duration;
            /// <summary>
            /// 获取持续时间
            /// </summary>
            /// <param name="name">动画名称</param>
            /// <returns>持续时间</returns>
            public int GetDuration(string name) => Duration.GetInt(name ?? "", 10);
            /// <summary>
            /// 获得 Str 里面储存的文本 (已翻译)
            /// </summary>
            /// <param name="name">定位名称</param>
            /// <returns>储存的文本 (已翻译)</returns>
            public string StrGetString(string name) => LocalizeCore.Translate(Str.GetString(name));
            /// <summary>
            /// 剩余设置数据
            /// </summary>
            public LPS_D Data;
            /// <summary>
            /// 初始化设置
            /// </summary>
            /// <param name="lps"></param>
            public Config(LpsDocument lps)
            {
                TouchHeadLocate = new Point(lps["touchhead"][(gdbe)"px"], lps["touchhead"][(gdbe)"py"]);
                TouchHeadSize = new Size(lps["touchhead"][(gdbe)"sw"], lps["touchhead"][(gdbe)"sh"]);
                TouchBodyLocate = new Point(lps["touchbody"][(gdbe)"px"], lps["touchbody"][(gdbe)"py"]);
                TouchBodySize = new Size(lps["touchbody"][(gdbe)"sw"], lps["touchbody"][(gdbe)"sh"]);
                TouchRaisedLocate = new Point[] {
                    new Point(lps["touchraised"][(gdbe)"happy_px"], lps["touchraised"][(gdbe)"happy_py"]),
                    new Point(lps["touchraised"][(gdbe)"nomal_px"], lps["touchraised"][(gdbe)"nomal_py"]),
                    new Point(lps["touchraised"][(gdbe)"poorcondition_px"], lps["touchraised"][(gdbe)"poorcondition_py"]),
                    new Point(lps["touchraised"][(gdbe)"ill_px"], lps["touchraised"][(gdbe)"ill_py"])
                };
                TouchRaisedSize = new Size[] {
                    new Size(lps["touchraised"][(gdbe)"happy_sw"], lps["touchraised"][(gdbe)"happy_sh"]),
                    new Size(lps["touchraised"][(gdbe)"nomal_sw"], lps["touchraised"][(gdbe)"nomal_sh"]),
                    new Size(lps["touchraised"][(gdbe)"poorcondition_sw"], lps["touchraised"][(gdbe)"poorcondition_sh"]),
                    new Size(lps["touchraised"][(gdbe)"ill_sw"], lps["touchraised"][(gdbe)"ill_sh"])
                };
                RaisePoint = new Point[] {
                    new Point(lps["raisepoint"][(gdbe)"happy_x"], lps["raisepoint"][(gdbe)"happy_y"]),
                    new Point(lps["raisepoint"][(gdbe)"nomal_x"], lps["raisepoint"][(gdbe)"nomal_y"]),
                    new Point(lps["raisepoint"][(gdbe)"poorcondition_x"], lps["raisepoint"][(gdbe)"poorcondition_y"]),
                    new Point(lps["raisepoint"][(gdbe)"ill_x"], lps["raisepoint"][(gdbe)"ill_y"])
                };

                foreach (var line in lps.FindAllLine("work"))
                {
                    Works.Add(LPSConvert.DeserializeObject<Work>(line));
                }
                foreach (var line in lps.FindAllLine("move"))
                {
                    Moves.Add(LPSConvert.DeserializeObject<Move>(line));
                }
                Str = new Line_D(lps["str"]);
                Duration = new Line_D(lps["duration"]);
                Data = new LPS_D(lps);
            }
            /// <summary>
            /// 加载更多设置,新的替换后来的,允许空内容
            /// </summary>
            public void Set(LpsDocument lps)
            {
                if (lps.FindLine("touchhead") != null)
                {
                    TouchHeadLocate = new Point(lps["touchhead"][(gdbe)"px"], lps["touchhead"][(gdbe)"py"]);
                    TouchHeadSize = new Size(lps["touchhead"][(gdbe)"sw"], lps["touchhead"][(gdbe)"wh"]);
                }
                if (lps.FindLine("touchbody") != null)
                {
                    TouchBodyLocate = new Point(lps["touchbody"][(gdbe)"px"], lps["touchbody"][(gdbe)"py"]);
                    TouchBodySize = new Size(lps["touchbody"][(gdbe)"sw"], lps["touchbody"][(gdbe)"sh"]);
                }

                if (lps.FindLine("touchraised") != null)
                {
                    TouchRaisedLocate = new Point[] {
                        new Point(lps["touchraised"].GetDouble("happy_px", TouchRaisedLocate[0].X), lps["touchraised"].GetDouble("happy_py", TouchRaisedLocate[0].Y)),
                        new Point(lps["touchraised"].GetDouble("nomal_px", TouchRaisedLocate[1].X), lps["touchraised"].GetDouble("nomal_py", TouchRaisedLocate[1].Y)),
                        new Point(lps["touchraised"].GetDouble("poorcondition_px", TouchRaisedLocate[2].X), lps["touchraised"].GetDouble("poorcondition_py", TouchRaisedLocate[2].Y)),
                        new Point(lps["touchraised"].GetDouble("ill_px", TouchRaisedLocate[3].X), lps["touchraised"].GetDouble("ill_py", TouchRaisedLocate[3].Y))
                    };

                    TouchRaisedSize = new Size[] {
                        new Size(lps["touchraised"].GetDouble("happy_sw", TouchRaisedSize[0].Width), lps["touchraised"].GetDouble("happy_sh", TouchRaisedSize[0].Height)),
                        new Size(lps["touchraised"].GetDouble("nomal_sw", TouchRaisedSize[1].Width), lps["touchraised"].GetDouble("nomal_sh", TouchRaisedSize[1].Height)),
                        new Size(lps["touchraised"].GetDouble("poorcondition_sw", TouchRaisedSize[2].Width), lps["touchraised"].GetDouble("poorcondition_sh", TouchRaisedSize[2].Height)),
                        new Size(lps["touchraised"].GetDouble("ill_sw", TouchRaisedSize[3].Width), lps["touchraised"].GetDouble("ill_sh", TouchRaisedSize[3].Height))
                    };
                }
                if (lps.FindLine("raisepoint") != null)
                {
                    RaisePoint = new Point[] {
                    new Point(lps["raisepoint"].GetDouble("happy_x",RaisePoint[0].X), lps["raisepoint"].GetDouble("happy_y",RaisePoint[0].Y)),
                    new Point(lps["raisepoint"].GetDouble ("nomal_x",RaisePoint[1].X), lps["raisepoint"].GetDouble("nomal_y",RaisePoint[1].Y)),
                    new Point(lps["raisepoint"].GetDouble("poorcondition_x",RaisePoint[2].X), lps["raisepoint"].GetDouble("poorcondition_y",RaisePoint[2].Y)),
                    new Point(lps["raisepoint"].GetDouble("ill_x",RaisePoint[3].X), lps["raisepoint"].GetDouble("ill_y",RaisePoint[3].Y))};
                }

                Str.AddRange(lps["str"]);
                Duration.AddRange(lps["duration"]);

                foreach (var line in lps.FindAllLine("work"))
                {
                    Works.Add(LPSConvert.DeserializeObject<Work>(line));
                }
                foreach (var line in lps.FindAllLine("move"))
                {
                    Moves.Add(LPSConvert.DeserializeObject<Move>(line));
                }
                Data.AddRange(lps);
            }
        }

    }
}
