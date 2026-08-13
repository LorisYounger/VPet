using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Windows.Interface.Photo.UnlockCondition;

namespace VPet_Simulator.Windows;

// 一些桌宠功能的实现函数
public partial class MainWindow : IMainWindow
{
    /// <summary>
    /// 上次吃食物的时间
    /// </summary>
    public DateTime LastTakeItemTime { get; set; } = DateTime.MinValue;
    /// <summary>
    /// 官方带有特效的食用食物
    /// </summary>
    /// <param name="obj"></param>
    private void MainWindow_Event_TakeItem(Food obj)
    {
        switch (obj.Name)
        {
            case "生日蛋糕2":
                //更新下生日蛋糕的属性和价格
                obj.Exp = GameSavesData.GameSave!.Level;
                obj.Likability = GameSavesData.GameSave!.LikabilityMax / 20;
                obj.StrengthDrink = GameSavesData.GameSave!.StrengthMax / 20;
                obj.StrengthFood = GameSavesData.GameSave!.StrengthMax / 20;
                obj.isoverload = false;
                obj.Price = (int)Math.Max(0, obj.RealPrice * .5);
                switch (Function.Rnd.Next(15))
                {
                    case 1:
                    case 2:
                    case 3:
                        GameSavesData.GameSave!.Strength = GameSavesData.GameSave!.StrengthMax;
                        Main.LabelDisplayShow("{0}充满抛瓦!".Translate(GameSavesData.GameSave!.Name), 3000);
                        break;
                    case 4:
                    case 5:
                        GameSavesData.GameSave!.Feeling = GameSavesData.GameSave!.FeelingMax;
                        Main.LabelDisplayShow("{0}今天也是好心情!".Translate(GameSavesData.GameSave!.Name), 3000);
                        break;
                    case 6:
                    case 7:
                        GameSavesData.GameSave!.StrengthFood = GameSavesData.GameSave!.StrengthMax;
                        Main.LabelDisplayShow("{0}吃饱了!".Translate(GameSavesData.GameSave!.Name), 3000);
                        break;
                    case 8:
                    case 9:
                        GameSavesData.GameSave!.StrengthDrink = GameSavesData.GameSave!.StrengthMax;
                        Main.LabelDisplayShow("{0}加满水了!".Translate(GameSavesData.GameSave!.Name), 3000);
                        break;
                    case 10:
                        int get = (Function.Rnd.Next(GameSavesData.GameSave!.LevelUpNeed() * (GameSavesData.GameSave.LevelMax + 1)) / 200 + 1) * 100;
                        GameSavesData.GameSave!.Exp += get;
                        Main.LabelDisplayShow("{0}经验 +{1} 告辞".Translate(GameSavesData.GameSave!.Name, get.ToString("N0")), 4000);
                        break;
                    case 11:
                        get = (Function.Rnd.Next(GameSavesData.GameSave!.LevelUpNeed() * (GameSavesData.GameSave.LevelMax + 1)) / 500 + 1) * 10;
                        GameSavesData.GameSave!.Exp += get;
                        Main.LabelDisplayShow("{0}在马路边捡到{1}金钱".Translate(GameSavesData.GameSave!.Name, get.ToString("N0")), 4000);
                        break;
                    case 12:
                        if (Function.Rnd.Next(3) != 0)
                        {//再随一次, 给好感度
                            get = Function.Rnd.Next((int)GameSavesData.GameSave!.LikabilityMax / 25) + 1;
                            GameSavesData.GameSave!.Likability += get;
                            Main.LabelDisplayShow("{0}更喜欢{1}了".Translate(GameSavesData.GameSave!.Name, GameSavesData.GameSave!.HostName), 4000);
                            break;
                        }
                        var photos = Photos.FindAll(x => x.IsUnlock == false && x.UnlockAble.Lock == false);
                        if (photos.Count > 0)
                        {
                            var tempphoto = photos.FindAll(x => x.UnlockAble.Time != null || x.UnlockAble.Date != null || x.UnlockAble.Holiday != HolidayType.None);
                            if (tempphoto.Count > 0)//优先解锁时间/日期/节日的照片
                                photos = tempphoto;
                            else
                            {
                                tempphoto = photos.FindAll(x => x.UnlockAble.SellBoth == false && (x.UnlockAble.Feeling > 10 || x.UnlockAble.Likability >= 10 || x.UnlockAble.Money >= 10));
                                if (tempphoto.Count > 0)//然后解锁好感度/金钱/饱腹/口渴的照片
                                    photos = tempphoto;
                            }

                            var photo = photos[Function.Rnd.Next(photos.Count)];
                            photo.Unlock(this);
                            Main.LabelDisplayShow("{0}收到了新照片".Translate(GameSavesData.GameSave!.Name) + '\n' + photo.Name, 4000);
                        }
                        else
                            goto case 11;
                        break;
                    default:
                        Main.LabelDisplayShow("{0}获得了谢谢惠顾".Translate(GameSavesData.GameSave!.Name), 4000);
                        break;
                }
                break;

            case "生日蛋糕3":
                if (LastTakeItemTime.AddSeconds(5) > DateTime.Now)
                    break;//避免频繁触发
                string Question;
                bool IsTrue;
                var stats = GameSavesData.Statistics;
                var hostName = string.IsNullOrWhiteSpace(GameSavesData.GameSave.HostName) ? Environment.UserName : GameSavesData.GameSave.HostName;

                int GetBuyCount(Food food)
                {
                    if (stats.Data.TryGetValue($"buy_{food.Name}", out var value) && value != null)
                        return (int)value;
                    return 0;
                }

                string GetMostBoughtFoodName(Food.FoodType? type = null)
                {
                    var targetFoods = type == null ? Foods : Foods.Where(x => x.Type == type.Value);
                    return targetFoods.OrderByDescending(GetBuyCount).FirstOrDefault()?.TranslateName ?? "";//TODO: 无对应购买记录时的展示文案
                }

                string ConvertQuestionText(string text)
                {
                    return IText.ConverText(text.Replace("{hostname}", hostName), Main);
                }

                switch (Function.Rnd.Next(95))
                {
                    case 0:
                        Question = IText.ConverText("{name}今年一岁啦".Translate(), Main);
                        IsTrue = false;
                        break;
                    case 1:
                        Question = IText.ConverText("{name}今年两岁啦".Translate(), Main);
                        IsTrue = false;
                        break;
                    case 2:
                        Question = IText.ConverText("{name}今年三岁啦".Translate(), Main);
                        IsTrue = true;
                        break;
                    case 3:
                        Question = IText.ConverText("{name}今年两岁啦".Translate(), Main);
                        IsTrue = false;
                        break;
                    case 4:
                        Question = IText.ConverText("{name}的生日是8月13日".Translate(), Main);
                        IsTrue = false;
                        break;
                    case 5:
                        Question = IText.ConverText("{name}的生日是8月14日".Translate(), Main);
                        IsTrue = true;
                        break;
                    case 6:
                        Question = IText.ConverText("{name}的生日是7月14日".Translate(), Main);
                        IsTrue = false;
                        break;
                    case 7:
                        Question = IText.ConverText("{name}的生日是3月25日".Translate(), Main);
                        IsTrue = false;
                        break;
                    case 8:
                        Question = IText.ConverText("{name}没开过生日会".Translate(), Main);
                        IsTrue = false;
                        break;
                    case 9:
                        Question = IText.ConverText("{name}生日会有很多二创作品".Translate(), Main);
                        IsTrue = true;
                        break;
                    case 10:
                        Question = IText.ConverText("{name}生日会在b站直播".Translate(), Main);
                        IsTrue = true;
                        break;
                    case 11:
                        Question = "更好买有1种生日蛋糕".Translate();
                        IsTrue = false;
                        break;
                    case 12:
                        Question = "更好买有2种生日蛋糕".Translate();
                        IsTrue = false;
                        break;
                    case 13:
                        Question = "更好买有3种生日蛋糕".Translate();
                        IsTrue = true;
                        break;
                    case 14:
                        Question = "生日蛋糕在更好买收藏里".Translate();
                        IsTrue = true;
                        break;
                    case 15:
                        Question = "生日蛋糕在更好买礼品里".Translate();
                        IsTrue = false;
                        break;
                    case 16:
                        Question = "一周年的生日蛋糕叫“全回复生日蛋糕”".Translate();
                        IsTrue = true;
                        break;
                    case 17:
                        Question = "二周年的生日蛋糕叫“惊喜生日蛋糕”".Translate();
                        IsTrue = true;
                        break;
                    case 18:
                        Question = "LBGame制作组（虚拟桌宠模拟器的制作组）是一个效率高，产能大，更新快，不画饼，永不跳票的好制作组。".Translate();
                        IsTrue = false;
                        break;
                    case 19:
                        Question = "桌宠的DLC是宅女".Translate();
                        IsTrue = true;
                        break;
                    case 20:
                        Question = "桌宠的DLC是小恶魔".Translate();
                        IsTrue = false;
                        break;
                    case 21:
                        Question = "桌宠的DLC是女仆".Translate();
                        IsTrue = false;
                        break;
                    case 22:
                        Question = "桌宠的DLC是JK".Translate();
                        IsTrue = false;
                        break;
                    case 23:
                        Question = IText.ConverText("在面板详细里可以查看{name}的统计面板".Translate(), Main);
                        IsTrue = true;
                        break;
                    case 24:
                        Question = IText.ConverText("在面板详细里可以查看{name}的活动日志".Translate(), Main);
                        IsTrue = true;
                        break;
                    case 25:
                        Question = IText.ConverText("在面板详细里可以生成{name}的统计总结".Translate(), Main);
                        IsTrue = true;
                        break;
                    case 26:
                        Question = IText.ConverText("在面板详细里可以生成{name}的生日祝福".Translate(), Main);
                        IsTrue = true;
                        break;
                    case 27:
                        Question = IText.ConverText("想使用日程表来让{name}自动学习要签署培训机构".Translate(), Main);
                        IsTrue = true;
                        break;
                    case 28:
                        Question = IText.ConverText("想使用日程表来让{name}自动工作要签署工作中介".Translate(), Main);
                        IsTrue = true;
                        break;
                    default:
                    case 29:
                        Question = "在互动里可以打开访客表功能邀请其他桌宠来玩".Translate();
                        IsTrue = true;
                        break;
                    case 30:
                        Question = ConvertQuestionText("你的桌宠名叫{name}".Translate());
                        IsTrue = true;
                        break;
                    case 31:
                        Question = string.Format(ConvertQuestionText("{name}目前已经升到{0}级啦".Translate()), GameSavesData.GameSave.Level);
                        IsTrue = true;
                        break;
                    case 32:
                        Question = string.Format(ConvertQuestionText("{name}目前已经升到{0}级啦".Translate()), GameSavesData.GameSave.Level * 10);
                        IsTrue = false;
                        break;
                    case 33:
                        Question = string.Format(ConvertQuestionText("{name}目前已经升到{0}级啦".Translate()), (GameSavesData.GameSave.Level / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 34:
                        Question = string.Format(ConvertQuestionText("{name}当前的饱食度为{0}".Translate()), GameSavesData.GameSave.StrengthFood.ToString("f1"));
                        IsTrue = true;
                        break;
                    case 35:
                        Question = string.Format(ConvertQuestionText("{name}当前的饱食度为{0}".Translate()), (GameSavesData.GameSave.StrengthFood * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 36:
                        Question = string.Format(ConvertQuestionText("{name}当前的饱食度为{0}".Translate()), (GameSavesData.GameSave.StrengthFood / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 37:
                        Question = string.Format(ConvertQuestionText("{name}当前的口渴度为{0}".Translate()), GameSavesData.GameSave.StrengthDrink.ToString("f1"));
                        IsTrue = true;
                        break;
                    case 38:
                        Question = string.Format(ConvertQuestionText("{name}当前的口渴度为{0}".Translate()), (GameSavesData.GameSave.StrengthDrink * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 39:
                        Question = string.Format(ConvertQuestionText("{name}当前的口渴度为{0}".Translate()), (GameSavesData.GameSave.StrengthDrink / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 40:
                        Question = string.Format(ConvertQuestionText("{name}在更好买中购买最多的是{0}".Translate()), GetMostBoughtFoodName());
                        IsTrue = true;
                        break;
                    case 41:
                        Question = string.Format(ConvertQuestionText("{name}最常购买的正餐是{0}".Translate()), GetMostBoughtFoodName(Food.FoodType.Meal));
                        IsTrue = true;
                        break;
                    case 42:
                        Question = string.Format(ConvertQuestionText("{name}最常购买的零食是{0}".Translate()), GetMostBoughtFoodName(Food.FoodType.Snack));
                        IsTrue = true;
                        break;
                    case 43:
                        Question = string.Format(ConvertQuestionText("{name}最常购买的饮料是{0}".Translate()), GetMostBoughtFoodName(Food.FoodType.Drink));
                        IsTrue = true;
                        break;
                    case 44:
                        Question = string.Format(ConvertQuestionText("{name}最常购买的功能性物品是{0}".Translate()), GetMostBoughtFoodName(Food.FoodType.Functional));
                        IsTrue = true;
                        break;
                    case 45:
                        Question = string.Format(ConvertQuestionText("{name}最常购买的药品是{0}".Translate()), GetMostBoughtFoodName(Food.FoodType.Drug));
                        IsTrue = true;
                        break;
                    case 46:
                        Question = string.Format(ConvertQuestionText("{name}最常购买的礼品是{0}".Translate()), GetMostBoughtFoodName(Food.FoodType.Gift));
                        IsTrue = true;
                        break;
                    case 47:
                        Question = string.Format(ConvertQuestionText("{name}一共在饮料上花费了{0}".Translate()), ((double)stats[(gdbe)"stat_bb_drink"]).ToString("f1"));
                        IsTrue = true;
                        break;
                    case 48:
                        Question = string.Format(ConvertQuestionText("{name}一共在饮料上花费了{0}".Translate()), (((double)stats[(gdbe)"stat_bb_drink"]) * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 49:
                        Question = string.Format(ConvertQuestionText("{name}一共在饮料上花费了{0}".Translate()), (((double)stats[(gdbe)"stat_bb_drink"]) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 50:
                        Question = string.Format(ConvertQuestionText("{name}一共在药品上花费了{0}".Translate()), ((double)stats[(gdbe)"stat_bb_drug"]).ToString("f0"));
                        IsTrue = true;
                        break;
                    case 51:
                        Question = string.Format(ConvertQuestionText("{name}一共在药品上花费了{0}".Translate()), (((double)stats[(gdbe)"stat_bb_drug"]) * 10).ToString("f0"));
                        IsTrue = false;
                        break;
                    case 52:
                        Question = string.Format(ConvertQuestionText("{name}一共在药品上花费了{0}".Translate()), (((double)stats[(gdbe)"stat_bb_drug"]) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 53:
                        Question = string.Format(ConvertQuestionText("{name}通过药品获得了{0}点经验".Translate()), ((double)stats[(gdbe)"stat_bb_drug_exp"]).ToString("f1"));
                        IsTrue = true;
                        break;
                    case 54:
                        Question = string.Format(ConvertQuestionText("{name}通过药品获得了{0}点经验".Translate()), (((double)stats[(gdbe)"stat_bb_drug_exp"]) * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 55:
                        Question = string.Format(ConvertQuestionText("{name}通过药品获得了{0}点经验".Translate()), (((double)stats[(gdbe)"stat_bb_drug_exp"]) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 56:
                        Question = string.Format(ConvertQuestionText("{name}一共在礼物上花费了{0}".Translate()), ((double)stats[(gdbe)"stat_bb_gift"]).ToString("f1"));
                        IsTrue = true;
                        break;
                    case 57:
                        Question = string.Format(ConvertQuestionText("{name}一共在礼物上花费了{0}".Translate()), (((double)stats[(gdbe)"stat_bb_gift"]) * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 58:
                        Question = string.Format(ConvertQuestionText("{name}一共在礼物上花费了{0}".Translate()), (((double)stats[(gdbe)"stat_bb_gift"]) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 59:
                        Question = string.Format(ConvertQuestionText("{name}一共在正餐上花费了{0}".Translate()), ((double)stats[(gdbe)"stat_bb_meal"]).ToString("f1"));
                        IsTrue = true;
                        break;
                    case 60:
                        Question = string.Format(ConvertQuestionText("{name}一共在正餐上花费了{0}".Translate()), (((double)stats[(gdbe)"stat_bb_meal"]) * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 61:
                        Question = string.Format(ConvertQuestionText("{name}一共在正餐上花费了{0}".Translate()), (((double)stats[(gdbe)"stat_bb_meal"]) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 62:
                        Question = string.Format(ConvertQuestionText("{name}一共在零食上花费了{0}".Translate()), ((double)stats[(gdbe)"stat_bb_snack"]).ToString("f1"));
                        IsTrue = true;
                        break;
                    case 63:
                        Question = string.Format(ConvertQuestionText("{name}一共在零食上花费了{0}".Translate()), (((double)stats[(gdbe)"stat_bb_snack"]) * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 64:
                        Question = string.Format(ConvertQuestionText("{name}一共在零食上花费了{0}".Translate()), (((double)stats[(gdbe)"stat_bb_snack"]) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 65:
                        Question = string.Format(ConvertQuestionText("{name}在更好买中累计花费了{0}".Translate()), ((double)stats[(gdbe)"stat_betterbuy"]).ToString("f0"));
                        IsTrue = true;
                        break;
                    case 66:
                        Question = string.Format(ConvertQuestionText("{name}在更好买中累计花费了{0}".Translate()), (((double)stats[(gdbe)"stat_betterbuy"]) * 10).ToString("f0"));
                        IsTrue = false;
                        break;
                    case 67:
                        Question = string.Format(ConvertQuestionText("{name}在更好买中累计花费了{0}".Translate()), (((double)stats[(gdbe)"stat_betterbuy"]) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 68:
                        Question = string.Format(ConvertQuestionText("{name}已经移动了足足{0}".Translate()), winCharacterPanel.px_tocm(stats[(gi64)"stat_move_length"]));
                        IsTrue = true;
                        break;
                    case 69:
                        Question = string.Format(ConvertQuestionText("{name}已经移动了足足{0}".Translate()), winCharacterPanel.px_tocm(stats[(gi64)"stat_move_length"] * 100));
                        IsTrue = false;
                        break;
                    case 70:
                        Question = string.Format(ConvertQuestionText("{name}已经移动了足足{0}".Translate()), winCharacterPanel.px_tocm(stats[(gi64)"stat_move_length"] / 100));
                        IsTrue = false;
                        break;
                    case 71:
                        Question = string.Format(ConvertQuestionText("{name}跟着音乐跳了{0}次舞".Translate()), stats[(gint)"stat_music"]);
                        IsTrue = true;
                        break;
                    case 72:
                        Question = string.Format(ConvertQuestionText("{name}跟着音乐跳了{0}次舞".Translate()), (stats[(gint)"stat_music"]) * 10);
                        IsTrue = false;
                        break;
                    case 73:
                        Question = string.Format(ConvertQuestionText("{name}跟着音乐跳了{0}次舞".Translate()), (stats[(gint)"stat_music"]) / 10);
                        IsTrue = false;
                        break;
                    case 74:
                        Question = string.Format(ConvertQuestionText("{hostname}一共启动了{name}所在的游戏{0}次".Translate()), stats[(gint)"stat_open_times"]);
                        IsTrue = true;
                        break;
                    case 75:
                        Question = string.Format(ConvertQuestionText("{hostname}一共启动了{name}所在的游戏{0}次".Translate()), (stats[(gint)"stat_open_times"]) * 10);
                        IsTrue = false;
                        break;
                    case 76:
                        Question = string.Format(ConvertQuestionText("{hostname}一共启动了{name}所在的游戏{0}次".Translate()), (stats[(gint)"stat_open_times"]) / 10);
                        IsTrue = false;
                        break;
                    case 77:
                        Question = string.Format(ConvertQuestionText("{name}一共睡了{0}小时".Translate()), (stats[(gint)"stat_sleep_time"] / 3600.0).ToString("f1"));
                        IsTrue = true;
                        break;
                    case 78:
                        Question = string.Format(ConvertQuestionText("{name}一共睡了{0}小时".Translate()), ((stats[(gint)"stat_sleep_time"] / 3600.0) * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 79:
                        Question = string.Format(ConvertQuestionText("{name}一共睡了{0}小时".Translate()), ((stats[(gint)"stat_sleep_time"] / 3600.0) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 80:
                        Question = string.Format(ConvertQuestionText("{name}认真学习了{0}小时".Translate()), (stats[(gint)"stat_study_time"] / 3600.0).ToString("f1"));
                        IsTrue = true;
                        break;
                    case 81:
                        Question = string.Format(ConvertQuestionText("{name}认真学习了{0}小时".Translate()), ((stats[(gint)"stat_study_time"] / 3600.0) * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 82:
                        Question = string.Format(ConvertQuestionText("{name}认真学习了{0}小时".Translate()), ((stats[(gint)"stat_study_time"] / 3600.0) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 83:
                        Question = string.Format(ConvertQuestionText("{hostname}已经陪伴{name}度过了{0}小时".Translate()), (stats[(gint)"stat_total_time"] / 3600.0).ToString("f1"));
                        IsTrue = true;
                        break;
                    case 84:
                        Question = string.Format(ConvertQuestionText("{hostname}已经陪伴{name}度过了{0}小时".Translate()), ((stats[(gint)"stat_total_time"] / 3600.0) * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 85:
                        Question = string.Format(ConvertQuestionText("{hostname}已经陪伴{name}度过了{0}小时".Translate()), ((stats[(gint)"stat_total_time"] / 3600.0) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 86:
                        Question = string.Format(ConvertQuestionText("{name}被{hostname}摸了{0}次身体".Translate()), stats[(gint)"stat_touch_body"]);
                        IsTrue = true;
                        break;
                    case 87:
                        Question = string.Format(ConvertQuestionText("{name}被{hostname}摸了{0}次身体".Translate()), ((int)stats[(gint)"stat_touch_body"]) * 10);
                        IsTrue = false;
                        break;
                    case 88:
                        Question = string.Format(ConvertQuestionText("{name}被{hostname}摸了{0}次身体".Translate()), (((int)stats[(gint)"stat_touch_body"]) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 89:
                        Question = string.Format(ConvertQuestionText("{name}被{hostname}摸了{0}次头".Translate()), stats[(gint)"stat_touch_head"]);
                        IsTrue = true;
                        break;
                    case 90:
                        Question = string.Format(ConvertQuestionText("{name}被{hostname}摸了{0}次头".Translate()), ((int)stats[(gint)"stat_touch_head"]) * 10);
                        IsTrue = false;
                        break;
                    case 91:
                        Question = string.Format(ConvertQuestionText("{name}被{hostname}摸了{0}次头".Translate()), (((int)stats[(gint)"stat_touch_head"]) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 92:
                        Question = string.Format(ConvertQuestionText("{name}勤勤恳恳地工作了{0}小时".Translate()), (stats[(gint)"stat_work_time"] / 60.0).ToString("f1"));
                        IsTrue = true;
                        break;
                    case 93:
                        Question = string.Format(ConvertQuestionText("{name}勤勤恳恳地工作了{0}小时".Translate()), ((stats[(gint)"stat_work_time"] / 60.0) * 10).ToString("f1"));
                        IsTrue = false;
                        break;
                    case 94:
                        Question = string.Format(ConvertQuestionText("{name}勤勤恳恳地工作了{0}小时".Translate()), ((stats[(gint)"stat_work_time"] / 60.0) / 10.0).ToString("f1"));
                        IsTrue = false;
                        break;
                }

                bool answerTrue = MessageBoxX.Show(Question, "生日蛋糕提问!".Translate(), MessageBoxButton.YesNo) == MessageBoxResult.Yes;
                if (answerTrue == IsTrue)
                {
                    var clone = obj.Clone();
                    clone.Name = "双份蛋糕".Translate();
                    TakeItem(clone);
                    Main.LabelDisplayShow("答对啦! 奖励双份蛋糕!".Translate(), 4000);
                }
                else
                {
                    Task.Run(() =>
                    {
                        Thread.Sleep(5000);
                        void DisplayWorkLikeAnimation(Work.WorkType workType)
                        {
                            var worklist = Core.Graph!.GraphConfig.Works.FindAll(x => x.Type == workType);
                            if (worklist.Count == 0)
                            {
                                Main.DisplayToNomal();
                                return;
                            }
                            Main.Display(worklist[Function.Rnd.Next(worklist.Count)].Graph, AnimatType.A_Start, (x) => Main.DisplayBLoopingToNomal(x, 20));
                        }

                        switch (Function.Rnd.Next(11))
                        {
                            case 0:
                                var moneyLoss = (int)Math.Min(GameSavesData.GameSave.Money / 100, Function.Rnd.Next(GameSavesData.GameSave.Level * 10));
                                GameSavesData.GameSave.Money -= moneyLoss;
                                GameSavesData.Data["loss"][(gint)"money"] += moneyLoss;
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}偷偷藏零花钱了".Translate(), Main), moneyLoss, 0, 4000);
                                break;
                            case 1:
                                var expLoss = (int)Math.Min(GameSavesData.GameSave.Exp / 100, Function.Rnd.Next(GameSavesData.GameSave.Level * 50));
                                GameSavesData.GameSave.Exp -= expLoss;
                                GameSavesData.Data["loss"][(gint)"exp"] += expLoss;
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}变聪明了".Translate(), Main), expLoss, 0, 4000);
                                break;
                            case 2:
                                var foodLoss = Math.Min(GameSavesData.GameSave!.StrengthFood, Math.Max(1, Math.Ceiling(GameSavesData.GameSave.StrengthMax * .2)));
                                GameSavesData.GameSave.StrengthChangeFood(-foodLoss);
                                GameSavesData.GameSave.Mode = GameSavesData.GameSave.CalMode();
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}想吃东西了".Translate(), Main), foodLoss, 0, 4000);
                                break;
                            case 3:
                                var drinkLoss = Math.Min(GameSavesData.GameSave!.StrengthDrink, Math.Max(1, Math.Ceiling(GameSavesData.GameSave.StrengthMax * .2)));
                                GameSavesData.GameSave.StrengthChangeDrink(-drinkLoss);
                                GameSavesData.GameSave.Mode = GameSavesData.GameSave.CalMode();
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}想喝东西了".Translate(), Main), drinkLoss, 0, 4000);
                                break;
                            case 4:
                                Main.Display(Function.Rnd.Next(2) == 0 ? GraphType.StartUP : GraphType.Shutdown, AnimatType.Single, Main.DisplayToNomal);
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}假装逃跑了".Translate(), Main), 0, 0, 4000);
                                break;
                            case 5:
                                List<Food> food = Foods.FindAll(x => x.Price >= 2 && x.Health >= -5 && x.Exp >= -10 && x.Likability >= 0 //桌宠不吃负面的食物
                                && !x.IsOverLoad() // 不吃超模食物
                                );
                                if (food.Count == 0)
                                    return;
                                var item = food[Function.Rnd.Next(food.Count)];
                                Main.Display(item.GetGraph(), item.ImageSource!, Main.DisplayToNomal);
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}假装偷吃了".Translate(), Main), 0, 0, 4000);
                                break;
                            case 6:
                                Main.DisplaySleep();
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}假装睡觉了".Translate(), Main), 0, 0, 4000);
                                break;
                            case 7:
                                DisplayWorkLikeAnimation(Work.WorkType.Work);
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}假装工作了".Translate(), Main), 0, 0, 4000);
                                break;
                            case 8:
                                DisplayWorkLikeAnimation(Work.WorkType.Study);
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}假装学习了".Translate(), Main), 0, 0, 4000);
                                break;
                            case 9:
                                DisplayWorkLikeAnimation(Work.WorkType.Play);
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}假装去玩了".Translate(), Main), 0, 0, 4000);
                                break;
                            default:
                                if (!Main.DisplayMove())
                                    Main.DisplayToNomal();
                                Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}跑掉了".Translate(), Main), 0, 0, 4000);
                                break;
                        }
                    });
                }

                break;
        }

        LastTakeItemTime = DateTime.Now;
    }

}
