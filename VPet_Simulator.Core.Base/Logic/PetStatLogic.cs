using System;

namespace VPet_Simulator.Core;

internal static class PetStatLogic
{
    public static void FunctionSpend(IPetStatHost host, double TimePass)
        {
            var attributes = host.AttributeModifier ?? new AttributeModifiers();
            var Save = host.Save;
            var NowWork = host.CurrentWork;
            int addhealth;
            Save.CleanChange();
            Save.StoreTake();
            double freedrop = (DateTime.Now - host.LastInteraction).TotalMinutes;
            if (freedrop < 1)
                freedrop = 0;
            else
                freedrop = Math.Min(Math.Sqrt(freedrop) * TimePass / 4, Save.FeelingMax / 800);
            double strengthMax = Save.StrengthMax;
            double sm25 = strengthMax * 0.25;
            double sm50 = strengthMax * 0.5;
            double sm60 = strengthMax * 0.6;
            double sm75 = strengthMax * 0.75;

            switch (host.WorkingState)
            {
                case PetWorkingState.Empty:
                    break;
                case PetWorkingState.Sleep:
                    //睡觉 缓慢恢复所有(除了心情,但是心情不会下降)
                    Save.StrengthChange(TimePass * 2 * attributes.Strength);
                    Save.StrengthChangeFood(TimePass * attributes.Food);
                    if (Save.StrengthFood <= sm25)
                    {//低状态2倍恢复速度
                        Save.StrengthChangeFood(TimePass * attributes.Food);
                    }
                    else if (Save.StrengthFood >= sm75)
                        Save.Health += TimePass * 2 * attributes.Health;
                    Save.StrengthChangeDrink(TimePass * attributes.Drink);
                    if (Save.StrengthDrink >= sm25)
                    {
                        Save.StrengthChangeDrink(TimePass * attributes.Drink);
                    }
                    else if (Save.StrengthDrink >= sm75)
                        Save.Health += TimePass * 2 * attributes.Health;
                    host.LastInteraction = DateTime.Now;
                    break;
                case PetWorkingState.Work:
                    if (NowWork == null)
                        break;
                    var needfood = TimePass * NowWork.StrengthFood;
                    var needdrink = TimePass * NowWork.StrengthDrink;

                    double efficiency = 0;
                    addhealth = -2;


                    var nsfood = needfood * .3;
                    var nsdrink = needdrink * .3;
                    if (Save.Strength > sm25 + nsfood + nsdrink)
                    {//可以用体力减少一些消耗,并且增加效率
                        Save.StrengthChange((-nsfood - nsdrink) * attributes.StrengthCost);
                        efficiency += 0.1;
                        needfood -= nsfood;
                        needdrink -= nsdrink;
                    }

                    if (Save.StrengthFood <= sm25)
                    {//低状态低效率
                        Save.StrengthChangeFood(-needfood / 2 * attributes.FoodCost);
                        efficiency += 0.2;
                        if (Save.Strength >= needfood)
                        {
                            Save.StrengthChange(-needfood * attributes.StrengthCost);
                            efficiency += 0.1;
                        }
                        addhealth -= 2;
                    }
                    else
                    {
                        Save.StrengthChangeFood(-needfood * attributes.FoodCost);
                        efficiency += 0.4;
                        if (Save.StrengthFood >= sm60)
                        {
                            addhealth += host.Rnd.Next(1, 3);
                            efficiency += 0.1;
                        }
                    }
                    if (Save.StrengthDrink <= sm25)
                    {//低状态低效率
                        Save.StrengthChangeDrink(-needdrink / 2 * attributes.DrinkCost);
                        efficiency += 0.2;
                        if (Save.Strength >= needdrink)
                        {
                            Save.StrengthChange(-needdrink * attributes.StrengthCost);
                            efficiency += 0.1;
                        }
                        addhealth -= 2;
                    }
                    else
                    {
                        Save.StrengthChangeDrink(-needdrink * attributes.DrinkCost);
                        efficiency += 0.4;
                        if (Save.StrengthDrink >= sm60)
                        {
                            addhealth += host.Rnd.Next(1, 3);
                            efficiency += 0.1;
                        }
                    }
                    if (addhealth > 0)
                        Save.Health += addhealth * TimePass * attributes.Health;
                    var addmoney = Math.Max(0, TimePass * NowWork.MoneyBase * (2 * efficiency - 0.5))
                        * (NowWork.Kind == PetWorkKind.Work ? attributes.Money : attributes.Experience);
                    if (NowWork.Kind == PetWorkKind.Work)
                        Save.Money += addmoney;
                    else
                        Save.Exp += addmoney;
                    host.AddWorkCount(addmoney);
                    if (NowWork.Kind == PetWorkKind.Play)
                    {
                        host.LastInteraction = DateTime.Now;
                        Save.FeelingChange(-NowWork.Feeling * TimePass
                            * (NowWork.Feeling > 0 ? attributes.FeelingCost : attributes.Feeling));
                    }
                    else
                        Save.FeelingChange(-freedrop * (0.5 + NowWork.Feeling / 2) * attributes.FeelingCost);
                    break;
                default://默认
                    //饮食等乱七八糟的消耗
                    addhealth = -2;
                    if (Save.StrengthFood >= sm50)
                    {
                        Save.StrengthChangeFood(-TimePass * attributes.FoodCost);
                        Save.StrengthChange(TimePass * attributes.Strength);
                        if (Save.StrengthFood >= sm75)
                            addhealth += host.Rnd.Next(1, 3);
                    }
                    else if (Save.StrengthFood <= sm25)
                    {
                        Save.Health -= host.Rnd.NextDouble() * TimePass * attributes.HealthCost;
                        addhealth -= 2;
                    }
                    if (Save.StrengthDrink >= sm50)
                    {
                        Save.StrengthChangeDrink(-TimePass * attributes.DrinkCost);
                        Save.StrengthChange(TimePass * attributes.Strength);
                        if (Save.StrengthDrink >= sm75)
                            addhealth += host.Rnd.Next(1, 3);
                    }
                    else if (Save.StrengthDrink <= sm25)
                    {
                        Save.Health -= host.Rnd.NextDouble() * TimePass * attributes.HealthCost;
                        addhealth -= 2;
                    }
                    if (addhealth > 0)
                        Save.Health += addhealth * TimePass * attributes.Health;
                    Save.StrengthChangeFood(-TimePass * attributes.FoodCost);
                    Save.StrengthChangeDrink(-TimePass * attributes.DrinkCost);
                    Save.FeelingChange(-freedrop * attributes.FeelingCost);
                    break;
            }

            //if (Core.GameSave.Strength <= 40)
            //{
            //    Core.GameSave.Health -= host.Rnd.Next(0, 1);
            //}
            Save.Exp += TimePass * attributes.Experience;
            //感受提升好感度
            if (Save.Feeling >= Save.FeelingMax * 0.75)
            {
                if (Save.Feeling >= Save.FeelingMax * 0.90)
                {
                    Save.Likability += TimePass * attributes.Likability;
                }
                Save.Exp += TimePass * 2 * attributes.Experience;
                Save.Health += TimePass * attributes.Health;
            }
            else if (Save.Feeling <= 25) //这个就不乘倍率了, 给上限高一些好处
            {
                Save.Likability -= TimePass;
                Save.Exp -= TimePass;
            }
            if (Save.StrengthDrink <= sm25)
            {
                Save.Health -= host.Rnd.Next(0, 1) * TimePass * attributes.HealthCost;
                Save.Exp -= TimePass;
            }
            else if (Save.StrengthDrink >= sm75)
                Save.Health += host.Rnd.Next(0, 1) * TimePass * attributes.Health;

            host.RaiseFunctionSpend();
            var newmod = Save.CalMode();
            if (Save.Mode != newmod)
            {
                //切换显示动画
                host.PlaySwitchAnimat(Save.Mode, newmod);

                Save.Mode = newmod;
            }
            //看情况播放停止工作动画
            if (Save.Mode == IGameSave.ModeType.Ill && host.WorkingState == PetWorkingState.Work)
            {
                host.StopWorkByStateFail();
            }
        }
}
