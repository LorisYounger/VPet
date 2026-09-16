using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Unified.Interface;

/// <summary>
/// 一份食物
/// </summary>
/// 这是对宿主里那个真食物对象的**实时视图**, 不是快照: 宿主在 MOD 加载之后还会
/// 改它(价格钳制、收藏状态、吃腻度), 拿 ILine 抄一份出来的话 MOD 看到的就是旧值,
/// MOD 改的也传不回去.
public interface IFoodInfo
{
    /// <summary>名字, 也是这份食物的主键</summary>
    string Name { get; }
    /// <summary>翻译后的名字</summary>
    string TranslateName { get; }
    PetFoodType Type { get; set; }

    int Exp { get; set; }
    double Strength { get; set; }
    double StrengthFood { get; set; }
    double StrengthDrink { get; set; }
    double Feeling { get; set; }
    double Health { get; set; }
    double Likability { get; set; }

    /// <summary>价格</summary>
    double Price { get; set; }
    /// <summary>描述</summary>
    string Desc { get; set; }
    /// <summary>食用时播的动画, 留空则按类型推断</summary>
    string? Graph { get; set; }
    /// <summary>图片名, 留空则用食物名去找</summary>
    string? Image { get; }
    /// <summary>已解析出来的图片绝对路径, 找不到则为 null</summary>
    string? ImagePath { get; }
    /// <summary>是否已收藏</summary>
    bool Star { get; set; }

    /// <summary>取食用时该播的动画名</summary>
    string GetGraph();
    /// <summary>按当前数值算出的推荐价格</summary>
    double RealPrice { get; }
    /// <summary>是不是超模(定价远低于推荐价)</summary>
    bool IsOverLoad();
    /// <summary>序列化成一行</summary>
    ILine ToLine();
}

/// <summary>
/// 背包里的一件物品
/// </summary>
public interface IItemInfo
{
    string Name { get; }
    string TranslateName { get; }
    /// <summary>物品类型, 决定用它时走哪个处理器</summary>
    string ItemType { get; set; }
    string Desc { get; set; }
    /// <summary>给界面显示的完整描述</summary>
    string Description { get; }
    double Price { get; set; }
    int Count { get; set; }
    /// <summary>MOD 自己往里塞东西的地方, 会随存档保存</summary>
    string Data { get; set; }
    bool CanUse { get; set; }
    bool Star { get; set; }
    /// <summary>是不是用一次就没了</summary>
    bool IsSingle { get; set; }
    /// <summary>在背包里是否可见</summary>
    bool Visibility { get; set; }
    string? Image { get; }
    string? ImagePath { get; }

    /// <summary>这件物品同时也是食物时返回它, 否则 null</summary>
    IFoodInfo? AsFood { get; }
    /// <summary>用掉它</summary>
    void Use();
    /// <summary>消耗指定数量</summary>
    void Consume(int count = 1);
    /// <summary>序列化成一行</summary>
    ILine ToLine();
}

/// <summary>
/// MOD 自己造出来的物品
/// </summary>
/// 创建器返回它, 宿主再按自己的类型体系把它实例化. 之所以不让创建器直接返回
/// IItemInfo: 那样 MOD 就得自己实现一整个接口, 而它其实只是想描述一件东西.
///
/// [Line] 的子项名与 Windows 版 Item 逐字一致, 存档格式两边通用.
public class UnifiedItem
{
    //name 和 itemtype 这两个子项名在 Windows 版里是写死的小写(Item.cs 的
    //[Line(name: "name")] / [Line(name: "itemtype")]), 其余的保持 PascalCase.
    //照抄是硬要求: 名字不一样存档就没法在两个平台之间拷。
    [Line(name: "name")] public string Name { get; set; } = string.Empty;
    [Line(name: "itemtype")] public string ItemType { get; set; } = "Item";
    [Line(ignoreCase: true)] public string Desc { get; set; } = string.Empty;
    [Line(ignoreCase: true)] public double Price { get; set; }
    [Line(ignoreCase: true)] public int Count { get; set; } = 1;
    [Line(ignoreCase: true)] public string Data { get; set; } = string.Empty;
    [Line(ignoreCase: true)] public bool CanUse { get; set; } = true;
    [Line(ignoreCase: true)] public bool Star { get; set; }
    [Line(ignoreCase: true)] public bool IsSingle { get; set; }
    [Line(ignoreCase: true)] public bool Visibility { get; set; } = true;
    [Line(ignoreCase: true)] public string? Image { get; set; }
}

/// <summary>
/// 一张收藏照片
/// </summary>
public interface IPhotoInfo
{
    string Name { get; }
    string TranslateName { get; }
    string Description { get; }
    string Tags { get; }
    PetPhotoType Type { get; }
    /// <summary>是否已解锁</summary>
    bool IsUnlock { get; }
    /// <summary>是否已标为喜欢</summary>
    bool IsStar { get; set; }
    /// <summary>解锁它</summary>
    void Unlock();
}

/// <summary>
/// 一条说话文本
/// </summary>
public interface ITextInfo
{
    PetTextKind Kind { get; }
    /// <summary>原文, 同时也是翻译键</summary>
    string Text { get; }
    /// <summary>翻译后的文本</summary>
    string TranslateText { get; }
    /// <summary>内容标签</summary>
    string Tag { get; }
    ILine ToLine();
}

/// <summary>
/// 一份工作/学习/玩耍
/// </summary>
public interface IWorkInfo
{
    string Name { get; }
    string TranslateName { get; }
    /// <summary>Work / Study / Play</summary>
    string Type { get; }
    /// <summary>基础收益</summary>
    double MoneyBase { get; }
    /// <summary>时长(分钟)</summary>
    int Time { get; }
    /// <summary>等级要求</summary>
    int LevelLimit { get; }
}

/// <summary>
/// 一个 MOD 的元信息
/// </summary>
public interface IPluginInfo
{
    string Name { get; }
    string Author { get; }
    long AuthorID { get; }
    ulong ItemID { get; }
    string Intro { get; }
    int GameVer { get; }
    int Ver { get; }
    /// <summary>MOD 目录</summary>
    string Path { get; }
    /// <summary>这个 MOD 提供了哪些内容</summary>
    IReadOnlyCollection<string> Tag { get; }
    /// <summary>玩家是否启用了它</summary>
    bool IsEnabled { get; }
}
