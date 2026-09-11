using Avalonia;
using LinePutScript;
using LinePutScript.Localization;
using LinePutScript.Converter;
using LinePutScript.Dictionary;
using System.Collections.Generic;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

public partial class GraphCore
{
    public Config? GraphConfig;

    public class Config
    {
        public Point TouchHeadLocate;
        public Point[] TouchRaisedLocate = new Point[4];
        public Size TouchHeadSize;
        public Point TouchBodyLocate;
        public Size TouchBodySize;
        public Size[] TouchRaisedSize = new Size[4];
        public Point[] RaisePoint = new Point[4];

        public List<VPet_Simulator.Core.MutiPlatform.GraphHelper.Work> Works = new();
        public List<VPet_Simulator.Core.MutiPlatform.GraphHelper.Move> Moves = new();

        public Line_D Str;
        public Line_D Duration;
        public LPS_D Data;

        public int GetDuration(string? name) => Duration.GetInt(name ?? string.Empty, 10);
        // 与 Windows 版 GraphCore.Config.StrGetString 一致: 宠物配置里的界面文案要过翻译
        public string StrGetString(string name) => LocalizeCore.Translate(Str.GetString(name) ?? string.Empty);

        public Config(LpsDocument lps)
        {
            TouchHeadLocate = new Point(lps["touchhead"][(gdbe)"px"], lps["touchhead"][(gdbe)"py"]);
            TouchHeadSize = new Size(lps["touchhead"][(gdbe)"sw"], lps["touchhead"][(gdbe)"sh"]);
            TouchBodyLocate = new Point(lps["touchbody"][(gdbe)"px"], lps["touchbody"][(gdbe)"py"]);
            TouchBodySize = new Size(lps["touchbody"][(gdbe)"sw"], lps["touchbody"][(gdbe)"sh"]);
            TouchRaisedLocate =
            [
                new Point(lps["touchraised"][(gdbe)"happy_px"], lps["touchraised"][(gdbe)"happy_py"]),
                new Point(lps["touchraised"][(gdbe)"nomal_px"], lps["touchraised"][(gdbe)"nomal_py"]),
                new Point(lps["touchraised"][(gdbe)"poorcondition_px"], lps["touchraised"][(gdbe)"poorcondition_py"]),
                new Point(lps["touchraised"][(gdbe)"ill_px"], lps["touchraised"][(gdbe)"ill_py"])
            ];
            TouchRaisedSize =
            [
                new Size(lps["touchraised"][(gdbe)"happy_sw"], lps["touchraised"][(gdbe)"happy_sh"]),
                new Size(lps["touchraised"][(gdbe)"nomal_sw"], lps["touchraised"][(gdbe)"nomal_sh"]),
                new Size(lps["touchraised"][(gdbe)"poorcondition_sw"], lps["touchraised"][(gdbe)"poorcondition_sh"]),
                new Size(lps["touchraised"][(gdbe)"ill_sw"], lps["touchraised"][(gdbe)"ill_sh"])
            ];
            RaisePoint =
            [
                new Point(lps["raisepoint"][(gdbe)"happy_x"], lps["raisepoint"][(gdbe)"happy_y"]),
                new Point(lps["raisepoint"][(gdbe)"nomal_x"], lps["raisepoint"][(gdbe)"nomal_y"]),
                new Point(lps["raisepoint"][(gdbe)"poorcondition_x"], lps["raisepoint"][(gdbe)"poorcondition_y"]),
                new Point(lps["raisepoint"][(gdbe)"ill_x"], lps["raisepoint"][(gdbe)"ill_y"])
            ];

            foreach (var line in lps.FindAllLine("work"))
            {
                var work = LPSConvert.DeserializeObject<VPet_Simulator.Core.MutiPlatform.GraphHelper.Work>(line);
                if (work != null)
                    Works.Add(work);
            }
            foreach (var line in lps.FindAllLine("move"))
            {
                var move = LPSConvert.DeserializeObject<VPet_Simulator.Core.MutiPlatform.GraphHelper.Move>(line);
                if (move != null)
                    Moves.Add(move);
            }
            Str = new Line_D(lps["str"]);
            Duration = new Line_D(lps["duration"]);
            Data = new LPS_D(lps);
        }

