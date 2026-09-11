using System;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 桌宠数值模拟 (平台无关)
    /// </summary>
    /// 这段逻辑决定了体力/饱腹/口渴/心情/健康/金钱/经验怎么随时间演化, 也就是整个
    /// 游戏的平衡性. 它必须在 Windows 版和跨平台版之间保持完全一致 —— 一旦分叉,
    /// 表现是"两个平台玩起来手感不一样", 没有任何报错, 极难发现.
    ///
    /// 因此这份源码同时被 VPet-Simulator.Core 和 VPet_Simulator.Core.MutiPlatform
    /// 直接编译(见两边 csproj 里的 Compile Include), 而不是各写一份.
    /// 类型都是 internal, 所以不会给 Core 的公开 API 增加任何东西.
    internal static class PetStatLogic
    {
        /// <summary>
        /// 根据消耗计算相关数据
        /// </summary>
        /// <param name="host">桌宠主体</param>
        /// <param name="TimePass">过去时间倍率</param>
        public static void FunctionSpend(IPetStatHost host, double TimePass)
        {
            var Save = host.Save;
            var Rnd = host.Rnd;

            Save.CleanChange();
            Save.StoreTake();
            double freedrop = (DateTime.Now - host.LastInteraction).TotalMinutes;
            if (freedrop < 1)
                freedrop = 0;
            else
                freedrop = Math.Min(Math.Sqrt(freedrop) * TimePass / 4, Save.FeelingMax / 800);
            double sm25 = Save.StrengthMax * 0.25;
            double sm50 = Save.StrengthMax * 0.5;
            double sm60 = Save.StrengthMax * 0.6;
            double sm75 = Save.StrengthMax * 0.75;

            int addhealth;
            switch (host.WorkingState)
            {
                case PetWorkingState.Empty:
                    break;
                case PetWorkingState.Sleep:
                    //睡觉 缓慢恢复所有(除了心情,但是心情不会下降)
                    Save.StrengthChange(TimePass * 2);
                    Save.StrengthChangeFood(TimePass);
                    if (Save.StrengthFood <= sm25)
                    {//低状态2倍恢复速度
                        Save.StrengthChangeFood(TimePass);
                    }
                    else if (Save.StrengthFood >= sm75)
                        Save.Health += TimePass * 2;
                    Save.StrengthChangeDrink(TimePass);
                    if (Save.StrengthDrink >= sm25)
                    {
                        Save.StrengthChangeDrink(TimePass);
                    }
                    else if (Save.StrengthDrink >= sm75)
                        Save.Health += TimePass * 2;
                    host.LastInteraction = DateTime.Now;
                    break;
                case PetWorkingState.Work:
                    var NowWork = host.CurrentWork;
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
                        Save.StrengthChange(-nsfood - nsdrink);
                        efficiency += 0.1;
                        needfood -= nsfood;
                        needdrink -= nsdrink;
                    }

                    if (Save.StrengthFood <= sm25)
                    {//低状态低效率
                        Save.StrengthChangeFood(-needfood / 2);
                        efficiency += 0.2;
                        if (Save.Strength >= needfood)
                        {
                            Save.StrengthChange(-needfood);
                            efficiency += 0.1;
                        }
                        addhealth -= 2;
                    }
                    else
                    {
                        Save.StrengthChangeFood(-needfood);
                        efficiency += 0.4;
                        if (Save.StrengthFood >= sm60)
                        {
                            addhealth += Rnd.Next(1, 3);
                            efficiency += 0.1;
                        }
                    }
                    if (Save.StrengthDrink <= sm25)
                    {//低状态低效率
                        Save.StrengthChangeDrink(-needdrink / 2);
                        efficiency += 0.2;
                        if (Save.Strength >= needdrink)
                        {
                            Save.StrengthChange(-needdrink);
                            efficiency += 0.1;
                        }
                        addhealth -= 2;
                    }
                    else
                    {
                        Save.StrengthChangeDrink(-needdrink);
                        efficiency += 0.4;
                        if (Save.StrengthDrink >= sm60)
                        {
                            addhealth += Rnd.Next(1, 3);
                            efficiency += 0.1;
                        }
                    }
                    if (addhealth > 0)
                        Save.Health += addhealth * TimePass;
                    var addmoney = Math.Max(0, TimePass * NowWork.MoneyBase * (2 * efficiency - 0.5));
                    if (NowWork.Kind == PetWorkKind.Work)
                        Save.Money += addmoney;
                    else
                        Save.Exp += addmoney;
                    host.AddWorkCount(addmoney);
                    if (NowWork.Kind == PetWorkKind.Play)
                    {
                        host.LastInteraction = DateTime.Now;
                        Save.FeelingChange(-NowWork.Feeling * TimePass);
                    }
                    else
                        Save.FeelingChange(-freedrop * (0.5 + NowWork.Feeling / 2));
                    break;
                default://默认
                    //饮食等乱七八糟的消耗
                    addhealth = -2;
                    if (Save.StrengthFood >= sm50)
                    {
                        Save.StrengthChangeFood(-TimePass);
                        Save.StrengthChange(TimePass);
                        if (Save.StrengthFood >= sm75)
                            addhealth += Rnd.Next(1, 3);
                    }
                    else if (Save.StrengthFood <= sm25)
                    {
                        Save.Health -= Rnd.NextDouble() * TimePass;
                        addhealth -= 2;
                    }
                    if (Save.StrengthDrink >= sm50)
                    {
                        Save.StrengthChangeDrink(-TimePass);
                        Save.StrengthChange(TimePass);
                        if (Save.StrengthDrink >= sm75)
                            addhealth += Rnd.Next(1, 3);
                    }
                    else if (Save.StrengthDrink <= sm25)
                    {
                        Save.Health -= Rnd.NextDouble() * TimePass;
                        addhealth -= 2;
                    }
                    if (addhealth > 0)
                        Save.Health += addhealth * TimePass;
                    Save.StrengthChangeFood(-TimePass);
                    Save.StrengthChangeDrink(-TimePass);
                    Save.FeelingChange(-freedrop);
                    break;
            }

            Save.Exp += TimePass;
            //感受提升好感度
            if (Save.Feeling >= Save.FeelingMax * 0.75)
            {
                if (Save.Feeling >= Save.FeelingMax * 0.90)
                {
                    Save.Likability += TimePass;
                }
                Save.Exp += TimePass * 2;
                Save.Health += TimePass;
            }
            else if (Save.Feeling <= 25) //这个就不乘倍率了, 给上限高一些好处
            {
                Save.Likability -= TimePass;
                Save.Exp -= TimePass;
            }
            if (Save.StrengthDrink <= sm25)
            {
                Save.Health -= Rnd.Next(0, 1) * TimePass;
                Save.Exp -= TimePass;
            }
            else if (Save.StrengthDrink >= sm75)
                Save.Health += Rnd.Next(0, 1) * TimePass;

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
}
