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

namespace VPet_Simulator.Windows.WinDesign.Gallery
{
    /// <summary>
    /// UnLockedGalleryItemUc.xaml 的交互逻辑
    /// </summary>
    public partial class UnLockedGalleryItemUc : UserControl
    {
        public UnLockedGalleryItemUc()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UnLockedGalleryItemUc));

        public event RoutedEventHandler StarChanged
        {
            add { AddHandler(StarChangedEvent, value); }
            remove { RemoveHandler(StarChangedEvent, value); }
        }

        public static readonly RoutedEvent StarChangedEvent =
            EventManager.RegisterRoutedEvent("StarChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UnLockedGalleryItemUc));

        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(UnLockedGalleryItemUc));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(UnLockedGalleryItemUc));

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(UnLockedGalleryItemUc));

        public bool IsStar
        {
            get { return (bool)GetValue(IsStarProperty); }
            set { SetValue(IsStarProperty, value); }
        }

        public static readonly DependencyProperty IsStarProperty =
            DependencyProperty.Register("IsStar", typeof(bool), typeof(UnLockedGalleryItemUc));

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(UnLockedGalleryItemUc));

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent));
        }

        private void ToggleButtonStar_CheckChanged(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(StarChangedEvent));
        }
    }
}