        /// <summary>
        /// 加载更多设置,新的替换后来的,允许空内容
        /// </summary>
        /// MOD 可以只覆盖其中几项设置, 所以这里对每一段都要先判断存在再取值,
        /// 取值时以当前值作为默认值. 与 Windows 版逐行对应, 不要"简化".
        public void Set(LpsDocument lps)
        {
            if (lps.FindLine("touchhead") != null && lps["touchhead"][(gdbe)"py"] != 0)
            {
                TouchHeadLocate = new Point(lps["touchhead"][(gdbe)"px"], lps["touchhead"][(gdbe)"py"]);
                TouchHeadSize = new Size(lps["touchhead"][(gdbe)"sw"], lps["touchhead"][(gdbe)"sh"]);
            }
            if (lps.FindLine("touchbody") != null && lps["touchbody"][(gdbe)"py"] != 0)
            {
                TouchBodyLocate = new Point(lps["touchbody"][(gdbe)"px"], lps["touchbody"][(gdbe)"py"]);
                TouchBodySize = new Size(lps["touchbody"][(gdbe)"sw"], lps["touchbody"][(gdbe)"sh"]);
            }

            if (lps.FindLine("touchraised") != null)
            {
                if (lps["touchraised"][(gdbe)"happy_py"] != 0)
                    TouchRaisedLocate =
                    [
                        new Point(lps["touchraised"].GetDouble("happy_px", TouchRaisedLocate[0].X), lps["touchraised"].GetDouble("happy_py", TouchRaisedLocate[0].Y)),
                        new Point(lps["touchraised"].GetDouble("nomal_px", TouchRaisedLocate[1].X), lps["touchraised"].GetDouble("nomal_py", TouchRaisedLocate[1].Y)),
                        new Point(lps["touchraised"].GetDouble("poorcondition_px", TouchRaisedLocate[2].X), lps["touchraised"].GetDouble("poorcondition_py", TouchRaisedLocate[2].Y)),
                        new Point(lps["touchraised"].GetDouble("ill_px", TouchRaisedLocate[3].X), lps["touchraised"].GetDouble("ill_py", TouchRaisedLocate[3].Y))
                    ];
                if (lps["touchraised"][(gdbe)"happy_sh"] != 0)
                    TouchRaisedSize =
                    [
                        new Size(lps["touchraised"].GetDouble("happy_sw", TouchRaisedSize[0].Width), lps["touchraised"].GetDouble("happy_sh", TouchRaisedSize[0].Height)),
                        new Size(lps["touchraised"].GetDouble("nomal_sw", TouchRaisedSize[1].Width), lps["touchraised"].GetDouble("nomal_sh", TouchRaisedSize[1].Height)),
                        new Size(lps["touchraised"].GetDouble("poorcondition_sw", TouchRaisedSize[2].Width), lps["touchraised"].GetDouble("poorcondition_sh", TouchRaisedSize[2].Height)),
                        new Size(lps["touchraised"].GetDouble("ill_sw", TouchRaisedSize[3].Width), lps["touchraised"].GetDouble("ill_sh", TouchRaisedSize[3].Height))
                    ];
            }
            if (lps.FindLine("raisepoint") != null && lps["raisepoint"][(gdbe)"happy_y"] != 0)
            {
                RaisePoint =
                [
                    new Point(lps["raisepoint"].GetDouble("happy_x", RaisePoint[0].X), lps["raisepoint"].GetDouble("happy_y", RaisePoint[0].Y)),
                    new Point(lps["raisepoint"].GetDouble("nomal_x", RaisePoint[1].X), lps["raisepoint"].GetDouble("nomal_y", RaisePoint[1].Y)),
                    new Point(lps["raisepoint"].GetDouble("poorcondition_x", RaisePoint[2].X), lps["raisepoint"].GetDouble("poorcondition_y", RaisePoint[2].Y)),
                    new Point(lps["raisepoint"].GetDouble("ill_x", RaisePoint[3].X), lps["raisepoint"].GetDouble("ill_y", RaisePoint[3].Y))
                ];
            }

            Str.AddRange(lps["str"]);
            Duration.AddRange(lps["duration"]);

            foreach (var line in lps.FindAllLine("work"))
            {
                var work = LPSConvert.DeserializeObject<VPet_Simulator.Core.MutiPlatform.GraphHelper.Work>(line);
                if (work != null)
                    Works.Add(work);
            }
            foreach (var line in lps.FindAllLine("move"))
            {
                var move = LPSConvert.DeserializeObject<VPet_Simulator.Core.MutiPlatform.GraphHelper.Move>(line);
                if (move != null)
                    Moves.Add(move);
            }
            foreach (var line in lps)
            {
                if (!string.IsNullOrEmpty(line.info))
                    Data.Add(line);
            }
        }
    }
}
