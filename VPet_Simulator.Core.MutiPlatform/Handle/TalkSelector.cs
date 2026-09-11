using System;
using System.Collections.Generic;
using System.Linq;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 对话框选项的挑选与刷新
/// </summary>
/// 对应 Windows 版 WinDesign/TalkSelect.xaml.cs 里除界面之外的那部分: 抽哪几句、
/// 什么时候换一批、说过的不再出现、说完顺着 ToTags 接下一句. 与界面分开是为了让
/// 门禁不起 Avalonia 也能核对这套规则, 时间全部从外面传进来, 好核对.
///
/// 选项与桌宠状态的关系(CheckState 要用 PetMain)也从外面传, 这里不碰界面.
public class TalkSelector
{
    /// <summary>
    /// 一批选项能撑多少分钟
    /// </summary>
    public const int RefreshMinutes = 10;
    /// <summary>
    /// 每聊一句往后推多少分钟
    /// </summary>
    public const int ExtendMinutes = 5;
    /// <summary>
    /// 一批最多几句
    /// </summary>
    public const int MaxOptions = 5;

    private readonly Func<IEnumerable<SelectText>> source;
    private readonly Func<SelectText, bool> check;

    /// <summary>
    /// 当前存在在列表的选项
    /// </summary>
    private readonly List<SelectText> textList = new List<SelectText>();
    /// <summary>
    /// 已经说过的话
    /// </summary>
    private readonly HashSet<string> textSaid = new HashSet<string>();
    /// <summary>
    /// 下次刷新时间
    /// </summary>
    public DateTime RelsTime { get; private set; }
    private DateTime lastAddTime;

    /// <param name="source">全部可选的话</param>
    /// <param name="check">这句话现在能不能说 (对应 SelectText.CheckState)</param>
    public TalkSelector(Func<IEnumerable<SelectText>> source, Func<SelectText, bool> check)
    {
        this.source = source;
        this.check = check;
    }

    /// <summary>
    /// 现在能选的话
    /// </summary>
    /// Windows 版把没有 Choose 的话留在列表里却不显示, 发送时按下拉框序号去列表里取,
    /// 序号就会错位. 这里直接把能显示的那几句给出去, 界面拿到什么就发什么.
    public IReadOnlyList<SelectText> Options
        => textList.Where(x => x.Choose != null && !textSaid.Contains(x.Choose)).ToList();

    /// <summary>
    /// 到点了就换一批
    /// </summary>
    /// <param name="now">现在的时间</param>
    /// <returns>换了返回 true</returns>
    public bool Refresh(DateTime now)
    {
        if (RelsTime >= now)
            return false;
        //刷新选项
        RelsTime = now.AddMinutes(RefreshMinutes);//10分钟刷新一次, 每次聊天增加5分钟
        lastAddTime = now;
        textList.Clear();
        textSaid.Clear();
        //随机选取选项
        var list = source().ToList();
        while (list.Count > 0 && textList.Count < MaxOptions)
        {
            int sid = Function.Rnd.Next(list.Count);
            var select = list[sid];
            list.RemoveAt(sid);
            if (textList.Find(x => x.Choose == select.Choose) == null && check(select))
            {
                textList.Add(select);
            }
        }
        return true;
    }

    /// <summary>
    /// 离下次刷新还有几分钟
    /// </summary>
    public double RemainingMinutes(DateTime now) => (RelsTime - now).TotalMinutes;

    /// <summary>
    /// 这一批用掉了几成 (0 到 1)
    /// </summary>
    public double Progress(DateTime now)
    {
        double min = RemainingMinutes(now);
        double interval = (RelsTime - lastAddTime).TotalMinutes;
        double progress = 1 - min / interval;
        return Math.Min(1, Math.Max(0, progress));
    }

    /// <summary>
    /// 把一句话说出去: 从列表里拿掉, 记成说过, 往后推刷新时间, 再顺着 ToTags 接一句
    /// </summary>
    /// <param name="say">要说的那句, 必须来自 Options</param>
    /// <param name="now">现在的时间</param>
    /// <returns>确实在列表里返回 true; 数值结算和说话动画由调用方做</returns>
    public bool Take(SelectText say, DateTime now)
    {
        if (!textList.Remove(say))
            return false;

        if (say.Choose != null)
            textSaid.Add(say.Choose);
        RelsTime = RelsTime.AddMinutes(ExtendMinutes);
        lastAddTime = now;

        if (say.ToTags.Count > 0)
        {
            var list = source().Where(x => x.ContainsTag(say.ToTags)).ToList();
            while (list.Count > 0)
            {
                int sid = Function.Rnd.Next(list.Count);
                var select = list[sid];
                list.RemoveAt(sid);
                if (select.Choose != null && textList.Find(x => x.Choose == select.Choose) == null && !textSaid.Contains(select.Choose) && check(select))
                {
                    textList.Add(select);
                    break;
                }
            }
        }
        return true;
    }
}
