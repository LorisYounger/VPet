using LinePutScript.Localization.WPF;
using Microsoft.Win32;
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
    private int _columns;
    private int _rows;
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

        //tag分类
        var tags = mw.Photos.SelectMany(p => p.TagsTrans).GroupBy(item => item) // 按照每个元素进行分组
            .Select(group => new { Item = group.Key, Count = group.Count() }) // 选择元素及其出现次数
            .OrderByDescending(x => x.Count) // 按出现次数降序排序
            .Select(x => x.Item); // 选择去重后的元素
        ToggleButtonGroupTags.ItemsSource = tags;
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

    private void ToggleButtonSearchParameters_CheckChanged(object sender, RoutedEventArgs e)
    {
        RefreshList();

    }

    public void RefreshList()
    {
        if (!IsLoaded)
        {
            return;
        }


        AutoUniformGridImages.Children.Clear();

        var searchText = _searchTextBox.Text;

        //如果某个分类一个都没选中，那就等于全部选中

        var isIllustrationChecked = ToggleButtonIllustration.IsChecked == true ? true : ToggleButtonThumbnail.IsChecked == false;
        var isThumbnailChecked = ToggleButtonThumbnail.IsChecked == true ? true : ToggleButtonThumbnail.IsChecked == false;
        //var isGIFChecked = ToggleButtonGIF.IsChecked == true;

        var isLockedChecked = ToggleButtonLocked.IsChecked == true ? true : ToggleButtonUnlocked.IsChecked == false;
        var isUnlockedChecked = ToggleButtonUnlocked.IsChecked == true ? true : ToggleButtonLocked.IsChecked == false;

        var isFavoriteChecked = CheckBoxFavorite.IsChecked == true;
        var selectedTags = ToggleButtonGroupTags.SelectedItems.Cast<string>();

        var photos = new List<Photo>();

        //获取锁定的图片
        if (isLockedChecked)
        {
            var lockphoto = mw.Photos.FindAll(p => p.IsUnlock == false
                    && (!isFavoriteChecked || p.IsStar)
                    && (isIllustrationChecked || p.Type != Photo.PhotoType.Illustration)
                    && (isThumbnailChecked || p.Type != Photo.PhotoType.Thumbnail)
                    && (!selectedTags.Any() || selectedTags.Any(st => p.Tags.Contains(st)))
                    && (string.IsNullOrWhiteSpace(searchText) || p.Name.Contains(searchText) || p.Description.Contains(searchText)));
            photos.AddRange(lockphoto);
        }
        //获取解锁的图片
        if (isUnlockedChecked)
        {
            var unlockphoto = mw.Photos.FindAll(p => p.IsUnlock == true
                && (!isFavoriteChecked || p.IsStar)
                && (isIllustrationChecked || p.Type != Photo.PhotoType.Illustration)
                && (isThumbnailChecked || p.Type != Photo.PhotoType.Thumbnail)
                && (!selectedTags.Any() || selectedTags.Any(st => p.Tags.Contains(st)))
                && (string.IsNullOrWhiteSpace(searchText) || p.Name.Contains(searchText) || p.Description.Contains(searchText)));

            photos.AddRange(unlockphoto);
        }

        var totalCount = photos.Count();
        var pageSize = _rows * _columns;
        pagination.MaxPage = (int)Math.Ceiling(totalCount * 1.0 / pageSize);
        var currentPage = Math.Max(0, Math.Min(pagination.MaxPage, pagination.CurrentPage) - 1);
        pagination.CurrentPage = currentPage + 1;

        photos = photos.Skip(currentPage * pageSize).Take(pageSize).ToList();

        foreach (var photo in photos)
        {
            if (photo.IsUnlock)
            {
                var newItem = new UnLockedGalleryItemUc(photo, mw);
                AutoUniformGridImages.Children.Add(newItem);
            }
            else
            {
                var newItem = new LockedGalleryItemUc(photo, mw);
                AutoUniformGridImages.Children.Add(newItem);
            }
        }

        BorderEmpty.Visibility = AutoUniformGridImages.Children.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    public void DisplayDetail(Photo photo)
    {
        ImagePhotoDetail.Source = photo.GetImage(mw);
        TextBlockPhotoDetailTitle.Text = photo.TranslateName;
        TextBlockPhotoDetailDescription.Text = photo.Description;
        IsMaskVisible = true;
        IsOverlayerVisible = true;
        if (photo.Type == Photo.PhotoType.Illustration)
        {
            DisplayGrid.Margin = new Thickness(50, 40, 50, 40);
        }
        else
        {
            DisplayGrid.Margin = new Thickness(150, 120, 150, 120);
        }
    }

    private void BorderOutDetail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        IsMaskVisible = false;
        IsOverlayerVisible = false;
    }

    private void ButtonSetDestop_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ButtonSave_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ButtonExportAll_Click(object sender, RoutedEventArgs e)
    {
        var selectedPhotos = new List<Photo>();
        foreach (var item in AutoUniformGridImages.Children)
        {
            if (item is UnLockedGalleryItemUc unlockedItem)
            {
                selectedPhotos.Add(unlockedItem.Photo);
            }
        }
        if (!selectedPhotos.Any())
        {
            Toast(
                message: "当前没有选中任何项目！".Translate(),
                icon: MessageBoxIcon.Error
            );
            return;
        }
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            //foreach (var photo in selectedPhotos)
            //{
            //    photo.SaveToFolder(dialog.SelectedPath);
            //}
            Toast(
                message: "已导出选中的项目！".Translate(),
                icon: MessageBoxIcon.Info
            );
        }
    }

    private void ButtonExportSele_Click(object sender, RoutedEventArgs e)
    {
        var selectedPhotos = new List<Photo>();
        foreach (var item in AutoUniformGridImages.Children)
        {
            if (item is UnLockedGalleryItemUc unlockedItem
                && unlockedItem.IsSelected)
            {
                selectedPhotos.Add(unlockedItem.Photo);
            }
        }
        if (!selectedPhotos.Any())
        {
            Toast(
                message: "当前没有选中任何项目！".Translate(),
                icon: MessageBoxIcon.Error
            );
            return;
        }
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            //foreach (var photo in selectedPhotos)
            //{
            //    photo.SaveToFolder(dialog.SelectedPath);
            //}
            Toast(
                message: "已导出选中的项目！".Translate(),
                icon: MessageBoxIcon.Info
            );
        }
    }


    private void Pagination_CurrentPageChanged(object sender, Panuon.WPF.SelectedValueChangedRoutedEventArgs<int> e)
    {
        RefreshList();
    }

    private void AutoUniformGridImages_Changed(object sender, RoutedEventArgs e)
    {
        var uniformGrid = e.OriginalSource as AutoUniformGrid;
        var columns = uniformGrid.Columns;
        var rows = uniformGrid.Rows;

        var isAnyChanged = false;
        if (columns != _columns)
        {
            _columns = columns;
            isAnyChanged = true;
        }
        if (rows != _rows)
        {
            _rows = rows;
            isAnyChanged = true;
        }
        if (isAnyChanged)
        {
            RefreshList();
        }
    }
    private void CheckBoxALL_Checked(object sender, RoutedEventArgs e)
    {
        bool isc = CheckBoxALL.IsChecked == true;
        foreach (var item in AutoUniformGridImages.Children)
        {
            if (item is UnLockedGalleryItemUc unlockedItem)
            {
                unlockedItem.IsSelected = isc;
            }

        }
    }
}
