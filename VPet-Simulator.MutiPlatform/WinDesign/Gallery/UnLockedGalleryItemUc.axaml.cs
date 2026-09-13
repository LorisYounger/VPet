using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using LinePutScript.Localization;
using System;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.MutiPlatform.WinDesign.Gallery
{
    /// <summary>
    /// UnLockedGalleryItemUc.xaml 的交互逻辑
    /// </summary>
    /// 跨平台: 原文复制自 Windows 版同名文件; DependencyProperty→StyledProperty, ToolTip→ToolTip.SetTip, ActualWidth→Bounds.Width
    public partial class UnLockedGalleryItemUc : UserControl
    {
        public Photo Photo;
        MainWindow mw;
        public UnLockedGalleryItemUc(Photo photo, MainWindow mw)
        {
            InitializeComponent();
            Photo = photo;
            this.mw = mw;

            tbTitle.Text = photo.TranslateName;
            ToolTip.SetTip(cbDesc, photo.TranslateName);
            ToolTip.SetTip(this, photo.Description.Translate());
            tbStar.IsChecked = photo.IsStar;
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly StyledProperty<bool> IsSelectedProperty =
            AvaloniaProperty.Register<UnLockedGalleryItemUc, bool>("IsSelected");

        private void Border_MouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            mw.winGallery?.DisplayDetail(Photo);
        }

        private void ToggleButtonStar_CheckChanged(object? sender, RoutedEventArgs e)
        {
            Photo.IsStar = tbStar.IsChecked == true;
        }

        private void this_Loaded(object? sender, RoutedEventArgs e)
        {
            displayimage.Source =
                Photo.ConvertToThumbnail(Photo.GetImage(mw),
               (int)(bbd.Bounds.Width * 2), (int)(bbd.Bounds.Height * 2));
        }
    }
}
