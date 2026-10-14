using LinePutScript.Localization.WPF;
using System;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows;

/// <summary>工具栏图标入口与独立浮层；仅在打开时读取 Buff，不参与业务结算。</summary>
public partial class BuffViewer : Border, IDisposable
{
    private const int MaxVisibleEffects = 9;
    private const int IconsPerRow = 5;
    private readonly MainWindow mw;
    private readonly UniformGrid icons = new();
    private readonly Popup popup;
    private readonly DispatcherTimer hoverDelay;
    private readonly DispatcherTimer countdown;
    private bool pinned;
    private int category;
    internal Label Entry { get; } = new();

    public BuffViewer(MainWindow mw)
    {
        this.mw = mw;
        InitializeComponent();
        Entry.Content = icons;
        Entry.Padding = new Thickness(0);
        Entry.Background = Brushes.Transparent;
        Entry.HorizontalAlignment = HorizontalAlignment.Right;
        Entry.VerticalAlignment = VerticalAlignment.Center;
        Entry.Visibility = Visibility.Collapsed;
        Entry.Focusable = true;
        Grid.SetColumn(Entry, 2);
        Grid.SetColumnSpan(Entry, 3);
        var panel = mw.Main.ToolBar.gdPanel;
        panel.RowDefinitions[0].MinHeight = 30;
        panel.RowDefinitions[0].Height = GridLength.Auto;

        // Popup 不参与 Main 的 Viewbox 缩放、布局和裁剪，使用 WPF 的屏幕边缘放置。
        popup = new Popup
        {
            Child = this,
            PlacementTarget = Entry,
            Placement = PlacementMode.Bottom,
            VerticalOffset = 4,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = true
        };
        mw.Main.UIGrid.Children.Add(popup);
        countdown = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = TimeSpan.FromSeconds(1) };
        countdown.Tick += (_, _) => UpdateRemainingTime();
        hoverDelay = new DispatcherTimer(DispatcherPriority.Input, Dispatcher) { Interval = TimeSpan.FromMilliseconds(200) };
        hoverDelay.Tick += (_, _) =>
        {
            hoverDelay.Stop();
            if (Entry.IsMouseOver || IsMouseOver) popup.IsOpen = true;
            else if (!pinned) popup.IsOpen = false;
        };
        popup.Opened += (_, _) =>
        {
            RefreshEntry();
            if (!popup.IsOpen) return;
            Refresh();
            mw.Main.ToolBar.CloseTimer.Stop();
            mw.Main.ToolBar.closePanelTimer.Stop();
        };
        popup.Closed += Popup_Closed;
        Entry.MouseEnter += (_, _) =>
        {
            hoverDelay.Stop();
            if (!popup.IsOpen)
            {
                hoverDelay.Interval = TimeSpan.FromMilliseconds(200);
                hoverDelay.Start();
            }
        };
        Entry.MouseLeave += (_, _) =>
        {
            if (!pinned)
            {
                hoverDelay.Interval = TimeSpan.FromMilliseconds(600);
                hoverDelay.Start();
            }
        };
        Entry.MouseLeftButtonDown += (_, e) => { e.Handled = true; TogglePin(); };
        Entry.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Space) { e.Handled = true; TogglePin(); }
        };
        MouseEnter += (_, _) => hoverDelay.Stop();
        MouseLeave += (_, _) =>
        {
            if (!pinned)
            {
                hoverDelay.Interval = TimeSpan.FromMilliseconds(600);
                hoverDelay.Start();
            }
        };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { popup.IsOpen = false; e.Handled = true; }
        };
        Entry.Loaded += Entry_Loaded;
        Entry.Unloaded += Entry_Unloaded;
    }

    private void Entry_Loaded(object sender, RoutedEventArgs e)
    {
        mw.Main.ToolBar.EventMenuPanelShow += RefreshEntry;
        mw.Main.ToolBar.MouseLeave += PauseAutoHide;
        mw.Main.ToolBar.BdrPanel.MouseLeave += PauseAutoHide;
        mw.Main.ToolBar.gdPanel.IsVisibleChanged += Panel_IsVisibleChanged;
        mw.PreviewMouseDown += Host_PreviewMouseDown;
        mw.Deactivated += Host_Deactivated;
        if (mw.Main.ToolBar.gdPanel.IsVisible) RefreshEntry();
    }

    private void Entry_Unloaded(object sender, RoutedEventArgs e)
    {
        popup.IsOpen = false;
        hoverDelay.Stop();
        countdown.Stop();
        mw.Main.ToolBar.EventMenuPanelShow -= RefreshEntry;
        mw.Main.ToolBar.MouseLeave -= PauseAutoHide;
        mw.Main.ToolBar.BdrPanel.MouseLeave -= PauseAutoHide;
        mw.Main.ToolBar.gdPanel.IsVisibleChanged -= Panel_IsVisibleChanged;
        mw.PreviewMouseDown -= Host_PreviewMouseDown;
        mw.Deactivated -= Host_Deactivated;
    }

    private void PauseAutoHide(object sender, MouseEventArgs e)
    {
        if (!popup.IsOpen) return;
        mw.Main.ToolBar.CloseTimer.Stop();
        mw.Main.ToolBar.closePanelTimer.Stop();
    }

    private void Popup_Closed(object? sender, EventArgs e)
    {
        pinned = false;
        popup.StaysOpen = true;
        hoverDelay.Stop();
        countdown.Stop();
        if (IsKeyboardFocusWithin) Entry.Focus();
        if (Entry.IsLoaded && mw.Main.ToolBar.IsVisible && !mw.Main.ToolBar.IsMouseOver)
        {
            mw.Main.ToolBar.CloseTimer.Start();
            mw.Main.ToolBar.closePanelTimer.Start();
        }
    }

    private void TogglePin()
    {
        hoverDelay.Stop();
        if (pinned) popup.IsOpen = false;
        else
        {
            pinned = true;
            popup.StaysOpen = false;
            popup.IsOpen = true;
            (category == 1 ? BenefitTab : category == 2 ? DebuffTab : category == 3 ? SpecialTab : AllTab).Focus();
        }
    }

    private void Panel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!mw.Main.ToolBar.gdPanel.IsVisible) popup.IsOpen = false;
    }

    private void Host_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!Entry.IsMouseOver && !IsMouseOver) popup.IsOpen = false;
    }

    private void Host_Deactivated(object? sender, EventArgs e) => popup.IsOpen = false;

    private void RefreshEntry()
    {
        int count = 0, iconIndex = 0;
        for (int i = 0; i < mw.Buff.Effects.Count; i++)
        {
            var effect = mw.Buff.Effects[i];
            if (effect.IsExpired) continue;
            count++;
            if (iconIndex >= MaxVisibleEffects) continue;
            // 入口无需保存第二份效果列表，直接复用已呈现的控件。
            TextBlock icon;
            if (iconIndex < icons.Children.Count && ReferenceEquals(((TextBlock)icons.Children[iconIndex]).Tag, effect))
                icon = (TextBlock)icons.Children[iconIndex];
            else
            {
                icon = new TextBlock { Tag = effect, Text = effect.Icon, Width = 24, Height = 24, FontSize = 22 };
                icon.SetResourceReference(TextBlock.FontFamilyProperty, "RemixIcon");
                if (iconIndex < icons.Children.Count) icons.Children.RemoveAt(iconIndex);
                icons.Children.Insert(iconIndex, icon);
            }
            var brush = (Brush)FindResource(AccentResource(effect));
            if (icon.Foreground != brush) icon.Foreground = brush;
            iconIndex++;
        }
        if (count > MaxVisibleEffects)
        {
            if (iconIndex == icons.Children.Count || ((TextBlock)icons.Children[iconIndex]).Tag != null)
            {
                var more = new TextBlock { Text = "\uEF79", Width = 24, Height = 24, FontSize = 22 };
                more.SetResourceReference(TextBlock.FontFamilyProperty, "RemixIcon");
                more.SetResourceReference(TextBlock.ForegroundProperty, "DARKPrimary");
                icons.Children.Insert(iconIndex, more);
            }
            iconIndex++;
        }
        while (icons.Children.Count > iconIndex) icons.Children.RemoveAt(icons.Children.Count - 1);
        icons.Columns = Math.Min(IconsPerRow, Math.Max(1, iconIndex));
        Entry.Visibility = count == 0 ? Visibility.Collapsed : Visibility.Visible;
        AutomationProperties.SetName(Entry, "增减益：{0}".Translate(count));
        if (count == 0) popup.IsOpen = false;
    }

    public void Dispose()
    {
        popup.Closed -= Popup_Closed;
        Entry.Loaded -= Entry_Loaded;
        Entry.Unloaded -= Entry_Unloaded;
        Entry_Unloaded(this, new RoutedEventArgs());
        mw.Main.UIGrid.Children.Remove(popup);
    }

    private string AccentResource(Buff.Effect effect)
    {
        bool active = mw.Buff.IsActivityActive(effect.Activity);
        if (effect.IsDebuff == true) return active ? "BuffDebuff" : "BuffInactiveDebuff";
        if (effect.IsDebuff == null) return active ? "PrimaryText" : "BuffInactiveSpecial";
        return active ? "DARKPrimary" : "BuffInactiveBenefit";
    }

    private void Refresh()
    {
        int rowIndex = 0, benefits = 0, debuffs = 0, special = 0;
        bool finite = false;
        var template = (DataTemplate)Resources["BuffRow"];
        for (int i = 0; i < mw.Buff.Effects.Count; i++)
        {
            var effect = mw.Buff.Effects[i];
            if (effect.IsExpired) continue;
            if (effect.IsDebuff == true) debuffs++;
            else if (effect.IsDebuff == null) special++;
            else benefits++;
            finite |= effect.EndTime != DateTime.MaxValue;

            // 用 Effect 引用复用实际条目，SourceKey 并不是唯一行标识。
            Expander? row = null;
            for (int j = rowIndex; j < Rows.Children.Count; j++)
            {
                var candidate = (Expander)Rows.Children[j];
                if (!ReferenceEquals(candidate.DataContext, effect)) continue;
                row = candidate;
                if (j != rowIndex)
                {
                    Rows.Children.RemoveAt(j);
                    Rows.Children.Insert(rowIndex, row);
                }
                break;
            }
            if (row == null)
            {
                row = (Expander)template.LoadContent();
                row.DataContext = effect;
                ((TextBlock)row.FindName("NameText")).Text = string.IsNullOrWhiteSpace(effect.Name)
                    ? (string.IsNullOrWhiteSpace(effect.Source) ? "未命名效果".Translate() : effect.Source) : effect.Name;
                ((TextBlock)row.FindName("SourceText")).Text = "来源：{0}".Translate(
                    string.IsNullOrWhiteSpace(effect.Source) ? "未知来源".Translate() : effect.Source);
                ((TextBlock)row.FindName("TypeText")).Text = (effect.IsDebuff == true ? "减益" : effect.IsDebuff == null ? "特殊" : "增益").Translate();
                ((TextBlock)row.FindName("ActivityText")).Text = "适用活动：{0}".Translate(ActivityName(effect.Activity));
                var description = (TextBlock)row.FindName("DescriptionText");
                description.Visibility = string.IsNullOrWhiteSpace(effect.Description) ? Visibility.Collapsed : Visibility.Visible;
                var summary = (TextBlock)row.FindName("SummaryText");
                if (string.IsNullOrWhiteSpace(effect.Description)) summary.Visibility = Visibility.Collapsed;
                else
                {
                    row.Expanded += (_, _) => summary.Visibility = Visibility.Collapsed;
                    row.Collapsed += (_, _) => summary.Visibility = Visibility.Visible;
                }

                var values = new StringBuilder();
                var activity = effect.Activity & PetActivity.All;
                bool item = activity == PetActivity.TakeItem;
                void AddValue(string name, double value, bool multiplier = true)
                {
                    if (value == (multiplier ? 1 : 0)) return;
                    if (values.Length != 0) values.AppendLine();
                    values.Append(name.Translate()).Append(' ').Append(multiplier ? "×" : value > 0 ? "+" : "").Append(value.ToString("G"));
                }
                AddValue(effect.IsWorkActivity ? "体力消耗倍率" : item ? "物品体力收益倍率" : "体力变化倍率", effect.Strength);
                AddValue(item ? "物品饱腹度收益倍率" : "饱腹度变化倍率", effect.Food);
                AddValue(item ? "物品口渴值收益倍率" : "口渴值变化倍率", effect.Drink);
                AddValue(item ? "物品心情收益倍率" : "心情变化倍率", effect.Feeling);
                AddValue(item ? "物品健康收益倍率" : "健康变化倍率", effect.Health);
                AddValue("金钱收益倍率", effect.Money);
                AddValue(item ? "物品经验收益倍率" : "经验变化倍率", effect.Experience);
                AddValue(item ? "物品好感度收益倍率" : "好感度变化倍率", effect.Likability);
                AddValue(activity == PetActivity.Work ? "工作金钱收益倍率" : activity == PetActivity.Study ? "学习经验收益倍率"
                    : activity == PetActivity.Play ? "娱乐经验收益倍率" : "工作／学习／娱乐收益倍率", effect.Reward);
                AddValue("体力／饱腹度／口渴值上限", effect.StrengthMax, false);
                AddValue("心情上限", effect.FeelingMax, false);
                var valueText = (TextBlock)row.FindName("ValuesText");
                valueText.Text = values.Length == 0 ? effect.EmptyString : values.ToString();
                valueText.Visibility = string.IsNullOrWhiteSpace(valueText.Text) ? Visibility.Collapsed : Visibility.Visible;
                Rows.Children.Insert(rowIndex, row);
            }
            string accent = AccentResource(effect);
            var accentBrush = (Brush)FindResource(accent);
            if (row.BorderBrush != accentBrush) row.BorderBrush = accentBrush;
            bool active = mw.Buff.IsActivityActive(effect.Activity);
            var background = (Brush)FindResource(active
                ? effect.IsDebuff == true ? "BuffDebuffBackground" : effect.IsDebuff == null ? "BuffSpecialBackground" : "DARKPrimaryTrans4"
                : effect.IsDebuff == true ? "BuffInactiveDebuffBackground" : effect.IsDebuff == null ? "BuffSpecialBackground" : "BuffInactiveBenefitBackground");
            if (row.Background != background) row.Background = background;
            ((TextBlock)row.FindName("InactiveText")).Visibility = active ? Visibility.Collapsed : Visibility.Visible;
            rowIndex++;
        }
        while (Rows.Children.Count > rowIndex) Rows.Children.RemoveAt(Rows.Children.Count - 1);
        AllTab.Content = "全部({0})".Translate(rowIndex);
        BenefitTab.Content = "增益({0})".Translate(benefits);
        DebuffTab.Content = "减益({0})".Translate(debuffs);
        SpecialTab.Content = "特殊({0})".Translate(special);
        CurrentActivityText.Text = "当前：{0}".Translate(ActivityName(mw.Activity));
        FilterRows();
        UpdateRemainingTime();
        if (finite) countdown.Start();
        else countdown.Stop();
    }

    private void UpdateRemainingTime()
    {
        var now = DateTime.Now;
        foreach (Expander row in Rows.Children)
        {
            var effect = (Buff.Effect)row.DataContext;
            if (effect.IsExpired)
            {
                RefreshEntry();
                if (popup.IsOpen) Refresh();
                return;
            }
            if (row.Visibility != Visibility.Visible) continue;
            string duration;
            if (effect.EndTime == DateTime.MaxValue) duration = "永久".Translate();
            else
            {
                var remaining = TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling((effect.EndTime - now).TotalSeconds)));
                duration = remaining.TotalDays >= 1 ? "{0}天{1}小时".Translate((int)remaining.TotalDays, remaining.Hours)
                    : remaining.TotalHours >= 1 ? "{0}小时{1}分".Translate((int)remaining.TotalHours, remaining.Minutes)
                    : remaining.TotalMinutes >= 1 ? "{0}分{1}秒".Translate((int)remaining.TotalMinutes, remaining.Seconds)
                    : "{0}秒".Translate(remaining.Seconds);
            }
            var text = (TextBlock)row.FindName("DurationText");
            if (text.Text != duration) text.Text = duration;
        }
    }

    private void FilterRows()
    {
        bool any = false;
        foreach (Expander row in Rows.Children)
        {
            var effect = (Buff.Effect)row.DataContext;
            int type = effect.IsDebuff == true ? 2 : effect.IsDebuff == null ? 3 : 1;
            bool visible = category == 0 || category == type;
            row.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            any |= visible;
        }
        EmptyText.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Category_Checked(object sender, RoutedEventArgs e)
    {
        category = int.Parse((string)((RadioButton)sender).Tag);
        if (!IsInitialized) return;
        FilterRows();
        UpdateRemainingTime();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => popup.IsOpen = false;

    // 活动名称在当前活动标题和条目适用范围中共用，不推断效果的正负方向。
    private static string ActivityName(PetActivity activity)
    {
        var scope = activity & PetActivity.All;
        var name = new StringBuilder();
        if (scope == PetActivity.None) name.Append("无活动".Translate());
        else if (scope == PetActivity.All) name.Append("所有活动".Translate());
        else
        {
            void Add(PetActivity flag, string text)
            {
                if ((scope & flag) == 0) return;
                if (name.Length != 0) name.Append('／');
                name.Append(text.Translate());
            }
            Add(PetActivity.Idle, "空闲");
            Add(PetActivity.Sleep, "睡觉");
            Add(PetActivity.Study, "学习");
            Add(PetActivity.Work, "工作");
            Add(PetActivity.Play, "娱乐");
            Add(PetActivity.Travel, "旅行");
            Add(PetActivity.Other, "其他");
            Add(PetActivity.TakeItem, "使用物品");
        }
        return (activity & PetActivity.Schedule) == 0 ? name.ToString()
            : scope is PetActivity.None or PetActivity.All ? "日程表开启期间".Translate() : "日程表开启期间的{0}".Translate(name);
    }
}
