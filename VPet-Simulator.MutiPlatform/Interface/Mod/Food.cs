//跨平台: 原文复制自 VPet-Simulator.Windows.Interface/Mod/Food.cs; 只换了本地化的命名空间
using LinePutScript.Localization;
using System;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 食物的跨平台半
    /// </summary>
    /// 只剩两个要 IMainWindow 的方法: 一个解位图, 一个从存档里读吃腻度.
    public partial class Food
    {
        /// <summary>
        /// 加载物品图片
        /// </summary>
        public void LoadImageSource(IMainWindow imw)
        {
            ImageSource = imw.ImageSources.FindImage("food_" + (Image ?? Name), "food");
            ImagePath = imw.ImageSources.FindSource("food_" + (Image ?? Name)) ?? imw.ImageSources.FindSource("food");
            Star = imw.Set["betterbuy"]["star"].GetInfos().Contains(Name);
            LoadEatTimeSource(imw);
            CalRealPrice(imw);
        }



    }
}
