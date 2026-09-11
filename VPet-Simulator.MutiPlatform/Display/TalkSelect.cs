using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using VPet_Simulator.Core.MutiPlatform;

namespace VPet_Simulator.MutiPlatform.Display;

/// <summary>
/// 选项式聊天框
/// </summary>
/// 对应 Windows 版 WinDesign/TalkSelect.xaml: 挂在工具栏顶部, 一个下拉框选话、
/// 一个发送按钮、底下一条进度条画这批选项还剩多久换.
/// 挑选规则在 TalkSelector 里, 这里只管画和转发.
internal sealed class TalkSelect : UserControl
{
    private readonly TalkSelector selector;
    private readonly Action<SelectText> send;
    private readonly ComboBox tbTalk;
    private readonly Button btn_Send;
    private readonly ProgressBar PrograssUsed;
    /// <summary>
    /// 下拉框里每一项对应的话, 与 tbTalk 的项同序
    /// </summary>
    private readonly List<SelectText> shown = new List<SelectText>();

    /// <param name="selector">挑选规则</param>
    /// <param name="send">玩家点了发送, 说的是哪句</param>
    internal TalkSelect(TalkSelector selector, Action<SelectText> send)
    {
        this.selector = selector;
        this.send = send;
        Width = 500;
        Height = 500;
        VerticalAlignment = VerticalAlignment.Top;

        tbTalk = new ComboBox
        {
            PlaceholderText = LocalizeCore.Translate("和桌宠说"),
            FontSize = 30,
            MaxDropDownHeight = 500,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        btn_Send = new Button
        {
            Content = LocalizeCore.Translate("发送"),
            FontSize = 30,
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        btn_Send.Bind(BackgroundProperty, btn_Send.GetResourceObservable("PrimaryLight"));
        //SimpleTheme 的按钮默认白字, 在浅蓝底上看不见; Windows 版用的是默认深色字
        btn_Send.Bind(ForegroundProperty, btn_Send.GetResourceObservable("DARKPrimary"));
        btn_Send.Bind(BorderBrushProperty, btn_Send.GetResourceObservable("DARKPrimaryDarker"));
        btn_Send.Click += (_, _) => Send();
        PrograssUsed = new ProgressBar
        {
            Height = 10,
            Minimum = 0,
            Maximum = 1,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(4),
        };
        PrograssUsed.Bind(ForegroundProperty, PrograssUsed.GetResourceObservable("ProgressBarForeground"));
        PrograssUsed.Bind(BackgroundProperty, PrograssUsed.GetResourceObservable("Secondary"));
        PrograssUsed.Bind(BorderBrushProperty, PrograssUsed.GetResourceObservable("DARKPrimary"));

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("4*,5,1*"),
            RowDefinitions = new RowDefinitions("*,2,Auto"),
        };
        grid.Children.Add(tbTalk);
        Grid.SetColumn(btn_Send, 2);
        grid.Children.Add(btn_Send);
        Grid.SetRow(PrograssUsed, 2);
        Grid.SetColumnSpan(PrograssUsed, 3);
        grid.Children.Add(PrograssUsed);

        var border = new Border
        {
            BorderThickness = new Thickness(5),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(5),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(5),
            Child = grid,
        };
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable("SecondaryLighter"));
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("Secondary"));
        Content = new Grid { Children = { border } };
        RelsSelect();
    }

    /// <summary>
    /// 刷新当前所有选项
    /// </summary>
    /// 工具栏每次弹出来都调一次, 与 Windows 版挂在 ToolBar.EventShow 上一致
    internal void RelsSelect()
    {
        var now = DateTime.Now;
        selector.Refresh(now);
        //刷新显示
        shown.Clear();
        shown.AddRange(selector.Options);
        var items = new List<string>();
        if (shown.Count > 0)
        {
            foreach (var item in shown)
                items.Add(item.TranslateChoose!);
            btn_Send.IsEnabled = true;
        }
        else
        {
            items.Add(LocalizeCore.Translate("没有可以说的话"));
            btn_Send.IsEnabled = false;
        }
        tbTalk.ItemsSource = items;
        double min = selector.RemainingMinutes(now);
        PrograssUsed.Value = selector.Progress(now);
        ToolTip.SetTip(PrograssUsed, LocalizeCore.Translate("下次刷新剩余时间: {0:f1}分钟", min));
    }

    private void Send()
    {
        int index = tbTalk.SelectedIndex;
        if (index < 0 || index >= shown.Count)
            return;
        var say = shown[index];
        if (!selector.Take(say, DateTime.Now))
            return;
        send(say);
        RelsSelect();
    }
}
