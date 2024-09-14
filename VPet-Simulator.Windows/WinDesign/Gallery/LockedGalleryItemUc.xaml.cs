using LinePutScript.Localization.WPF;
using NAudio.Gui;
using Panuon.WPF.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows.WinDesign.Gallery
{
    /// <summary>
    /// LockedGalleryItemUc.xaml 的交互逻辑
    /// </summary>
    public partial class LockedGalleryItemUc : UserControl
    {
        Photo Photo;
        MainWindow mw;
        public LockedGalleryItemUc(Photo photo, MainWindow mw)
        {
            InitializeComponent();
            Photo = photo;
            this.mw = mw;
            tbTitle.Text = photo.TranslateName;
            UnlockMoney = photo.UnlockAble.SellPrice;

            ToolTip = unlocktext.Text = photo.UnlockAble.CheckReason(mw.GameSavesData);

            if (Photo.UnlockAble.SellPrice > 1)
            {
                if (Photo.UnlockAble.SellBoth)
                {
                    if (Photo.UnlockAble.Check(mw.GameSavesData))
                    {
                        btnCan.Visibility = Visibility.Visible;
                    }
                    else
                        btnCannot.Visibility = Visibility.Visible;
                }
                else
                {
                    btnCan.Visibility = Visibility.Visible;
                }
            }

        }

        public double UnlockMoney { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {//花钱解锁
            mw.GameSavesData.GameSave.Money -= Photo.UnlockAble.SellPrice;
            Photo.Unlock(mw);
            if (mw.winGallery != null)
            {
                var i = mw.winGallery.AutoUniformGridImages.Children.IndexOf(this);
                mw.winGallery.AutoUniformGridImages.Children.Remove(this);
                mw.winGallery.AutoUniformGridImages.Children.Insert(i, new UnLockedGalleryItemUc(Photo, mw));
            }

            NoticeBox.Show(string.Concat(Photo.TranslateName, "\n", "以上照片已解锁".Translate()), "新的照片已解锁".Translate());
        }

        private void this_Loaded(object sender, RoutedEventArgs e)
        {
            displayimage.Source = Photo.ConvertToGrayScale(
                Photo.ConvertToThumbnail(Photo.GetImage(mw),
               (int)(bbd.ActualWidth * 2), (int)(bbd.ActualHeight * 2)));
        }
    }
}
