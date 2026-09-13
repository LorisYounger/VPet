using Avalonia;
using Avalonia.Media;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace VPet_Simulator.Core.MutiPlatform;

public static class Function
{
    public static Color HEXToColor(string hex) => Color.Parse(hex);

    public static string ColorToHEX(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static Random Rnd = new();

    public static IBrush? ResourcesBrush(string name)
    {
        if (Application.Current?.Resources.TryGetResource(name, null, out var value) == true)
        {
            return value as IBrush;
        }
        return null;
    }
    /// <summary>
    /// 获取资源笔刷
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IBrush? ResourcesBrush(BrushType name) => ResourcesBrush(name.ToString());
    public enum BrushType
    {
        Primary,
        PrimaryTrans,
        PrimaryTrans4,
        PrimaryTransA,
        PrimaryTransE,
        PrimaryLight,
        PrimaryLighter,
        PrimaryDark,
        PrimaryDarker,
        PrimaryText,

        Secondary,
        SecondaryTrans,
        SecondaryTrans4,
        SecondaryTransA,
        SecondaryTransE,
        SecondaryLight,
        SecondaryLighter,
        SecondaryDark,
        SecondaryDarker,
        SecondaryText,

        DARKPrimary,
        DARKPrimaryTrans,
        DARKPrimaryTrans4,
        DARKPrimaryTransA,
        DARKPrimaryTransE,
        DARKPrimaryLight,
        DARKPrimaryLighter,
        DARKPrimaryDark,
        DARKPrimaryDarker,
        DARKPrimaryText,
    }

    public class LPSConvertToLower : LPSConvert.ConvertFunction
    {
        public override string Convert(dynamic value) => value;
        public override dynamic ConvertBack(string info) => info.ToLowerInvariant();
    }

    public static double MemoryUsage() => Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;

    public static double MemoryAvailable()
    {
        try
        {
            return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024.0 / 1024.0 / 3 * 2;
        }
        catch
        {
            return 4000;
        }
    }

    public static List<char> ComChars { get; } = ['，', '。', '！', '？', '；', '：', '\n', '.', ',', '!', '?', ';', ':'];

    public static int ComCheck(string text)
    {
        return text.Replace("\r", string.Empty).Replace("\n\n", "\n").Count(ComChars.Contains);
    }
}
