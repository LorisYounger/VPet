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
using WpfAnimatedGif;

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
    private Photo nowphoto;
    public void DisplayDetail(Photo photo)
    {
        nowphoto = photo;

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
        if (photo.Path.ToLower().EndsWith(".gif"))
        {
            ImageBehavior.SetAnimatedSource(ImagePhotoDetail, photo.GetImage(mw));
        }
        else
            ImagePhotoDetail.Source = photo.GetImage(mw);
    }

    private void BorderOutDetail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        IsMaskVisible = false;
        IsOverlayerVisible = false;
    }

    private void ButtonClose_Click(object sender, RoutedEventArgs e)
    {
        IsMaskVisible = false;
        IsOverlayerVisible = false;
    }
    private void ButtonCopy_Click(object sender, RoutedEventArgs e)
    {
        if (nowphoto == null)
            return;
        Clipboard.SetImage(nowphoto.GetImage(mw));
        Toast(message: "已复制图片！".Translate(),
                     icon: MessageBoxIcon.Info);
    }

    private void ButtonSave_Click(object sender, RoutedEventArgs e)
    {
        if (nowphoto == null)
            return;
        SaveFileDialog dialog = new SaveFileDialog();
        string ext = nowphoto.Path.Split('.').Last();
        dialog.Filter = ext + "|*." + ext;
        if (dialog.ShowDialog() == true)
        {
            Task.Run(() =>
                {
                    nowphoto.SaveAs(mw, dialog.FileName);
                    Dispatcher.Invoke(() =>
                     Toast(
                        message: "已保存图片！".Translate(),
                        icon: MessageBoxIcon.Info
                    ));
                });
        }
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
        if (selectedPhotos.Count == 0)
        {
            Toast(
                message: "当前没有解锁任何图片！".Translate(),
                icon: MessageBoxIcon.Error
            );
            return;
        }
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            Task.Run(() =>
            {
                foreach (var photo in selectedPhotos)
                {
                    photo.SaveAs(mw, photo.FilePath(dialog.FolderName));
                }
                Dispatcher.Invoke(() => Toast(
                          message: "已导出全部解锁的图片！".Translate(),
                          icon: MessageBoxIcon.Info
                      ));
            });
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
                message: "当前没有选中任何图片！".Translate(),
                icon: MessageBoxIcon.Error
            );
            return;
        }
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            foreach (var photo in selectedPhotos)
            {
                photo.SaveAs(mw, photo.FilePath(dialog.FolderName));
            }
            Toast(
                message: "已导出选中的图片！".Translate(),
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
