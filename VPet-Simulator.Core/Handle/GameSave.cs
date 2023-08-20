﻿using LinePutScript;
using LinePutScript.Converter;
using System;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 游戏存档
    /// </summary>
    public class GameSave
    {
        /// <summary>
        /// 宠物名字
        /// </summary>
        [Line(name: "name")]
        public virtual string Name { get; set; }

        /// <summary>
        /// 金钱
        /// </summary>
        [Line(Type = LPSConvert.ConvertType.ToFloat, Name = "money")]
        public virtual double Money { get; set; }
        /// <summary>
        /// 经验值
        /// </summary>
        [Line(type: LPSConvert.ConvertType.ToFloat, name: "exp")] public virtual double Exp { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        public virtual int Level => Exp < 0 ? 1 : (int)(Math.Sqrt(Exp) / 10) + 1;
        /// <summary>
        /// 升级所需经验值
        /// </summary>
        /// <returns></returns>
        public virtual int LevelUpNeed() => (int)(Math.Pow((Level) * 10, 2));
        /// <summary>
        /// 体力 0-100
        /// </summary>
        public virtual double Strength { get => strength; set => strength = Math.Min(100, Math.Max(0, value)); }
        [Line(Type = LPSConvert.ConvertType.ToFloat, IgnoreCase = true)]
        protected double strength { get; set; }
        /// <summary>
        /// 待补充的体力,随着时间缓慢加给桌宠
        /// </summary>//让游戏更有游戏性
        [Line(Type = LPSConvert.ConvertType.ToFloat, IgnoreCase = true)]
        public double StoreStrength { get; set; }
        /// <summary>
        /// 变化 体力
        /// </summary>
        public double ChangeStrength = 0;
        public virtual void StrengthChange(double value)
        {
            ChangeStrength += value;
            Strength += value;
        }
        /// <summary>
        /// 饱腹度
        /// </summary>
        public virtual double StrengthFood
        {
            get => strengthFood; set
            {
                value = Math.Min(100, value);
                if (value <= 0)
                {
                    Health += value;
                    strengthFood = 0;
                }
                else
                    strengthFood = value;
            }
        }
        [Line(Type = LPSConvert.ConvertType.ToFloat)]
        protected double strengthFood { get; set; }
        /// <summary>
        /// 待补充的饱腹度,随着时间缓慢加给桌宠
        /// </summary>//让游戏更有游戏性
        [Line(Type = LPSConvert.ConvertType.ToFloat)]
        public virtual double StoreStrengthFood { get; set; }
        public virtual void StrengthChangeFood(double value)
        {
            ChangeStrengthFood += value;
            StrengthFood += value;
        }
        /// <summary>
        /// 变化 食物
        /// </summary>
        public double ChangeStrengthFood = 0;
        /// <summary>
        /// 口渴度
        /// </summary>
        public virtual double StrengthDrink
        {
            get => strengthDrink; set
            {
                value = Math.Min(100, value);
                if (value <= 0)
                {
                    Health += value;
                    strengthDrink = 0;
                }
                else
                    strengthDrink = value;
            }
        }

        [Line(Type = LPSConvert.ConvertType.ToFloat)]
        protected double strengthDrink { get; set; }
        /// <summary>
        /// 待补充的口渴度,随着时间缓慢加给桌宠
        /// </summary>//让游戏更有游戏性
        [Line(Type = LPSConvert.ConvertType.ToFloat)]
        public virtual double StoreStrengthDrink { get; set; }
        /// <summary>
        /// 变化 口渴度
        /// </summary>
        public double ChangeStrengthDrink = 0;
        public void StrengthChangeDrink(double value)
        {
            ChangeStrengthDrink += value;
            StrengthDrink += value;
        }
        /// <summary>
        /// 心情
        /// </summary>
        public virtual double Feeling
        {
            get => feeling; set
            {

                value = Math.Min(100, value);
                if (value <= 0)
                {
                    Health += value / 2;
                    Likability += value / 2;
                    feeling = 0;
                }
                else
                    feeling = value;
            }
        }

        [Line(Type = LPSConvert.ConvertType.ToFloat)]
        protected double feeling { get; set; }
        /// <summary>
        /// 待补充的心情,随着时间缓慢加给桌宠
        /// </summary>//让游戏更有游戏性
        [Line(Type = LPSConvert.ConvertType.ToFloat)]
        public double StoreFeeling { get; set; }
        /// <summary>
        /// 变化 心情
        /// </summary>
        public double ChangeFeeling = 0;
        public virtual void FeelingChange(double value)
        {
            ChangeFeeling += value;
            Feeling += value;
        }
        /// <summary>
        /// 健康(生病)(隐藏)
        /// </summary>
        public double Health { get => health; set => health = Math.Min(100, Math.Max(0, value)); }

        [Line(Type = LPSConvert.ConvertType.ToFloat)]
        protected double health { get; set; }
        /// <summary>
        /// 好感度(隐藏)(累加值)
        /// </summary>
        public double Likability
        {
            get => likability; set
            {
                int max = 90 + Level * 10;
                value = Math.Max(0, value);
                if (value > max)
                {
                    likability = max;
                    Health += value - max;
                }
                else
                    likability = value;
            }
        }

        [Line(Type = LPSConvert.ConvertType.ToFloat)]
        protected double likability { get; set; }

        /// <summary>
        /// 清除变化
        /// </summary>
        public void CleanChange()
        {
            ChangeStrength /= 2;
            ChangeFeeling /= 2;
            ChangeStrengthDrink /= 2;
            ChangeStrengthFood /= 2;
        }
        /// <summary>
        /// 取回被储存的体力
        /// </summary>
        public void StoreTake()
        {
            const int t = 10;
            var s = StoreFeeling / t;
            StoreFeeling -= s;
            if (Math.Abs(StoreFeeling) < 1)
                StoreFeeling = 0;
            else
                FeelingChange(s);

            s = StoreStrength / t;
            StoreStrength -= s;
            if (Math.Abs(StoreStrength) < 1)
                StoreStrength = 0;
            else
                StrengthChange(s);

            s = StoreStrengthDrink / t;
            StoreStrengthDrink -= s;
            if (Math.Abs(StoreStrengthDrink) < 1)
                StoreStrengthDrink = 0;
            else
                StrengthChangeDrink(s);

            s = StoreStrengthFood / t;
            StoreStrengthFood -= s;
            if (Math.Abs(StoreStrengthFood) < 1)
                StoreStrengthFood = 0;
            else
                StrengthChangeFood(s);
        }
        /// <summary>
        /// 吃食物
        /// </summary>
        /// <param name="food">食物类</param>
        public void EatFood(IFood food)
        {
            Exp += food.Exp;
            var tmp = food.Strength / 2;
            StrengthChange(tmp);
            StoreStrength += tmp;
            tmp = food.StrengthFood / 2;
            StrengthChangeFood(tmp);
            StoreStrengthFood += tmp;
            tmp = food.StrengthDrink / 2;
            StrengthChangeDrink(tmp);
            StoreStrengthDrink += tmp;
            tmp = food.Feeling / 2;
            FeelingChange(tmp);
            StoreFeeling += tmp;
            Health += food.Health;
            Likability += food.Likability;
        }
        /// <summary>
        /// 宠物状态模式
        /// </summary>
        public enum ModeType
        {
            /// <summary>
            /// 高兴
            /// </summary>
            Happy,
            /// <summary>
            /// 普通
            /// </summary>
            Nomal,
            /// <summary>
            /// 状态不佳
            /// </summary>
            PoorCondition,
            /// <summary>
            /// 生病(躺床)
            /// </summary>
            Ill
        }
        [Line(name: "mode")]
        public ModeType Mode { get; set; } = ModeType.Nomal;
        /// <summary>
        /// 计算宠物当前状态
        /// </summary>
        public ModeType CalMode()
        {
            var healthFactor = GetHealthFactor();
            if (Health <= healthFactor)
            {
                return Health <= GetHalfFactor(healthFactor) ? ModeType.Ill : ModeType.PoorCondition;
            }

            var feelingFactor = GetFeelingFactor();
            if (Feeling < feelingFactor)
            {
                return Feeling <= GetHalfFactor(feelingFactor) ? ModeType.PoorCondition : ModeType.Nomal;
            }
            else
            {
                return ModeType.Happy;
            }

            int GetHalfFactor(int factor)
            {
                return (int)factor / 2;
            }
        }

        private int GetHealthFactor()
        {
            return 60 - (IsHighFeeling ? 12 : 0) - (IsHighestLikability ? 12 : IsNormalLikability ? 6 : 0);
        }

        private int GetFeelingFactor()
        {
            return 90 - (IsHighestLikability ? 20 : (IsNormalLikability ? 10 : 0));
        }

        private bool IsNormalLikability => Likability is >= 40 and < 80;

        private bool IsHighestLikability => Likability >= 80;

        private bool IsHighFeeling => Feeling >= 80;

        /// <summary>
        /// 新游戏
        /// </summary>
        public GameSave(string name)
        {
            Name = name;
            Money = 100;
            Exp = 0;
            Strength = 100;
            StrengthFood = 100;
            StrengthDrink = 100;
            Feeling = 60;
            Health = 100;
            Likability = 0;
            Mode = CalMode();
        }
        /// <summary>
        /// 读档
        /// </summary>
        public GameSave()
        {
            //Money = line.GetFloat("money");
            //Name = line.Info;
            //Exp = line.GetInt("exp");
            //Strength = line.GetFloat("strength");
            //StrengthDrink = line.GetFloat("strengthdrink");
            //StrengthFood = line.GetFloat("strengthfood");
            //Feeling = line.GetFloat("feeling");
            //Health = line.GetFloat("health");
            //Likability = line.GetFloat("likability");
            //Mode = CalMode();
        }
        /// <summary>
        /// 读档
        /// </summary>
        public static GameSave Load(ILine data) => LPSConvert.DeserializeObject<GameSave>(data);
        /// <summary>
        /// 存档
        /// </summary>
        /// <returns>存档行</returns>
        public Line ToLine()
        {
            //Line save = new Line("vpet", Name);
            //save.SetFloat("money", Money);
            //save.SetInt("exp", Exp);
            //save.SetFloat("strength", Strength);
            //save.SetFloat("strengthdrink", StrengthDrink);
            //save.SetFloat("strengthfood", StrengthFood);
            //save.SetFloat("feeling", Feeling);
            //save.SetFloat("health", Health);
            //save.SetFloat("Likability", Likability);
            return LPSConvert.SerializeObject(this, "vpet");
        }
    }
}
