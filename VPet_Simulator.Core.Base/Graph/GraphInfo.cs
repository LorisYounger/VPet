using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 动画信息
    /// </summary>
    public class GraphInfo
    {
        private static string[][]? graphTypeValue;
        public static string[][] GraphTypeValue
        {
            get
            {
                if (graphTypeValue == null)
                {
                    List<string[]> gtv = new List<string[]>();
                    foreach (string v in Enum.GetNames(typeof(GraphType)))
                    {
                        gtv.Add(v.ToLowerInvariant().Split('_'));
                    }
                    graphTypeValue = gtv.ToArray();
                }
                return graphTypeValue;
            }
        }

        public GraphInfo()
        {
        }

        public GraphInfo(string name, GraphType type = GraphType.Common, AnimatType animat = AnimatType.Single, IGameSave.ModeType modeType = IGameSave.ModeType.Nomal)
        {
            Name = name;
            Animat = animat;
            Type = type;
            ModeType = modeType;
        }

        public GraphInfo(FileSystemInfo path, ILine info)
        {
            string pn;
            if (path is DirectoryInfo)
                pn = Sub.Split(path.FullName.ToLowerInvariant(), info[(gstr)"startuppath"]!.ToLowerInvariant()).Last();
            else
                pn = Sub.Split(path.FullName.Substring(0, path.FullName.Length - path.Extension.Length).ToLowerInvariant(), info[(gstr)"startuppath"]!.ToLowerInvariant()).Last();

            var path_name = pn.Replace('\\', '_').Split('_').ToList();
            path_name.RemoveAll(string.IsNullOrWhiteSpace);
            if (!Enum.TryParse(info[(gstr)"mode"], true, out IGameSave.ModeType modetype))
            {
                if (path_name.Remove("happy"))
                    modetype = IGameSave.ModeType.Happy;
                else if (path_name.Remove("nomal"))
                    modetype = IGameSave.ModeType.Nomal;
                else if (path_name.Remove("poorcondition"))
                    modetype = IGameSave.ModeType.PoorCondition;
                else if (path_name.Remove("ill"))
                    modetype = IGameSave.ModeType.Ill;
                else
                    modetype = IGameSave.ModeType.Nomal;
            }

            if (!Enum.TryParse(info[(gstr)"graph"], true, out GraphType graphtype))
            {
                graphtype = GraphType.Common;
                for (int i = 0; i < GraphTypeValue.Length; i++)
                {
                    if (!path_name.Contains(GraphTypeValue[i][0]))
                        continue;
                    int index = path_name.IndexOf(GraphTypeValue[i][0]);
                    bool ismatch = true;
                    for (int b = 1; b < GraphTypeValue[i].Length && b + index < path_name.Count; b++)
                    {
                        if (path_name[index + b] != GraphTypeValue[i][b])
                        {
                            ismatch = false;
                            break;
                        }
                    }
                    if (ismatch)
                    {
                        graphtype = (GraphType)i;
                        path_name.RemoveRange(index, GraphTypeValue[i].Length);
                        break;
                    }
                }
            }

            if (!Enum.TryParse(info[(gstr)"animat"], true, out AnimatType animatType))
            {
                if (path_name.Remove("a") || path_name.Remove("start"))
                    animatType = AnimatType.A_Start;
                else if (path_name.Remove("b") || path_name.Remove("loop"))
                    animatType = AnimatType.B_Loop;
                else if (path_name.Remove("c") || path_name.Remove("end"))
                    animatType = AnimatType.C_End;
                else if (path_name.Remove("single"))
                    animatType = AnimatType.Single;
                else
                    animatType = AnimatType.Single;
            }

            Name = info.Info;
            if (string.IsNullOrWhiteSpace(Name))
            {
                while (path_name.Count > 0 && (double.TryParse(path_name.Last(), out _) || path_name.Last().StartsWith("~")))
                {
                    path_name.RemoveAt(path_name.Count - 1);
                }
                if (path_name.Count > 0)
                    Name = path_name.Last();
            }
            if (string.IsNullOrWhiteSpace(Name))
                Name = graphtype.ToString().ToLowerInvariant();

            Type = graphtype;
            Animat = animatType;
            ModeType = modetype;
        }

        public enum GraphType
        {
            Common,
            Raised_Dynamic,
            Raised_Static,
            Move,
            Default,
            Touch_Head,
            Touch_Body,
            Idel,
            Sleep,
            Say,
            StateONE,
            StateTWO,
            StartUP,
            Shutdown,
            Work,
            Switch_Up,
            Switch_Down,
            Switch_Thirsty,
            Switch_Hunger,
            SideHide_Left_Main,
            SideHide_Left_Rise,
            SideHide_Right_Main,
            SideHide_Right_Rise,
        }

        public enum AnimatType
        {
            Single,
            A_Start,
            B_Loop,
            C_End,
        }

        public string Name { get; set; } = "";
        public AnimatType Animat { get; set; }
        public GraphType Type { get; set; }
        public IGameSave.ModeType ModeType { get; set; }

        public override string ToString()
        {
            return $"[{Name}]{Type}_{ModeType.ToString()[0]}{Animat.ToString()[0]}]";
        }
    }
}
