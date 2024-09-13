using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VPet_Simulator.Windows.WinDesign.Gallery
{
    /// <summary>
    /// LockedGalleryItemUc.xaml 的交互逻辑
    /// </summary>
    public partial class LockedGalleryItemUc : UserControl
    {
        public LockedGalleryItemUc()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler Unlock
        {
            add { AddHandler(UnlockEvent, value); }
            remove { RemoveHandler(UnlockEvent, value); }
        }

        public static readonly RoutedEvent UnlockEvent =
            EventManager.RegisterRoutedEvent("Unlock", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(LockedGalleryItemUc));

        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(LockedGalleryItemUc));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(LockedGalleryItemUc));


        public bool Sellboth
        {
            get { return (bool)GetValue(SellbothProperty); }
            set { SetValue(SellbothProperty, value); }
        }

        public static readonly DependencyProperty SellbothProperty =
            DependencyProperty.Register("Sellboth", typeof(bool), typeof(LockedGalleryItemUc));

        public double UnlockMoney
        {
            get { return (double)GetValue(UnlockMoneyProperty); }
            set { SetValue(UnlockMoneyProperty, value); }
        }

        public static readonly DependencyProperty UnlockMoneyProperty =
            DependencyProperty.Register("UnlockMoney", typeof(double), typeof(LockedGalleryItemUc));

        public string UnlockAble
        {
            get { return (string)GetValue(UnlockAbleProperty); }
            set { SetValue(UnlockAbleProperty, value); }
        }

        public static readonly DependencyProperty UnlockAbleProperty =
            DependencyProperty.Register("UnlockAble", typeof(string), typeof(LockedGalleryItemUc));

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(UnlockEvent));
        }
    }
}
