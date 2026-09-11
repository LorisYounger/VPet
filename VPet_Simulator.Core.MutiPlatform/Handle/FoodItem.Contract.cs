using LinePutScript;
using LinePutScript.Converter;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 食物在统一 MOD 契约里的那一面
/// </summary>
/// 跨平台侧刻意不做 Windows 那种"真对象 + 包装器"两层: 契约要的东西 FoodItem
/// 本来就有, 直接实现出来, 少一层就少一处会走样的地方. MOD 拿到的就是宿主手里
/// 那个食物本身, 改了立刻生效。
public partial class FoodItem : IFoodInfo
{
    /// <summary>
    /// 契约里的类型, 与 FoodType 数值一一对应
    /// </summary>
    PetFoodType IFoodInfo.Type
    {
        get => (PetFoodType)(int)Type;
        set => Type = (FoodType)(int)value;
    }

    string IFoodInfo.TranslateName => NameTrans;

    /// <summary>
    /// 是否已收藏
    /// </summary>
    /// 与 Windows 版一样存在设置里的 betterbuy:star, 不随存档走
    public bool Star { get; set; }

    /// <summary>
    /// 按当前数值算出的推荐价格
    /// </summary>
    /// 与 Windows 版 Food.RealPrice 同一个式子
    public double RealPrice => FoodPricing.RealPrice(
        Exp, Strength, StrengthFood, StrengthDrink, Feeling, Health, Likability);

    /// <summary>
    /// 是不是超模(定价远低于推荐价)
    /// </summary>
    public bool IsOverLoad() => FoodPricing.IsOverLoad(Price, RealPrice);

    /// <summary>
    /// 序列化成一行
    /// </summary>
    public ILine ToLine() => LPSConvert.SerializeObjectToLine<Line>(this, "food");
}
