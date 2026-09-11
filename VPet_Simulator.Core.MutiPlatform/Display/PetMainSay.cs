using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 桌宠主体: 说话
/// </summary>
/// 对应 Windows 版 MainLogic.cs 里的 Say 系列. 逐方法对应.
public partial class PetMain
{
    /// <summary>
    /// 消息栏
    /// </summary>
    /// <summary>
    /// 消息栏
    /// </summary>
    /// 类型是接口而不是 MessageBar, 且允许宿主替换 —— 多人联机时访客桌宠要挂一个
    /// 不一样的消息栏. 赋值时会自动把旧的从前景层摘掉、把新的挂上去.
    public IMassageBar? MsgBar
    {
        get => msgBar;
        set
        {
            if (ReferenceEquals(msgBar, value))
                return;
            if (msgBar != null)
                UIGrid.Children.Remove(msgBar.This);
            msgBar = value;
            if (msgBar != null && !UIGrid.Children.Contains(msgBar.This))
                UIGrid.Children.Add(msgBar.This);
        }
    }

    private IMassageBar? msgBar;

    /// <summary>
    /// 处理说话内容
    /// </summary>
    public event Action<string>? OnSay;

    /// <summary>
    /// 随机表情的方法, 修改这个方法可以使用指定类型的说话表情
    /// </summary>
    public Func<string, string> SayRndFunction;

    /// <summary>
    /// 说话处理 (请不要阻塞该处理)
    /// </summary>
    public List<Action<SayInfo>> SayProcess = new List<Action<SayInfo>>();

    /// <summary>
    /// 建立工作计时器/工具栏/消息栏并挂到前景层上
    /// </summary>
    /// 必须在 UI 线程调用. 添加顺序与 Windows 版 Main.Load_0_BaseConsole 一致,
    /// 顺序决定了三者互相遮挡时谁在上面.
    private void Load_0_BaseConsole()
    {
        WorkTimer = new WorkTimer(this) { IsVisible = false };
        UIGrid.Children.Add(WorkTimer);
        ToolBar = new ToolBar(this) { IsVisible = false };
        UIGrid.Children.Add(ToolBar);
        MsgBar = new MessageBar(this);
    }

    /// <summary>
    /// 说话,使用随机表情
    /// </summary>
    public void SayRnd(string text, bool force = false, string? desc = null)
    {
        Say(text, SayRndFunction(text), force, desc);
    }

    /// <summary>
    /// 处理sayInfo,使用随机表情
    /// </summary>
    /// <param name="sayInfo">SayInfoWithStream Class 用于提供stream基本信息 以及基本方法</param>
    public void SayRnd(SayInfoWithStream sayInfo)
    {
        Task.Run(() =>
        {
            while (!sayInfo.IsFinishGen && Function.ComCheck(sayInfo.CurrentText.ToString()) < 4 && sayInfo.CurrentText.Length < 80)
            {
                Thread.Sleep(100);
            }
            sayInfo.GraphName = SayRndFunction(sayInfo.CurrentText.ToString());
            if (sayInfo.IsFinishGen)
                Say(sayInfo.ToNoneStream().Result);
            else
                Say(sayInfo);
        });
    }

    /// <summary>
    /// 流式传输的说话
    /// </summary>
    /// <param name="sayInfoWithStream">说话信息</param>
    public void Say(SayInfoWithStream sayInfoWithStream)
    {
        Task.Run(() =>
        {
            sayInfoWithStream.Event_Finish += (text) => OnSay?.Invoke(text);

            if (sayInfoWithStream.IsFinishGen)
            {
                OnSay?.Invoke(sayInfoWithStream.CurrentText.ToString());
            }

            SayProcess.ForEach(a => a.Invoke(sayInfoWithStream));

            //这里不使用idle是因为idle包括学习等
            if (sayInfoWithStream.Force || !string.IsNullOrWhiteSpace(sayInfoWithStream.GraphName) && DisplayType?.Type == GraphType.Default)
                Display(sayInfoWithStream.GraphName, AnimatType.A_Start, () =>
                {
                    MsgBar?.Show(Core.Save!.Name, sayInfoWithStream);
                    DisplayBLoopingForce(sayInfoWithStream.GraphName!);
                });
            else
            {
                MsgBar?.Show(Core.Save!.Name, sayInfoWithStream);
            }
        });
    }

    /// <summary>
    /// 普通说话
    /// </summary>
    /// <param name="sayinfo">说话信息</param>
    public void Say(SayInfoWithOutStream sayinfo)
    {
        Task.Run(() =>
        {
            OnSay?.Invoke(sayinfo.Text);

            SayProcess.ForEach(a => a.Invoke(sayinfo));

            //这里不使用idle是因为idle包括学习等
            if (sayinfo.Force || !string.IsNullOrWhiteSpace(sayinfo.GraphName) && DisplayType?.Type == GraphType.Default)
                Display(sayinfo.GraphName, AnimatType.A_Start, () =>
                {
                    MsgBar?.Show(Core.Save!.Name, sayinfo.Text, sayinfo.GraphName,
                        sayinfo.MsgContent, sayinfo.Desc);
                    DisplayBLoopingForce(sayinfo.GraphName!);
                });
            else
            {
                MsgBar?.Show(Core.Save!.Name, sayinfo.Text, sayinfo.GraphName,
                    sayinfo.MsgContent, sayinfo.Desc);
            }
        });
    }

    /// <summary>
    /// 说话
    /// </summary>
    /// <param name="text">说话内容</param>
    /// <param name="graphname">图像名</param>
    /// <param name="force">强制显示图像</param>
    /// <param name="desc">描述</param>
    public void Say(string text, string? graphname = null, bool force = false, string? desc = null) => Say(new SayInfoWithOutStream()
    {
        Text = text,
        GraphName = graphname,
        Desc = desc,
        Force = force,
        MsgContent = null
    });

    /// <summary>
    /// 说话
    /// </summary>
    /// <param name="text">说话内容</param>
    /// <param name="msgcontent">消息内容</param>
    /// <param name="graphname">图像名</param>
    /// <param name="force">强制显示图像</param>
    public void Say(string text, Control msgcontent, string? graphname = null, bool force = false) => Say(new SayInfoWithOutStream()
    {
        Text = text,
        GraphName = graphname,
        Desc = null,
        Force = force,
        MsgContent = msgcontent
    });
}
