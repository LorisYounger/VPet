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
        }

        public void LoadEatTimeSource(IMainWindow imw)
        {
            DateTime now = DateTime.Now;
            DateTime eattime = imw.GameSavesData["buytime"].GetDateTime(Name, now);
            if (eattime <= now)
            {
                if (Type == FoodType.Meal || Type == FoodType.Snack || Type == FoodType.Drink || Type == FoodType.Gift)// || Type == FoodType.Limit)
                    Data = "喜好度".Translate();
                else
                    Data = "有效度".Translate();
                Data += ":\t100%";
            }
            else
            {
                if (Type == FoodType.Meal || Type == FoodType.Snack || Type == FoodType.Drink || Type == FoodType.Gift)// || Type == FoodType.Limit)
                    Data = "喜好度".Translate();
                else
                    Data = "有效度".Translate();
                //吃腻度的折算式子在共享后端里, 与真正结算数值时用的是同一个
                Data += ":\t" + FeedingRules.Effectiveness(
                    FeedingRules.RemainingBoredom(eattime, now), Type == FoodType.Gift).ToString("p0");
                Data += "\t\t" + "恢复".Translate() + ":\t" + (eattime).ToString("MM/dd HH");
            }
        }

    }
}
