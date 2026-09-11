namespace VPet_Simulator.Core
{
    /// <summary>
    /// 静态图片动画契约
    /// </summary>
    public interface IPictureGraphBase : IGraphBase
    {
        int Length { get; }
    }

    /// <summary>
    /// 帧序列动画契约 (PNG/APNG)
    /// </summary>
    public interface IFrameSequenceGraphBase : IGraphBase
    {
        int FrameCount { get; }
        int FrameWidth { get; }
        int FrameHeight { get; }
    }

    /// <summary>
    /// 三层叠加动画契约 (FoodAnimation)
    /// </summary>
    public interface IFoodAnimationGraphBase : IGraphBase
    {
        string FrontLayerName { get; }
        string BackLayerName { get; }
        int FrameCount { get; }
    }
}
