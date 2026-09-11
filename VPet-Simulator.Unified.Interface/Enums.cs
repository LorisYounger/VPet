using System;

namespace VPet_Simulator.Unified.Interface;

// 契约自有的一套枚举.
//
// 为什么不直接用 Core 的: IGameSave.ModeType / GraphInfo.GraphType 这些类型在
// VPet-Simulator.Core.dll 和 VPet_Simulator.Core.Base.dll 里各有一份(共享源码的
// 必然结果), 契约引用哪一份都会把自己绑死在一边.
//
// 因此这里重新声明一套, 并要求**数值与真值来源逐项一致**, 两边的适配器直接
// (T)(int)v 转换. 新增成员时必须三处一起加(真值来源 / 这里 / 另一端), 同一个提交里.
//
//   PetGraphType   <- VPet_Simulator.Core.Base/Graph/GraphInfo.cs 的 GraphType
//   PetAnimatType  <- 同上的 AnimatType
//   PetWorkingState<- VPet-Simulator.Core/Display/MainLogic.cs 的 Main.WorkingState
//                     (跨平台侧是 PetMainLogic.cs 的 PetMain.WorkingState)
//   PetModeType    <- VPet_Simulator.Core.Base/Handle/IGameSave.cs 的 ModeType
//   PetFoodType    <- VPet-Simulator.Windows.Interface/Mod/Food.cs 的 FoodType
//   PetMenuType    <- VPet-Simulator.Core/Display/ToolBar.xaml.cs 的 MenuType
//   PetPhotoType   <- VPet-Simulator.Windows.Interface/Mod/Photo.cs 的 PhotoType

/// <summary>
/// 动画类型
/// </summary>
public enum PetGraphType
{
    Common,
    Raised_Dynamic,
    Raised_Static,
    Move,
    Default,
    Touch_Head,
    Touch_Body,
    Idel,
    Sleep,
    Say,
    StateONE,
    StateTWO,
    StartUP,
    Shutdown,
    Work,
    Switch_Up,
    Switch_Down,
    Switch_Thirsty,
    Switch_Hunger,
    SideHide_Left_Main,
    SideHide_Left_Rise,
    SideHide_Right_Main,
    SideHide_Right_Rise,
}

/// <summary>
/// 动画的动作段
/// </summary>
public enum PetAnimatType
{
    /// <summary>单段动画</summary>
    Single,
    /// <summary>开始段</summary>
    A_Start,
    /// <summary>循环段</summary>
    B_Loop,
    /// <summary>结束段</summary>
    C_End,
}

/// <summary>
/// 桌宠当前正在干什么
/// </summary>
public enum PetWorkingState
{
    /// <summary>默认:啥都没干</summary>
    Nomal,
    /// <summary>正在干活/学习中</summary>
    Work,
    /// <summary>睡觉</summary>
    Sleep,
    /// <summary>旅游中</summary>
    Travel,
    /// <summary>其他状态,给开发者留个空位计算</summary>
    Empty,
}

/// <summary>
/// 桌宠的状态模式
/// </summary>
public enum PetModeType
{
    /// <summary>高兴</summary>
    Happy,
    /// <summary>普通</summary>
    Nomal,
    /// <summary>状态不佳</summary>
    PoorCondition,
    /// <summary>生病</summary>
    Ill,
}

/// <summary>
/// 食物类型
/// </summary>
public enum PetFoodType
{
    /// <summary>食物 (默认)</summary>
    Food,
    /// <summary>收藏 (自定义)</summary>
    Star,
    /// <summary>正餐</summary>
    Meal,
    /// <summary>零食</summary>
    Snack,
    /// <summary>饮料</summary>
    Drink,
    /// <summary>功能性</summary>
    Functional,
    /// <summary>药品</summary>
    Drug,
    /// <summary>礼品</summary>
    Gift,
}

/// <summary>
/// 工具栏上的一级菜单
/// </summary>
public enum PetMenuType
{
    /// <summary>投喂</summary>
    Feed,
    /// <summary>互动</summary>
    Interact,
    /// <summary>自定</summary>
    DIY,
    /// <summary>系统</summary>
    Setting,
}

/// <summary>
/// 照片类型
/// </summary>
public enum PetPhotoType
{
    /// <summary>全部</summary>
    ALL,
    /// <summary>插图</summary>
    Illustration,
    /// <summary>缩略图</summary>
    Thumbnail,
}

/// <summary>
/// 说话文本的种类
/// </summary>
/// 契约独有: Windows 版是按行名(lowfoodtext / clicktext ...)区分的, 这里做成枚举
/// 好让 MOD 一个方法就能取到想要的那一类.
public enum PetTextKind
{
    /// <summary>饿了会说的话</summary>
    LowFood,
    /// <summary>渴了会说的话</summary>
    LowDrink,
    /// <summary>点击时说的话</summary>
    Click,
    /// <summary>对话框里的选项</summary>
    Select,
}
