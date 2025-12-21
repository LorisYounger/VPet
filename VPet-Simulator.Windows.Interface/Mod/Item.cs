using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 物品 (这里的物品指的是能够在背包中查看和使用的物品)
/// </summary>
/// 注: 物品的使用方法均需要代码插件手写, 在 imw.TakeItem 中实现
/// 虽然说 Item 类 给了很多
public class Item : NotifyPropertyChangedBase
{

    /// <summary>
    /// 物品名字 (ID)
    /// </summary>
    [Line(name: "name")]
    public string Name { get; set; }
    private string transname = null;
    /// <summary>
    /// 物品名字 (翻译)
    /// </summary>
    public string TranslateName
    {
        get
        {
            if (transname == null)
            {
                transname = LocalizeCore.Translate(Name);
            }
            return transname;
        }
    }

    /// <summary>
    /// 物品类型
    /// </summary>
    [Line(name: "itemtype")]
    public virtual string ItemType { get; set; } = "Item";


    /// <summary>
    /// 支持自定义的物品类型列表 (记得进行翻译 eg: Item_Item => 物品)
    /// </summary>

    public static List<String> ItemTypes = new List<string>()
    {
        //物品 - 默认分类
        "Item",
        //食物 - 可以吃的食物
        "Food",
        //道具 - 具有特殊功能的物品
        "Tool",
        //玩具 - 可以播放动画的物品
        "Toy",
    };

    /// <summary>
    /// 物品价格
    /// </summary>
    [Line(ignoreCase: true)]
    public double Price { get; set; }
    /// <summary>
    /// 描述
    /// </summary>
    [Line(ignoreCase: true)]
    public string Desc { get; set; }

    /// <summary>
    /// 显示的图片 (图片在 {itemtypes}/{itemname}.png )
    /// </summary>
    public BitmapImage ImageSource { get; set; }

    /// <summary>
    /// 物品个数
    /// </summary>
    public int Count { get; set; } = 1;
    /// <summary>
    /// 其他数据, 用于给程序储存个性化数据用
    /// </summary>
    public string Data { get; set; } = "";
    /// <summary>
    /// 能否使用
    /// </summary>
    [Line(ignoreCase: true)]
    public bool CanUse { get; set; } = true;

    
}
