using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                obj.Exp = Core.Save!.Level;
                obj.Likability = Core.Save!.LikabilityMax / 20;
                obj.StrengthDrink = Core.Save!.StrengthMax / 20;
                obj.StrengthFood = Core.Save!.StrengthMax / 20;
                obj.isoverload = false;
                obj.Price = (int)Math.Max(0, obj.RealPrice * .5);
                switch (Function.Rnd.Next(15))
                {
                    case 1:
                    case 2:
                    case 3:
                        Core.Save!.Strength = Core.Save!.StrengthMax;
                        Main.LabelDisplayShow("{0}充满抛瓦!".Translate(Core.Save!.Name), 3000);
                        break;
                    case 4:
                    case 5:
                        Core.Save!.Feeling = Core.Save!.FeelingMax;
                        Main.LabelDisplayShow("{0}今天也是好心情!".Translate(Core.Save!.Name), 3000);
                        break;
                    case 6:
                    case 7:
                        Core.Save!.StrengthFood = Core.Save!.StrengthMax;
                        Main.LabelDisplayShow("{0}吃饱了!".Translate(Core.Save!.Name), 3000);
                        break;
                    case 8:
                    case 9:
                        Core.Save!.StrengthDrink = Core.Save!.StrengthMax;
                        Main.LabelDisplayShow("{0}加满水了!".Translate(Core.Save!.Name), 3000);
                        break;
                    case 10:
                        int get = (Function.Rnd.Next(Core.Save!.LevelUpNeed() * (GameSavesData.GameSave.LevelMax + 1)) / 200 + 1) * 100;
                        Core.Save!.Exp += get;
                        Main.LabelDisplayShow("{0}经验 +{1} 告辞".Translate(Core.Save!.Name, get.ToString("N0")), 4000);
                        break;
                    case 11:
                        get = (Function.Rnd.Next(Core.Save!.LevelUpNeed() * (GameSavesData.GameSave.LevelMax + 1)) / 500 + 1) * 10;
                        Core.Save!.Exp += get;
                        Main.LabelDisplayShow("{0}在马路边捡到{1}金钱".Translate(Core.Save!.Name, get.ToString("N0")), 4000);
                        break;
                    case 12:
                        if (Function.Rnd.Next(3) != 0)
                        {//再随一次, 给好感度
                            get = Function.Rnd.Next((int)Core.Save!.LikabilityMax / 25) + 1;
                            Core.Save!.Likability += get;
                            Main.LabelDisplayShow("{0}更喜欢{1}了".Translate(Core.Save!.Name, Core.Save!.HostName), 4000);
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
                            Main.LabelDisplayShow("{0}收到了新照片".Translate(Core.Save!.Name) + '\n' + photo.Name, 4000);
                        }
                        else
                            goto case 11;
                        break;
                    default:
                        Main.LabelDisplayShow("{0}获得了谢谢惠顾".Translate(Core.Save!.Name), 4000);
                        break;
                }
                break;

            case "生日蛋糕3":
                if (LastTakeItemTime.AddSeconds(5) > DateTime.Now)
                    break;//避免频繁触发

                var birthdayQuiz = new (string Question, bool IsTrue)[]
                {
                        ("{0}今年一岁啦", false),
                        ("{0}今年两岁啦", false),
                        ("{0}今年三岁啦", true),
                        ("{0}今年两岁啦", false),
                        ("{0}的生日是8月13日", false),
                        ("{0}的生日是8月14日", true),
                        ("{0}的生日是7月14日", false),
                        ("{0}的生日是3月25日", false),
                        ("{0}没开过生日会", false),
                        ("{0}生日会有很多二创作品", true),
                        ("{0}生日会在b站直播", true),
                        ("更好买有1种生日蛋糕", false),
                        ("更好买有2种生日蛋糕", false),
                        ("更好买有3种生日蛋糕", true),
                        ("生日蛋糕在更好买收藏里", true),
                        ("生日蛋糕在更好买礼品里", false),
                        ("一周年的生日蛋糕叫“全回复生日蛋糕”", true),
                        ("二周年的生日蛋糕叫“惊喜生日蛋糕”", true),
                        ("LBGame制作组（虚拟桌宠模拟器的制作组）是一个效率高，产能大，更新快，不画饼，永不跳票的好制作组。", false),
                };

                var quiz = birthdayQuiz[Function.Rnd.Next(birthdayQuiz.Length)];
                bool answerTrue = MessageBoxX.Show(quiz.Question.Translate(Core.Save!.Name), "生日蛋糕问答".Translate(), MessageBoxButton.YesNo) == MessageBoxResult.Yes;
                if (answerTrue == quiz.IsTrue)
                {
                    var clone = obj.Clone();
                    clone.Name = "答对啦! 奖励双份蛋糕!".Translate();
                    TakeItem(clone);
                    Main.LabelDisplayShow("答对啦! 奖励双份蛋糕!".Translate(), 4000);
                }
                else
                {
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
                            var moneyLoss = (int)Math.Min(Core.Save.Money / 100, Function.Rnd.Next(Core.Save.Level * 10));
                            Core.Save.Money -= moneyLoss;
                            GameSavesData.Data["loss"][(gint)"money"] += moneyLoss;
                            Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}偷偷藏零花钱了".Translate(), Main), moneyLoss, 0, 4000);
                            break;
                        case 1:
                            var expLoss = (int)Math.Min(Core.Save.Exp / 100, Function.Rnd.Next(Core.Save.Level * 50));
                            Core.Save.Exp -= expLoss;
                            GameSavesData.Data["loss"][(gint)"exp"] += expLoss;
                            Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}变聪明了".Translate(), Main), expLoss, 0, 4000);
                            break;
                        case 2:
                            var foodLoss = Math.Min(Core.Save!.StrengthFood, Math.Max(1, Math.Ceiling(Core.Save.StrengthMax * .2)));
                            Core.Save.StrengthChangeFood(-foodLoss);
                            Core.Save.Mode = Core.Save.CalMode();
                            Main.LabelDisplayShowChangeNumber(IText.ConverText("{name}想吃东西了".Translate(), Main), foodLoss, 0, 4000);
                            break;
                        case 3:
                            var drinkLoss = Math.Min(Core.Save!.StrengthDrink, Math.Max(1, Math.Ceiling(Core.Save.StrengthMax * .2)));
                            Core.Save.StrengthChangeDrink(-drinkLoss);
                            Core.Save.Mode = Core.Save.CalMode();
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
                }

                break;
        }

        LastTakeItemTime = DateTime.Now;
    }

}
