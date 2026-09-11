using System;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 桌宠当前正在的状态 (平台无关)
    /// </summary>
    /// 数值必须与 Windows 版 Main.WorkingState 逐项一致, 两边是直接强制转换的
    internal enum PetWorkingState
    {
        /// <summary>
        /// 默认:啥都没干
        /// </summary>
        Nomal = 0,
        /// <summary>
        /// 正在干活/学习中
        /// </summary>
        Work = 1,
        /// <summary>
        /// 睡觉
        /// </summary>
        Sleep = 2,
        /// <summary>
        /// 旅游中
        /// </summary>
        Travel = 3,
        /// <summary>
        /// 其他状态,给开发者留个空位计算
        /// </summary>
        Empty = 4,
    }

    /// <summary>
    /// 工作类型 (平台无关)
    /// </summary>
    /// 数值必须与 GraphHelper.Work.WorkType 逐项一致
    internal enum PetWorkKind
    {
        Work = 0,
        Study = 1,
        Play = 2,
    }

    /// <summary>
    /// 数值模拟需要用到的工作参数
    /// </summary>
    internal interface IWorkDefinition
    {
        /// <summary>
        /// 工作类型
        /// </summary>
        PetWorkKind Kind { get; }
        /// <summary>
        /// 基础收入
        /// </summary>
        double MoneyBase { get; }
        /// <summary>
        /// 每单位时间消耗的饱腹度
        /// </summary>
        double StrengthFood { get; }
        /// <summary>
        /// 每单位时间消耗的口渴度
        /// </summary>
        double StrengthDrink { get; }
        /// <summary>
        /// 心情消耗系数
        /// </summary>
        double Feeling { get; }
    }

    /// <summary>
    /// 数值模拟需要从桌宠主体拿到的东西
    /// </summary>
    /// 这个接口刻意做得很窄: 只包含 FunctionSpend 真正用到的成员, 这样 Windows 侧
    /// 的 Main 只需要追加几个显式接口实现, 所有公开字段都保持原样不动 —— 一旦把
    /// 公开字段挪到基类或改成属性, 已编译的 MOD 就会失效.
    internal interface IPetStatHost
    {
        /// <summary>
        /// 游戏数据
        /// </summary>
        IGameSave Save { get; }
        /// <summary>
        /// 随机源
        /// </summary>
        /// 两个平台各有自己的 Function.Rnd(都是公开静态字段, MOD 可以替换),
        /// 共享代码不能直接引用其中任何一个, 所以从宿主拿
        Random Rnd { get; }
        /// <summary>
        /// 当前正在的状态
        /// </summary>
        PetWorkingState WorkingState { get; }
        /// <summary>
        /// 当前工作, 没有则为 null
        /// </summary>
        IWorkDefinition? CurrentWork { get; }
        /// <summary>
        /// 上次互动时间
        /// </summary>
        DateTime LastInteraction { get; set; }
        /// <summary>
        /// 累加工作已获得的数值 (工作计时器不存在时应当忽略)
        /// </summary>
        void AddWorkCount(double value);
        /// <summary>
        /// 触发数值结算事件
        /// </summary>
        void RaiseFunctionSpend();
        /// <summary>
        /// 播放状态切换动画
        /// </summary>
        void PlaySwitchAnimat(IGameSave.ModeType before, IGameSave.ModeType after);
        /// <summary>
        /// 因为状态变差而停止工作
        /// </summary>
        void StopWorkByStateFail();
    }
}
