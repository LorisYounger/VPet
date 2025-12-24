using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 物品 (这里的物品指的是能够在背包中查看和使用的物品)
/// </summary>
/// 注: 物品的使用方法均需要代码插件手写, 在 imw.TakeItem 中实现
public class Item : NotifyPropertyChangedBase
{
    /// <summary>
    /// 创建物品方法
    /// </summary>
    /// <param name="data">物品数据</param>
    /// <returns>物品</returns>
    public static Item CreateItem(ILine data)
    {
        if (Creators.ContainsKey(data[(gstr)"itemtype"]))
        {
            return Creators[data[(gstr)"itemtype"]](data);
        }
        else
        {
            return LPSConvert.DeserializeObject<Item>(data);
        }
    }
    /// <summary>
    /// 创建物品方法集合, 在这里添加自定义物品类型的创建方法 在LoadPlugin之后,GameLoaded之前. 请不要添加阻塞内容
    /// </summary>
    public static Dictionary<string, Func<ILine, Item>> Creators = new()
    {
        { "Food", (line) => { return LPSConvert.DeserializeObject<Food>(line); } },
    };
    /// <summary>
    /// 对应类型物品的使用方法
    /// </summary>
    public static Dictionary<string, List<Action<Item>>> UseAction = new();

    /// <summary>
    /// 使用该物品
    /// </summary>
    public void Use()
    {
        if (UseAction.ContainsKey(ItemType))
        {
            foreach (var action in UseAction[ItemType])
            {
                action(this);
            }
            return;
        }
        MessageBoxX.Show("物品 {0} 使用失败".Translate(TranslateName), "该物品无法使用".Translate());
    }
    /// <summary>
    /// 消耗该物品, 如果物品数量小于等于0时则销毁物品(从背包中移除) (不会主动调用)
    /// </summary>
    /// <param name="count">消耗数量</param>
    public void Consume(IMainWindow imw, int count = 1)
    {
        Count -= count;
        if (Count <= 0)
        {
            //销毁物品
            imw.Items.Remove(this);
        }
    }

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
    [Line(ignoreCase: true)]
    public int Count { get; set; } = 1;
    /// <summary>
    /// 其他数据, 用于给程序储存个性化数据用
    /// </summary>
    [Line(ignoreCase: true)]
    public string Data { get; set; } = "";
    /// <summary>
    /// 能否使用
    /// </summary>
    [Line(ignoreCase: true)]
    public bool CanUse { get; set; } = true;


}
