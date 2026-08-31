using Avalonia;
using LinePutScript;
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
        public string StrGetString(string name) => Str.GetString(name) ?? string.Empty;

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
    }
}
