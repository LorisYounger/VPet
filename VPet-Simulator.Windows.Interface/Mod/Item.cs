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
using System.Windows.Media;
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
    public static Item CreateItem(IMainWindow imw, ILine data)
    {
        if (Creators.ContainsKey(data[(gstr)"itemtype"]))
        {
            return Creators[data[(gstr)"itemtype"]](imw, data);
        }
        else
        {
            return LPSConvert.DeserializeObject<Item>(data);
        }
    }
    /// <summary>
    /// 创建物品方法集合, 在这里添加自定义物品类型的创建方法 在LoadPlugin之后,GameLoaded之前. 请不要添加阻塞内容
    /// </summary>
    public static Dictionary<string, Func<IMainWindow, ILine, Item>> Creators = new()
    {
        { "Food", (_,line) => { return LPSConvert.DeserializeObject<Food>(line); } },
    };
    /// <summary>
    /// 对应类型物品的使用方法 (物品/是否使用完成)
    /// </summary>
    public static Dictionary<string, List<Func<IMainWindow, Item, bool>>> UseAction = new();
    /// <summary>
    /// 物品图片 (图片默认在 {itemtypes}/{Image or itemname}.png )
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual string Image { get; set; } = null;
    /// <summary>
    /// 使用该物品
    /// </summary>
    public virtual void Use(IMainWindow imw)
    {
        if (UseAction.ContainsKey(ItemType))
        {
            foreach (var action in UseAction[ItemType])
            {
                if (action(imw, this))
                    return;
            }
            return;
        }
        MessageBoxX.Show("物品 {0} 使用失败".Translate(TranslateName), "该物品无法使用".Translate());
    }
    /// <summary>
    /// 消耗该物品, 如果物品数量小于等于0时则销毁物品(从背包中移除) (不会主动调用)
    /// </summary>
    /// <param name="count">消耗数量</param>
    public virtual void Consume(IMainWindow imw, int count = 1)
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
    private string transdesc = null;
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
    /// 描述 (翻译后)
    /// </summary>

    public virtual string Description
    {
        get
        {
            if (transdesc == null)
            {
                transdesc = LocalizeCore.Translate(Desc);
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
    public string Desc { get; set; }

    /// <summary>
    /// 显示的图片 (图片默认在 {itemtypes}/{itemname}.png )
    /// </summary>
    public virtual BitmapImage ImageSource { get; set; }

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
    /// <summary>
    /// 加载物品图片
    /// </summary>
    public virtual void LoadSource(IMainWindow imw)
    {
        ImageSource = imw.ImageSources.FindImage(ItemType + "_" + (Image ?? Name), "food");
    }
}
