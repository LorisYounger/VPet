using LinePutScript;
using System;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using VPet_Simulator.MutiPlatform.Display;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 桌宠窗口: 选项式聊天
/// </summary>
/// 对应 Windows 版 TalkSelect.xaml.cs 里 btn_Send_Click 的结算部分和 MainWindow 里
/// 把聊天框挂进工具栏那一段. 挑选规则在 Core 的 TalkSelector, 界面在 Display/TalkSelect.
public partial class PetWindow
{
    private TalkSelect? talkSelect;

    /// <summary>
    /// 把选项式聊天框挂进工具栏
    /// </summary>
    private void LoadTalkSelect(PetMain m)
    {
        var selector = new TalkSelector(() => resources.SelectTexts, x => x.CheckState(m));
        talkSelect = new TalkSelect(selector, SendTalk);
        m.ToolBar.MainGrid.Children.Add(talkSelect);
        m.ToolBar.EventShow += talkSelect.RelsSelect;
    }

    /// <summary>
    /// 玩家选了一句话说出去
    /// </summary>
    /// 统计项名与 Windows 版逐字一致, 年度报告和成就才对得上号.
    /// Windows 版还往活动日志里记一条 hostsay, 跨平台这边没有那个日志窗口, 记进普通日志.
    private void SendTalk(SelectText say)
    {
        if (core?.Save is not IGameSave save || pet == null)
            return;
        pet.ToolBar.CloseTimer.Enabled = false;
        pet.ToolBar.IsVisible = false;

        //添加日志
        if (say.TranslateChoose != null)
            Log($"[hostsay] {say.TranslateChoose}");
        //聊天效果
        var statistics = gameSavesData.Statistics;
        if (say.Exp != 0)
        {
            if (say.Exp > 0)
                statistics[(gint)"stat_say_exp_p"]++;
            else
                statistics[(gint)"stat_say_exp_d"]++;
        }
        if (say.Likability != 0)
        {
            if (say.Likability > 0)
                statistics[(gint)"stat_say_like_p"]++;
            else
                statistics[(gint)"stat_say_like_d"]++;
        }
        if (say.Money != 0)
        {
            if (say.Money > 0)
                statistics[(gint)"stat_say_money_p"]++;
            else
                statistics[(gint)"stat_say_money_d"]++;
        }
        save.EatFood(say);
        save.Money += say.Money;

        pet.SayRnd(say.TranslateTextConvert(save), desc: FoodToDescription(say));
    }
}
