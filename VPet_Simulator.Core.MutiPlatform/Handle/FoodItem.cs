using LinePutScript.Converter;
using static LinePutScript.Converter.LPSConvert;
using LinePutScript.Localization;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 食物
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Windows.Interface/Mod/Food.cs.
///
/// Windows 那边的 Food 继承自 Item, 带着背包、商店、收藏、超模检测那一整套东西,
/// 而它们都依赖 Panuon.WPF 的通知基类和 WPF 的 BitmapImage. 跨平台版没有商店也没有
/// 背包, 所以这里只留"喂给桌宠会发生什么"所需的字段, 做成一个平铺的类.
/// 字段名和 [Line] 标注与 Windows 版逐字一致, MOD 里的 food/*.lps 两边通用.
public partial class FoodItem : IFood
{
    /// <summary>
    /// 食物类型
    /// </summary>
    public enum FoodType
    {
        /// <summary>
        /// 食物 (默认)
        /// </summary>
        Food,
        /// <summary>
        /// 收藏 (自定义)
        /// </summary>
        Star,
        /// <summary>
        /// 正餐
        /// </summary>
        Meal,
        /// <summary>
        /// 零食
        /// </summary>
        Snack,
        /// <summary>
        /// 饮料
        /// </summary>
        Drink,
        /// <summary>
        /// 功能性
        /// </summary>
        Functional,
        /// <summary>
        /// 药品
        /// </summary>
        Drug,
        /// <summary>
        /// 礼品
        /// </summary>
        Gift,
    }

    [Line(ignoreCase: true)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 食物类型
    /// </summary>
    [Line(type: ConvertType.ToEnum, ignoreCase: true)]
    public FoodType Type { get; set; } = FoodType.Food;

    [Line(ignoreCase: true)]
    public int Exp { get; set; }
    [Line(ignoreCase: true)]
    public double Strength { get; set; }
    [Line(ignoreCase: true)]
    public double StrengthFood { get; set; }
    [Line(ignoreCase: true)]
    public double StrengthDrink { get; set; }
    [Line(ignoreCase: true)]
    public double Feeling { get; set; }
    [Line(ignoreCase: true)]
    public double Health { get; set; }
    [Line(ignoreCase: true)]
    public double Likability { get; set; }

    /// <summary>
    /// 价格
    /// </summary>
    [Line(ignoreCase: true)]
    public double Price { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [Line(ignoreCase: true)]
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 食用时显示的动画
    /// </summary>
    [Line(ignoreCase: true)]
    public string? Graph { get; set; } = null;

    /// <summary>
    /// 图片名, 留空时用食物名去找
    /// </summary>
    [Line(ignoreCase: true)]
    public string? Image { get; set; } = null;

    private string? nametrans;
    /// <summary>
    /// 食物名称 已翻译
    /// </summary>
    public string NameTrans => nametrans ??= Name.Translate();

    /// <summary>
    /// 图片文件的绝对路径, 找不到图时为 null
    /// </summary>
    /// 由 ModLoader 在扫描 image/food 时填上
    public string? ImagePath { get; set; }

    /// <summary>
    /// 获取食用时显示的动画
    /// </summary>
    public string GetGraph()
    {
        if (string.IsNullOrEmpty(Graph))
            switch (Type)
            {
                default:
                    return "eat";
                case FoodType.Drink:
                    return "drink";
                case FoodType.Gift:
                    return "gift";
            }
        else
            return Graph;
    }
}
