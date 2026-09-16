using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 扩展方法
    /// </summary>
    /// 这一半是共享源码: 带倍率的进食结算两个平台都要用(吃腻度就是靠它落地的),
    /// 而它只依赖 IGameSave 和 IFood, 与界面无关.
    public static partial class ExtensionFunction
    {
        /// <summary>
        /// 吃食物 附带倍率
        /// </summary>
        /// <param name="save">存档</param>
        /// <param name="food">食物</param>
        /// <param name="buff">默认1倍</param>
        public static void EatFood(this IGameSave save, IFood food, double buff)
        {
            save.Exp += food.Exp * buff;
            var tmp = food.Strength / 2 * buff;
            save.StrengthChange(tmp);
            save.StoreStrength += tmp;
            tmp = food.StrengthFood / 2 * buff;
            save.StrengthChangeFood(tmp);
            save.StoreStrengthFood += tmp;
            tmp = food.StrengthDrink / 2 * buff;
            save.StrengthChangeDrink(tmp);
            save.StoreStrengthDrink += tmp;
            save.FeelingChange(food.Feeling * buff);
            save.Health += food.Health * buff;
            save.Likability += food.Likability * buff;
        }
    }
}
