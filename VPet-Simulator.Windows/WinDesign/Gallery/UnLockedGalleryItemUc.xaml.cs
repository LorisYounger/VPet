using LinePutScript.Localization.WPF;
using NAudio.Gui;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows.WinDesign.Gallery
{
    /// <summary>
    /// UnLockedGalleryItemUc.xaml 的交互逻辑
    /// </summary>
    public partial class UnLockedGalleryItemUc : UserControl
    {
        Photo Photo;
        MainWindow mw;
        public UnLockedGalleryItemUc(Photo photo, MainWindow mw)
        {
            InitializeComponent();
            Photo = photo;
            this.mw = mw;

            cbDesc.ToolTip = tbTitle.Text = photo.TranslateName;
            ToolTip = photo.Description.Translate();
            tbStar.IsChecked = photo.IsStar;
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(UnLockedGalleryItemUc));

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mw.winGallery?.DisplayDetail(Photo);
        }

        private void ToggleButtonStar_CheckChanged(object sender, RoutedEventArgs e)
        {
            Photo.IsStar = tbStar.IsChecked == true;
        }

        private void this_Loaded(object sender, RoutedEventArgs e)
        {
            displayimage.Source =
                Photo.ConvertToThumbnail(Photo.GetImage(mw),
               (int)(bbd.ActualWidth * 2), (int)(bbd.ActualHeight * 2));
        }
    }
}
