using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Core.MutiPlatform;

public static class GraphHelper
{
    public class Work : ICloneable
    {
        public enum WorkType { Work, Study, Play }
        [Line(ignoreCase: true)]
        public WorkType Type { get; set; }
        [Line(ignoreCase: true)]
        public string Name { get; set; } = string.Empty;
        [Line(ignoreCase: true)]
        public string Graph { get; set; } = string.Empty;
        [Line(ignoreCase: true)]
        public double MoneyBase { get; set; }
        [Line(ignoreCase: true)]
        public double StrengthFood { get; set; }
        [Line(ignoreCase: true)]
        public double StrengthDrink { get; set; }
        [Line(ignoreCase: true)]
        public double Feeling { get; set; }
        [Line(ignoreCase: true)]
        public int LevelLimit { get; set; }
        [Line(ignoreCase: true)]
        public int Time { get; set; }
        [Line(ignoreCase: true)]
        public double FinishBonus { get; set; }

        public object Clone() => MemberwiseClone();
    }

    public class Move
    {
        [Line(ignoreCase: true)]
        public string Graph { get; set; } = string.Empty;
    }
}
