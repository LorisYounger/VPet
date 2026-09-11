using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 把文本里的占位符换成存档里的实际值
    /// </summary>
    /// 共享源码: 占位符表两个平台必须一模一样, 否则同一份 MOD 文本在两边显示出来
    /// 不一样 —— 少认一个占位符的表现是玩家看到一串 {money} 而不是数字。
    ///
    /// 从 Windows 版 IText.ConverText 抽出来的, 只依赖 IGameSave, 与界面无关。
    public static class SaveTextTemplate
    {
        /// <summary>
        /// 把占位符换成实际值
        /// </summary>
        /// <param name="text">原文</param>
        /// <param name="save">存档</param>
        /// 先判有没有花括号再动手是有意的: 绝大多数文本没有占位符, 省掉九次
        /// Replace 比看上去值 —— 这是每句话都会走的路。
        public static string Convert(string text, IGameSave save)
        {
            if (!text.Contains('{') || !text.Contains('}'))
                return text;
            return text.Replace("{name}", save.Name)
                .Replace("{food}", save.StrengthFood.ToString("f0"))
                .Replace("{drink}", save.StrengthDrink.ToString("f0"))
                .Replace("{feel}", save.Feeling.ToString("f0"))
                .Replace("{strength}", save.Strength.ToString("f0"))
                .Replace("{money}", save.Money.ToString("f0"))
                .Replace("{level}", save.Level.ToString("f0"))
                .Replace("{health}", save.Health.ToString("f0"))
                .Replace("{hostname}", save.HostName);
        }
    }
}
