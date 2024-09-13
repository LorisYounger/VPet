using LinePutScript.Localization.WPF;
using NAudio.Gui;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VPet_Simulator.Windows.Interface;
using VPet_Simulator.Windows.WinDesign.Gallery;

namespace VPet_Simulator.Windows;
/// <summary>
/// winGallery.xaml 的交互逻辑
/// </summary>
public partial class winGallery : WindowX
{
    private TextBox _searchTextBox;
    MainWindow mw;

    public winGallery(MainWindow mw)
    {
        InitializeComponent();
        this.mw = mw;

        //每次打开的时候都检查下是否解锁, 并自动解锁
        //这个解锁条件可以塞到 保存前的检查里面
        mw.CheckGalleryUnlock();

        //Note:这个是给不二一的示例: 不用可以删了

        //补充策划案未写出的内容:

        //玩家可以查看解锁和未解锁的图片

        //图片获取: item.GetImage(mw) (全分辨率)
        //图片获取: Photo.ConvertToThumbnail(item.GetImage(mw),宽,高) (缩略图,宽高以最小比例为准)
        //转换成黑白(用于未解锁): Photo.ConvertToBlackWhite(item.GetImage(mw)) Photo.ConvertToGrayScale

        //图片有标题和描述, 描述可以只在解锁后显示, 解锁前可以显示解锁条件
        //解锁条件 item.UnlockAble.CheckReason(mw)

        //未解锁的图片有部分支持花钱解锁,可能需要做上相关功能
        //解锁的图片支持多选后导出

        //逻辑啥的可以空出来我写

        //TODO：这里TagsTrans还是英文代码
        var tags = mw.Photos.SelectMany(p => p.TagsTrans).Distinct();
        foreach (var tag in tags)
        {
            ToggleButtonGroupTags.ItemsSource = tags;
        }


    }

    private void Window_Closed(object sender, EventArgs e)
    {
        mw.winGallery = null;
    }

    private void Button_Loaded(object sender, RoutedEventArgs e)
    {
        ((Button)sender).Content = "照片图库".Translate() + mw.PrefixSave;
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        RefreshList();
    }

    private void TbTitleSearch_Loaded(object sender, RoutedEventArgs e)
    {
        _searchTextBox = sender as TextBox;
        RefreshList();
    }

    private void ToggleButtonGroupTags_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshList();
    }

    private void ToggleButtonLockedOrUnlocked_CheckChanged(object sender, RoutedEventArgs e)
    {
        RefreshList();
    }

    private void CheckBoxFavorite_CheckChanged(object sender, RoutedEventArgs e)
    {
        RefreshList();
    }

    private void RefreshList()
    {
        if (!IsLoaded)
        {
            return;
        }

        AutoUniformGridImages.Children.Clear();

        var searchText = _searchTextBox.Text;
        var isUnlockedChecked = ToggleButtonUnlocked.IsChecked == true;
        var isLockedChecked = ToggleButtonLocked.IsChecked == true;
        var isFavoriteChecked = CheckBoxFavorite.IsChecked == true;

        //获取锁定的图片
        var lockphoto = mw.Photos.FindAll(x => x.IsUnlock == false 
            && (!isFavoriteChecked || x.IsStar)
            && (string.IsNullOrWhiteSpace(searchText) || x.Name.Contains(searchText) || x.Description.Contains(searchText)));
        foreach (var photo in lockphoto)
        {
            var newItem = new LockedGalleryItemUc()
            {
                Height = 160,
                Width = 185,
                Margin = new Thickness(0, 0, 10, 10),
                Title = photo.TranslateName,
                UnlockAble = photo.UnlockAble.LockString,
                UnlockMoney = 3.55, //如果不可用金钱解锁，传null
                Image = photo.GetImage(mw) //Photo.ConvertToBlackWhite()方法不存在
            };
            //newItem.Unlock += ... 点击解锁按钮时触发该事件
            AutoUniformGridImages.Children.Add(newItem);
        }
        //获取解锁的图片
        var unlockphoto = mw.Photos.FindAll(x => x.IsUnlock == true
            && (!isFavoriteChecked || x.IsStar)
            && (string.IsNullOrWhiteSpace(searchText) || x.Name.Contains(searchText) || x.Description.Contains(searchText)));
        foreach (var photo in unlockphoto)
        {
            var newItem = new UnLockedGalleryItemUc()
            {
                Height = 160,
                Width = 185,
                Margin = new Thickness(0, 0, 10, 10),
                Title = photo.TranslateName,
                Description = photo.Description,
                Image = photo.GetImage(mw)
            };
            AutoUniformGridImages.Children.Add(newItem);
        }
    }

}
