using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.MutiPlatform.WinDesign.Gallery;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.MutiPlatform;
/// <summary>
/// winGallery.xaml 的交互逻辑
/// </summary>
/// 跨平台: 原文复制自 VPet-Simulator.Windows/WinDesign/winGallery.xaml.cs; WindowX→VPetWindow, Visibility→IsVisible,
/// 文件/文件夹对话框换成 StorageProvider (异步), GIF 动图由 Display/Shell/ImageBehavior 播 (对应 WpfAnimatedGif), 其余逐行相同
public partial class winGallery : VPetWindow
{
    private TextBox? _searchTextBox;
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

    private void Window_Closed(object? sender, WindowClosingEventArgs e)
    {
        mw.winGallery = null;
    }

    private void Button_Loaded(object? sender, RoutedEventArgs e)
    {
        ((Button)sender!).Content = "照片图库".Translate() + mw.PrefixSave;
    }

    private void BtnSearch_Click(object? sender, RoutedEventArgs e)
    {
        RefreshList();
    }

    private void TbTitleSearch_Loaded(object? sender, RoutedEventArgs e)
    {
        _searchTextBox = sender as TextBox;
        RefreshList();
    }

    private void ToggleButtonGroupTags_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshList();
    }

    private void ToggleButtonSearchParameters_CheckChanged(object? sender, RoutedEventArgs e)
    {
        RefreshList();

    }
    bool process = false;
    public void RefreshList()
    {
        if (!IsLoaded)
        {
            return;
        }
        if (process)
        {
            return;
        }
        process = true;

        AutoUniformGridImages.Children.Clear();

        var searchText = _searchTextBox?.Text;

        //如果某个分类一个都没选中，那就等于全部选中

        var isIllustrationChecked = ToggleButtonIllustration.IsChecked == true ? true : ToggleButtonThumbnail.IsChecked == false;
        var isThumbnailChecked = ToggleButtonThumbnail.IsChecked == true ? true : ToggleButtonIllustration.IsChecked == false;
        //var isGIFChecked = ToggleButtonGIF.IsChecked == true;

        var isLockedChecked = ToggleButtonLocked.IsChecked == true ? true : ToggleButtonUnlocked.IsChecked == false;
        var isUnlockedChecked = ToggleButtonUnlocked.IsChecked == true ? true : ToggleButtonLocked.IsChecked == false;

        var isFavoriteChecked = CheckBoxFavorite.IsChecked == true;
        var selectedTags = ToggleButtonGroupTags.SelectedItems.Cast<string>();

        var photos = new List<Photo>();

        //获取锁定的图片
        if (isLockedChecked)
        {
            var lockphoto = mw.Photos.FindAll(p =>
                p.IsUnlock == false
                && (!isFavoriteChecked || p.IsStar)
                && (isIllustrationChecked || p.Type != Photo.PhotoType.Illustration)
                && (isThumbnailChecked || p.Type != Photo.PhotoType.Thumbnail)
                && (!selectedTags.Any() || selectedTags.All(st => p.TagsTrans.Contains(st)))
                && (
                    string.IsNullOrWhiteSpace(searchText)
                    || p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || p.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || p.TranslateName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || p.Description.Translate().Contains(searchText, StringComparison.OrdinalIgnoreCase)
                )
            );
            photos.AddRange(lockphoto);
        }
        //获取解锁的图片
        if (isUnlockedChecked)
        {
            var unlockphoto = mw.Photos.FindAll(p =>
                p.IsUnlock == true
                && (!isFavoriteChecked || p.IsStar)
                && (isIllustrationChecked || p.Type != Photo.PhotoType.Illustration)
                && (isThumbnailChecked || p.Type != Photo.PhotoType.Thumbnail)
                && (!selectedTags.Any() || selectedTags.All(st => p.TagsTrans.Contains(st)))
                && (
                    string.IsNullOrWhiteSpace(searchText)
                    || p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || p.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || p.TranslateName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || p.Description.Translate().Contains(searchText, StringComparison.OrdinalIgnoreCase)
                )
            );
            photos.AddRange(unlockphoto);
        }
        switch (OrderBox.SelectedIndex)
        {
            default:
                break;
            case 1:
                photos = photos.OrderBy(p => p.TranslateName).ToList();
                break;
            case 2:
                photos = photos.OrderByDescending(p => p.PlayerInfo?.UnlockTime ?? DateTime.MinValue).ToList();
                break;
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

        BorderEmpty.IsVisible = AutoUniformGridImages.Children.Count == 0;
        process = false;
    }
    private Photo? nowphoto;
    public void DisplayDetail(Photo photo)
    {
        nowphoto = photo;
        LablePhotoLoading.IsVisible = true;
        TextBlockPhotoDetailTitle.Text = photo.TranslateName;
        TextBlockPhotoDetailDescription.Text = "解锁时间".Translate() + ": " + photo.PlayerInfo!.UnlockTime.ToString() + '\n' + photo.Description.Translate();
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
        Task.Run(() =>
        {
            var img = photo.GetGifImage(mw);
            Dispatcher.UIThread.Invoke(() =>
            {
                ImageBehavior.SetAnimatedSource(ImagePhotoDetail, img);
                LablePhotoLoading.IsVisible = false;
            });
        });
    }

    private void BorderOutDetail_MouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
    {
        IsMaskVisible = false;
        IsOverlayerVisible = false;
    }

    private void ButtonClose_Click(object? sender, RoutedEventArgs e)
    {
        IsMaskVisible = false;
        IsOverlayerVisible = false;
    }
    private void ButtonCopy_Click(object? sender, RoutedEventArgs e)
    {
        if (nowphoto == null)
            return;
        nowphoto.CopyImageToClipboard(mw);
        Toast(message: "已复制图片！".Translate(),
                     icon: MessageBoxIcon.Info);
    }

    private async void ButtonSave_Click(object? sender, RoutedEventArgs e)
    {
        if (nowphoto == null)
            return;
        string ext = nowphoto.Path.Split('.').Last();
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            SuggestedFileName = nowphoto.FilePath(),
            FileTypeChoices = new[] { new FilePickerFileType(ext) { Patterns = new[] { "*." + ext } } },
        });
        if (file?.TryGetLocalPath() is string path)
        {
            Task.Run(() =>
                {
                    nowphoto.SaveAs(mw, path);
                    Dispatcher.UIThread.Invoke(() =>
                     Toast(
                        message: "已保存图片！".Translate(),
                        icon: MessageBoxIcon.Info
                    ));
                });
        }
    }

    private async void ButtonExportAll_Click(object? sender, RoutedEventArgs e)
    {
        var selectedPhotos = mw.Photos.FindAll(p =>
                p.IsUnlock == true);
        if (selectedPhotos.Count == 0)
        {
            Toast(
                message: "当前没有解锁任何图片！".Translate(),
                icon: MessageBoxIcon.Error
            );
            return;
        }
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        if (folders.Count > 0 && folders[0].TryGetLocalPath() is string folderName)
        {
            Task.Run(() =>
            {
                foreach (var photo in selectedPhotos)
                {
                    photo.SaveAs(mw, photo.FilePath(folderName));
                }
                Dispatcher.UIThread.Invoke(() => Toast(
                          message: "已导出全部解锁的图片！".Translate(),
                          icon: MessageBoxIcon.Info
                      ));
            });
        }
    }

    private async void ButtonExportSele_Click(object? sender, RoutedEventArgs e)
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
        if (selectedPhotos.Count == 0)
        {
            Toast(
                message: "当前没有选中任何图片！".Translate(),
                icon: MessageBoxIcon.Error
            );
            return;
        }
        else if (selectedPhotos.Count == 1)
        {
            nowphoto = selectedPhotos[0];
            string ext = nowphoto.Path.Split('.').Last();
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                SuggestedFileName = nowphoto.FilePath(),
                FileTypeChoices = new[] { new FilePickerFileType(ext) { Patterns = new[] { "*." + ext } } },
            });
            if (file?.TryGetLocalPath() is string path)
            {
                Task.Run(() =>
                {
                    nowphoto.SaveAs(mw, path);
                    Dispatcher.UIThread.Invoke(() =>
                     Toast(
                        message: "已保存图片！".Translate(),
                        icon: MessageBoxIcon.Info
                    ));
                });
            }
        }
        else
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
            if (folders.Count > 0 && folders[0].TryGetLocalPath() is string folderName)
            {
                foreach (var photo in selectedPhotos)
                {
                    photo.SaveAs(mw, photo.FilePath(folderName));
                }
                Toast(
                    message: "已导出选中的图片！".Translate(),
                    icon: MessageBoxIcon.Info
                );
            }
        }
    }


    private void Pagination_CurrentPageChanged(object? sender, SelectedValueChangedRoutedEventArgs<int> e)
    {
        RefreshList();
    }

    private void AutoUniformGridImages_Changed(object? sender, RoutedEventArgs e)
    {
        var uniformGrid = e.Source as AutoUniformGrid;
        var columns = uniformGrid!.Columns;
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
    private void CheckBoxALL_Checked(object? sender, RoutedEventArgs e)
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
