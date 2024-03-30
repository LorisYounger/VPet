using LinePutScript.Converter;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 低状态自动说的话
    /// </summary>
    public class LowText : IText
    {
        /// <summary>
        /// 状态
        /// </summary>
        public enum ModeType
        {
            /// <summary>
            /// 高状态: 开心/普通
            /// </summary>
            H,
            /// <summary>
            /// 低状态: 低状态/生病
            /// </summary>
            L,
        }
        /// <summary>
        /// 状态
        /// </summary>
        [Line(IgnoreCase = true)] public ModeType Mode { get; set; } = ModeType.L;
        /// <summary>
        /// 体力
        /// </summary>
        public enum StrengthType
        {
            /// <summary>
            /// 一般口渴/饥饿
            /// </summary>
            L,
            /// <summary>
            /// 有点口渴/饥饿
            /// </summary>
            M,
            /// <summary>
            /// 非常口渴/饥饿
            /// </summary>
            S,
        }
        /// <summary>
        /// 体力
        /// </summary>
        [Line(IgnoreCase = true)] public StrengthType Strength { get; set; } = StrengthType.S;
        /// <summary>
        /// 好感度要求
        /// </summary>
        public enum LikeType
        {
            /// <summary>
            /// 不需要好感度
            /// </summary>
            N,
            /// <summary>
            /// 低好感度需求
            /// </summary>
            S,
            /// <summary>
            /// 中好感度需求
            /// </summary>
            M,
            /// <summary>
            /// 高好感度
            /// </summary>
            L,
        }
        /// <summary>
        /// 好感度要求
        /// </summary>
        [Line(IgnoreCase = true)] public LikeType Like { get; set; } = LikeType.N;
    }
}
