using Avalonia.Controls;
using Avalonia.Interactivity;
using LinePutScript.Localization;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.MutiPlatform.WinDesign.Gallery
{
    /// <summary>
    /// LockedGalleryItemUc.xaml 的交互逻辑
    /// </summary>
    /// 跨平台: 原文复制自 Windows 版同名文件; Visibility→IsVisible, ToolTip→ToolTip.SetTip, ActualWidth→Bounds.Width
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

            string untxt = photo.UnlockAble.CheckReason(mw.GameSavesData);
            unlocktext.Text = untxt;


            if (Photo.UnlockAble.SellPrice > 0)
            {
                if (Photo.UnlockAble.SellBoth)
                {
                    if (Photo.UnlockAble.Check(mw.GameSavesData))
                    {
                        btnCan.IsVisible = true;
                        rAnd.Text = "并".Translate();
                        clmoney.Text = convertk(photo.UnlockAble.SellPrice);
                    }
                    else
                    {
                        btnCannot.IsVisible = true;
                        nlmoney.Text = convertk(photo.UnlockAble.SellPrice);
                    }
                    ToolTip.SetTip(this, "花费${0} 并 满足以下条件:".Translate(photo.UnlockAble.SellPrice) + '\n' + untxt);
                }
                else
                {
                    btnCan.IsVisible = true;
                    clmoney.Text = convertk(photo.UnlockAble.SellPrice);
                    ToolTip.SetTip(this, "花费${0} 或 满足以下条件:".Translate(photo.UnlockAble.SellPrice) + '\n' + untxt);
                    rAnd.Text = "或".Translate();
                }
            }
            else
                ToolTip.SetTip(this, untxt);

        }
        private string convertk(double price)
        {
            if (price < 100000)
                return price.ToString("N0");
            return (price / 1000).ToString("N0") + "k";
        }
        private void Button_Click(object? sender, RoutedEventArgs e)
        {//花钱解锁
            if (mw.GameSavesData.GameSave.Money < Photo.UnlockAble.SellPrice)
            {
                mw.winGallery!.Toast("金钱不足".Translate() + " " + convertk(Photo.UnlockAble.SellPrice),
                    icon: MessageBoxIcon.Warning);
                return;
            }

            mw.GameSavesData.GameSave.Money -= Photo.UnlockAble.SellPrice;
            Photo.Unlock(mw);
            var i = mw.winGallery!.AutoUniformGridImages.Children.IndexOf(this);
            mw.winGallery.AutoUniformGridImages.Children.Remove(this);
            mw.winGallery.AutoUniformGridImages.Children.Insert(i, new UnLockedGalleryItemUc(Photo, mw));
            mw.winGallery.Toast(
            message: "已解锁".Translate() + " " + Photo.TranslateName,
            icon: MessageBoxIcon.Info
            );
        }

        private void this_Loaded(object? sender, RoutedEventArgs e)
        {
            displayimage.Source = Photo.ConvertToGrayScale(
                Photo.ConvertToThumbnail(Photo.GetImage(mw),
               (int)(bbd.Bounds.Width * 2), (int)(bbd.Bounds.Height * 2)));
        }
    }
}
