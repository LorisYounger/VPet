using LinePutScript;
using LinePutScript.Converter;
using System;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 游戏存档
    /// </summary>
    public interface IGameSave
    {
        /// <summary>
        /// 宠物名字
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 金钱
        /// </summary>
        double Money { get; set; }
        /// <summary>
        /// 经验值
        /// </summary>
        double Exp { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        int Level { get; }
        /// <summary>
        /// 升级所需经验值
        /// </summary>
        /// <returns></returns>
        int LevelUpNeed();
        /// <summary>
        /// 体力 0-100
        /// </summary>
        double Strength { get; set; }
        /// <summary>
        /// 最大体力值
        /// </summary>
        double StrengthMax { get;}
        /// <summary>
        /// 待补充的体力,随着时间缓慢加给桌宠
        /// </summary>//让游戏更有游戏性
        double StoreStrength { get; set; }
        /// <summary>
        /// 变化 体力
        /// </summary>
        double ChangeStrength { get; set; }
        /// <summary>
        /// 修改体力
        /// </summary>
        /// <param name="value"></param>
        void StrengthChange(double value);
        /// <summary>
        /// 饱腹度
        /// </summary>
        double StrengthFood { get; set; }
        /// <summary>
        /// 待补充的饱腹度,随着时间缓慢加给桌宠
        /// </summary>//让游戏更有游戏性
        double StoreStrengthFood { get; set; }
        void StrengthChangeFood(double value);
        /// <summary>
        /// 变化 食物
        /// </summary>
        double ChangeStrengthFood { get; set; }
        /// <summary>
        /// 口渴度
        /// </summary>
        double StrengthDrink { get; set; }

        /// <summary>
        /// 待补充的口渴度,随着时间缓慢加给桌宠
        /// </summary>//让游戏更有游戏性
        double StoreStrengthDrink { get; set; }
        /// <summary>
        /// 变化 口渴度
        /// </summary>
        double ChangeStrengthDrink { get; set; }
        /// <summary>
        /// 修改口渴度
        /// </summary>
        void StrengthChangeDrink(double value);
        /// <summary>
        /// 修改心情
        /// </summary>
        void FeelingChange(double value);

        /// <summary>
        /// 心情
        /// </summary>
        double Feeling { get; set; }

        /// <summary>
        /// 待补充的心情,随着时间缓慢加给桌宠
        /// </summary>//让游戏更有游戏性
        double StoreFeeling { get; set; }

        /// <summary>
        /// 健康(生病)(隐藏)
        /// </summary>
        double Health { get; set; }

        /// <summary>
        /// 好感度(隐藏)(累加值)
        /// </summary>
        double Likability { get; set; }
        /// <summary>
        /// 好感度(隐藏)(最大值)
        /// </summary>
        double LikabilityMax { get;}

        /// <summary>
        /// 清除变化
        /// </summary>
        void CleanChange();
        /// <summary>
        /// 取回被储存的体力
        /// </summary>
        void StoreTake();
        /// <summary>
        /// 吃食物
        /// </summary>
        /// <param name="food">食物类</param>
        void EatFood(IFood food);
        /// <summary>
        /// 宠物当前状态
        /// </summary>
        ModeType Mode { get; set; }
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

        /// <summary>
        /// 计算宠物当前状态
        /// </summary>
        ModeType CalMode();

        /// <summary>
        /// 存档
        /// </summary>
        /// <returns>存档行</returns>
        Line ToLine();
    }
}
