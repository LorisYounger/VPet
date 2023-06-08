using System;
using System.Windows;
using System.Windows.Media;

namespace VPet_Simulator.Core
{
    public class Function
    {
        public static Random Rnd = new Random();
        /// <summary>
        /// 获取资源笔刷
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Brush ResourcesBrush(BrushType name)
        {
            return (Brush)Application.Current.Resources.MergedDictionaries[0].MergedDictionaries[0][name.ToString()];
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
    }
}
