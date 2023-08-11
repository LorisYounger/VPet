using Panuon.WPF.UI;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace VPet_Simulator.Windows
{
    public class AutoUniformGrid
   : UniformGrid
    {

        #region ItemsMinWidth
        public double ItemsMinWidth
        {
            get { return (double)GetValue(ItemsMinWidthProperty); }
            set { SetValue(ItemsMinWidthProperty, value); }
        }

        public static readonly DependencyProperty ItemsMinWidthProperty =
            DependencyProperty.Register("ItemsMinWidth", typeof(double), typeof(AutoUniformGrid), new PropertyMetadata(double.NaN));
        #endregion

        #region ItemsMinHeight
        public double ItemsMinHeight
        {
            get { return (double)GetValue(ItemsMinHeightProperty); }
            set { SetValue(ItemsMinHeightProperty, value); }
        }

        public static readonly DependencyProperty ItemsMinHeightProperty =
            DependencyProperty.Register("ItemsMinHeight", typeof(double), typeof(AutoUniformGrid), new PropertyMetadata(double.NaN));
        #endregion

        public event RoutedEventHandler Changed
        {
            add { AddHandler(ChangedEvent, value); }
            remove { RemoveHandler(ChangedEvent, value); }
        }

        public static readonly RoutedEvent ChangedEvent =
            EventManager.RegisterRoutedEvent("Changed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AutoUniformGrid));


        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            var isChanged = false;
            if (!double.IsNaN(ItemsMinWidth))
            {
                var columns = (int)Math.Floor(sizeInfo.NewSize.Width / ItemsMinWidth);
                if(Columns != columns)
                {
                    isChanged = true;
                }
                SetCurrentValue(ColumnsProperty, columns);
            }
            if (!double.IsNaN(ItemsMinHeight))
            {
                var rows = (int)Math.Floor(sizeInfo.NewSize.Height / ItemsMinHeight);
                if(Rows != rows)
                {
                    isChanged = true;
                }
                SetCurrentValue(RowsProperty, rows);
            }
            if(isChanged)
            {
                RaiseEvent(new RoutedEventArgs(ChangedEvent));
            }
        }
    }

}
