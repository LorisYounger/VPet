using LinePutScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 游戏存档
    /// </summary>
    public class Save
    {
        /// <summary>
        /// 金钱
        /// </summary>
        public double Money;
        /// <summary>
        /// 经验值
        /// </summary>
        public int Exp;
        /// <summary>
        /// 等级
        /// </summary>
        public int Level => (int)(Math.Sqrt(Exp) / 5) + 1;


        /// <summary>
        /// 升级所需经验值
        /// </summary>
        /// <returns></returns>
        public int LevelUpNeed() => (int)(Math.Pow((Level) * 5, 2));
        /// <summary>
        /// 体力 0-100
        /// </summary>
        public double Strength { get => strength; set => strength = Math.Min(100, Math.Max(0, value)); }

        private double strength;
        /// <summary>
        /// 饱腹度
        /// </summary>
        public double StrengthFood { get => strengthFood; set => strengthFood = Math.Min(100, Math.Max(0, value)); }

        private double strengthFood;
        /// <summary>
        /// 口渴度
        /// </summary>
        public double StrengthDrink { get => strengthDrink; set => strengthDrink = Math.Min(100, Math.Max(0, value)); }

        private double strengthDrink;
        /// <summary>
        /// 心情
        /// </summary>
        public double Feeling { get => feeling; set => feeling = Math.Min(100, Math.Max(0, value)); }

        private double feeling;
        /// <summary>
        /// 健康(生病)(隐藏)
        /// </summary>
        public double Health { get => health; set => health = Math.Min(100, Math.Max(0, value)); }

        private double health;
        /// <summary>
        /// 好感度(隐藏)(累加值)
        /// </summary>
        public double Likability { get => likability; set => likability = Math.Min(90 + Level * 10, Math.Max(0, value)); }

        private double likability;
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
        public ModeType Mode = ModeType.Nomal;
        /// <summary>
        /// 计算宠物当前状态
        /// </summary>
        public ModeType CalMode()
        {
            int realhel = 60 - (Feeling >= 80 ? 20 : 0) - (Likability >= 80 ? 20 : (Likability >= 40 ? 10 : 0));
            //先从最次的开始
            if (Health <= realhel)
            {
                //可以确认从状态不佳和生病二选一
                if (Health <= realhel / 2)
                {//生病
                    return ModeType.Ill;
                }
                else
                {
                    return ModeType.PoorCondition;
                }
            }
            //然后判断是高兴还是普通
            else if (Feeling >= 80 - (Likability >= 80 ? 20 : (Likability >= 40 ? 10 : 0)))
            {
                return ModeType.Happy;
            }
            else
            {
                return ModeType.Nomal;
            }
        }
        /// <summary>
        /// 新游戏
        /// </summary>
        public Save()
        {
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
        public Save(Line line)
        {
            Money = line.GetFloat("money");
            Exp = line.GetInt("exp");
            Strength = line.GetFloat("strength");
            StrengthDrink = line.GetFloat("strengthdrink");
            StrengthFood = line.GetFloat("strengthfood");
            Feeling = line.GetFloat("feeling");
            Health = line.GetFloat("health");
            Likability = line.GetFloat("likability");
            Mode = CalMode();
        }
        /// <summary>
        /// 存档
        /// </summary>
        /// <returns>存档行</returns>
        public Line ToLine()
        {
            Line save = new Line("vpet", "");
            save.SetFloat("money", Money);
            save.SetInt("exp", Exp);
            save.SetFloat("strength", Strength);
            save.SetFloat("strengthdrink", StrengthDrink);
            save.SetFloat("strengthfood", StrengthFood);
            save.SetFloat("feeling", Feeling);
            save.SetFloat("health", Health);
            save.SetFloat("Likability", Likability);
            return save;
        }
    }
}
