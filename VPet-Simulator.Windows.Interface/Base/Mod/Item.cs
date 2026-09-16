using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 物品 (这里的物品指的是能够在背包中查看和使用的物品)
/// </summary>
/// 注: 物品的使用方法均需要代码插件手写, 在 imw.TakeItem 中实现
public partial class Item
{
    /// <summary>
    /// 物品图片 (图片默认在 {itemtypes}/{Image or itemname}.png )
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual string? Image { get; set; } = null;

    /// <summary>
    /// 物品名字 (ID)
    /// </summary>
    [Line(name: "name")]
    public string Name { get; set; } = string.Empty;
    private string? transname = null;
    private string? transdesc = null;
    /// <summary>
    /// 物品名字 (翻译)
    /// </summary>
    public string TranslateName
    {
        get
        {
            if (transname == null)
            {
                transname = Name.Translate();
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
    /// 描述 (翻译后)
    /// </summary>

    public virtual string Description
    {
        get
        {
            if (transdesc == null)
            {
                transdesc = Desc.Translate();
            }
            return transdesc;
        }
    }

    /// <summary>
    /// 支持自定义的物品类型列表 (记得进行翻译 eg: Item_Item => 物品)
    /// </summary>

    public static List<string> ItemTypes = new List<string>()
    {
        //物品 - 默认分类
        "Item",
        //食物 - 可以吃的食物 (也可以指代物品)
        "Food",
        //道具 - 具有特殊功能的物品
        "Tool",
        //玩具 - 可以播放动画的物品
        "Toy",
        //邮件 - 打开后可以获得物品的信件
        "Mail",
    };

    /// <summary>
    /// 物品价格
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual double Price { get; set; }
    /// <summary>
    /// 描述
    /// </summary>
    [Line(ignoreCase: true)]
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 物品个数
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual int Count { get; set; } = 1;
    /// <summary>
    /// 其他数据, 用于给程序储存个性化数据用
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual string Data { get; set; } = "";
    /// <summary>
    /// 能否使用
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual bool CanUse { get; set; } = true;
    /// <summary>
    /// 是否收藏了物品
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual bool Star { get; set; } = false;
    /// <summary>
    /// 是否为单个物品 (不可堆叠) (同时使用不会被消耗) (注: 无论这里标注消不消耗, 最终的消耗逻辑都需要在 Use 方法中自行实现)
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual bool IsSingle { get; set; } = false;

    /// <summary>
    /// 能否在背包中显示
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual bool Visibility { get; set; } = true;
}
