using Panuon.WPF.UI;
using LinePutScript.Localization.WPF;
using System;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 游戏存档 最新版: Windows 专属的那一半
    /// </summary>
    /// 主体在 VPet-Simulator.Windows.Interface.Base/Save/GameSave_v2.cs, 两个平台共享.
    /// 这里只放依赖 WPF 的部分.
    public partial class GameSave_v2
    {
        partial void ReportHashCheckError(Exception e)
        {
            MessageBoxX.Show(e.ToString(), "当前存档Hash验证信息".Translate() + ":" + "失败".Translate());
        }
    }
}
