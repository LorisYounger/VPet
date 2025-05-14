using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace VPet_Simulator.Core
{
    public static partial class Function
    {
        /// <summary>
        /// HEX值转颜色
        /// </summary>
        /// <param name="HEX">HEX值</param>
        /// <returns>颜色</returns>
        public static Color HEXToColor(string HEX) => (Color)ColorConverter.ConvertFromString(HEX);
        /// <summary>
        /// 颜色转HEX值
        /// </summary>
        /// <param name="color">颜色</param>
        /// <returns>HEX值</returns>
        public static string ColorToHEX(Color color) => "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        public static Random Rnd = new Random();
        /// <summary>
        /// 获取资源笔刷
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Brush ResourcesBrush(BrushType name)
        {
            return (Brush)Application.Current.Resources[name.ToString()];
        }
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
        ///// <summary>
        ///// 翻译文本
        ///// </summary>
        ///// <param name="TranFile">翻译文件</param>
        ///// <param name="Name">翻译内容</param>
        ///// <returns>翻译后的文本</returns>
        //public static string Translate(this LPS_D TranFile, string Name) => TranFile.GetString(Name, Name);

        public class LPSConvertToLower : LPSConvert.ConvertFunction
        {
            public override string Convert(dynamic value) => value;

            public override dynamic ConvertBack(string info) => info.ToLower();
        }

        /// <summary>
        /// 获取内存使用情况(MB)
        /// </summary>
        public static double MemoryUsage()
        {
            return Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
        }
        public static double MemoryAvailable()
        {
            try
            {
                var d = DateTime.Now;
                var info = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024.0 / 1024.0 / 3 * 2;
                return info;
                //这个方法居然要7秒才能拿到内存数据

                //using (PerformanceCounter pc = new PerformanceCounter("Memory", "Available Bytes"))
                //{
                //    var v = pc.NextValue() / 1024.0 / 1024.0;
                //    //MessageBox.Show((DateTime.Now - d).TotalSeconds.ToString());
                //    return v;
                //}
            }
            catch
            {
                return 4000;
            }
        }
        /// <summary>
        /// 用于区分句子数量的标点符号
        /// </summary>
        public static List<char> com { get; } = new List<char> { '，', '。', '！', '？', '；', '：', '\n', '.', ',', '!', '?', ';', ':' };
        /// <summary>
        /// 计算说话内容的句子数量
        /// </summary>
        /// <param name="text">句子</param>
        public static int ComCheck(string text)
        {
            return text.Replace("\r","").Replace("\n\n", "\n").Count(com.Contains);
        }
    }
}
