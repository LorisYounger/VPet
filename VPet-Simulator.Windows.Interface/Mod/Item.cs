using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 物品的 Windows 半
/// </summary>
/// 基类子句只能写在这一半: NotifyPropertyChangedBase 是 Panuon 的类型,
/// 换基类会破坏已编译的 MOD(见 ABI-Compatibility.md 的 R2).
///
/// 留在这里的还有创建器与使用处理器(签名里有 IMainWindow)和位图相关的东西.
public partial class Item : NotifyPropertyChangedBase
{
    /// <summary>
    /// 创建物品方法
    /// </summary>
    /// <param name="data">物品数据</param>
    /// <returns>物品</returns>
    public static Item? CreateItem(IMainWindow imw, ILine data)
    {
        if (Creators.ContainsKey(data[(gstr)"itemtype"] ?? ""))
        {
            return Creators[data[(gstr)"itemtype"] ?? ""](imw, data);
        }
        else
        {
            return LPSConvert.DeserializeObject<Item>(data);
        }
    }

    /// <summary>
    /// 创建物品方法集合, 在这里添加自定义物品类型的创建方法 在LoadPlugin之后,GameLoaded之前. 请不要添加阻塞内容
    /// </summary>
    public static Dictionary<string, Func<IMainWindow, ILine, Item?>> Creators = new()
    {
        { "Food", (_,line) => { return LPSConvert.DeserializeObject<Food>(line); } },
    };

    /// <summary>
    /// 对应类型物品的使用方法 (物品/是否使用完成)
    /// </summary>
    public static Dictionary<string, List<Func<IMainWindow, Item, bool>>> UseAction = new();

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
    /// 显示的图片 (图片默认在 {itemtypes}/{itemname}.png )
    /// </summary>
    public virtual BitmapImage ImageSource { get; set; } = null!;

    /// <summary>
    /// 加载物品图片
    /// </summary>
    public virtual void LoadSource(IMainWindow imw)
    {
        ImageSource = imw.ImageSources.FindImage(ItemType + "_" + (Image ?? Name), "food");
    }

}
