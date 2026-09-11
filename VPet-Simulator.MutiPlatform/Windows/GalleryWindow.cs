using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Unified.Services;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.MutiPlatform.Windows;

/// <summary>
/// 照片图库
/// </summary>
/// 对应 Windows 版的 winGallery。解锁条件的判定是共享源码
/// (Photo.UnlockCondition + GalleryUnlockRules), 两个平台判出来一样。
///
/// 没解锁的照片显示为一个灰格子加解锁条件 —— Windows 版是把图灰度化, 那要多解一次
/// 码再逐像素转换, 而"看不到内容"这件事用一个占位格子说得同样清楚。
internal sealed class GalleryWindow : VPetWindow
{
    private readonly PetWindow host;
    private readonly TextBox search = new TextBox
    {
        Watermark = LocalizeCore.Translate("搜索"),
        MinWidth = 200,
    };
    private readonly CheckBox onlyUnlocked = new CheckBox
    {
        Content = LocalizeCore.Translate("只看已解锁"),
    };
    private readonly TextBlock summary = new TextBlock();
    private readonly PagedWrapPanel<Photo> grid;

    internal GalleryWindow(PetWindow host)
    {
        this.host = host;
        Title = LocalizeCore.Translate("照片图库");
        CanResize = true;
        SizeToContent = SizeToContent.Manual;
        Width = 760;
        Height = 600;

        grid = new PagedWrapPanel<Photo>(BuildCell) { PageSize = 12 };
        Body = BuildRoot();
        Refresh();
    }

    private Control BuildRoot()
    {
        var root = new DockPanel();
        var top = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 8),
        };
        summary.VerticalAlignment = VerticalAlignment.Center;
        top.Children.Add(summary);
        top.Children.Add(search);
        onlyUnlocked.VerticalAlignment = VerticalAlignment.Center;
        top.Children.Add(onlyUnlocked);
        top.Children.Add(SecondaryButton(LocalizeCore.Translate("检查解锁"), CheckUnlock));
        search.TextChanged += (_, _) => Refresh();
        onlyUnlocked.IsCheckedChanged += (_, _) => Refresh();
        DockPanel.SetDock(top, Dock.Top);
        root.Children.Add(top);
        root.Children.Add(grid);
        return root;
    }

    private void Refresh()
    {
        var all = host.HostPhotos.Photos.AsEnumerable();
        if (onlyUnlocked.IsChecked == true)
            all = all.Where(x => x.IsUnlock);
        var keyword = search.Text;
        if (!string.IsNullOrWhiteSpace(keyword))
            all = all.Where(x => x.TranslateName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.TagsTrans.Any(t => t.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        // 已解锁的排前面: 玩家更常看已经拿到的那些
        var list = all.OrderByDescending(x => x.IsUnlock).ThenBy(x => x.TranslateName, StringComparer.CurrentCulture).ToList();
        grid.SetSource(list);

        int unlocked = host.HostPhotos.Photos.Count(x => x.IsUnlock);
        summary.Text = LocalizeCore.Translate("已解锁 {0} / {1}", unlocked, host.HostPhotos.Photos.Count);
    }

    private Control BuildCell(Photo photo)
    {
        var cell = new Border { Width = 170, Height = 210, Margin = new Thickness(4) };
        cell.Classes.Add("vpet-card");

        var panel = new DockPanel { LastChildFill = true };
        var top = new StackPanel { Spacing = 4 };
        DockPanel.SetDock(top, Dock.Top);
        panel.Children.Add(top);

        if (photo.IsUnlock)
        {
            var image = host.HostPhotos.GetImage(photo);
            if (image != null)
                top.Children.Add(new Image { Source = image, Height = 110, Stretch = Stretch.Uniform });
        }
        else
        {
            //没解锁就摆个占位格子, 别让玩家以为图坏了
            top.Children.Add(new Border
            {
                Height = 110,
                Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                Child = new TextBlock
                {
                    Text = LocalizeCore.Translate("未解锁"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });
        }

        top.Children.Add(new TextBlock
        {
            Text = photo.TranslateName,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        var detail = new TextBlock
        {
            Text = photo.IsUnlock
                ? string.Join(" ", photo.TagsTrans)
                : photo.UnlockAble.CheckReason(host.HostGameSave),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        };
        detail.Classes.Add("vpet-hint");
        top.Children.Add(detail);

        if (photo.IsUnlock)
        {
            var star = new CheckBox
            {
                Content = LocalizeCore.Translate("喜欢"),
                IsChecked = photo.IsStar,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            star.IsCheckedChanged += (_, _) => photo.IsStar = star.IsChecked == true;
            panel.Children.Add(star);
        }

        cell.Child = panel;
        return cell;
    }

    /// <summary>
    /// 把条件已经满足的照片解锁掉
    /// </summary>
    /// 判定与 Windows 版 MainWindow.CheckGalleryUnlock 是同一份 (GalleryUnlockRules)
    private async void CheckUnlock()
    {
        var unlocked = new List<Photo>();
        foreach (var photo in host.HostPhotos.Photos)
        {
            if (!GalleryUnlockRules.ShouldAutoUnlock(
                    photo.IsUnlock, photo.UnlockAble.SellBoth, photo.UnlockAble.Check(host.HostGameSave)))
                continue;
            host.HostPhotos.Unlock(host.HostGameSave.Data, photo);
            unlocked.Add(photo);
        }
        if (unlocked.Count == 0)
        {
            await DialogService.ShowAsync(
                LocalizeCore.Translate("暂时没有可以解锁的照片"), Title, this);
            return;
        }
        Refresh();
        await DialogService.ShowAsync(
            string.Join(", ", unlocked.Select(x => x.TranslateName)) + "\n"
            + LocalizeCore.Translate("以上照片已解锁"),
            LocalizeCore.Translate("新的照片已解锁"), this);
    }
}
