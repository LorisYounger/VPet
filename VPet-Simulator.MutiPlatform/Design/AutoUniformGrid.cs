using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using System;

namespace VPet_Simulator.MutiPlatform
{
    //跨平台: 对应 VPet-Simulator.Windows/Design/AutoUniformGrid.cs, 依赖属性改成 Avalonia 的 StyledProperty,
    //OnRenderSizeChanged 改成 SizeChanged 事件, 其余逐行相同
    public class AutoUniformGrid
   : UniformGrid
    {

        #region ItemsMinWidth
        public double ItemsMinWidth
        {
            get { return GetValue(ItemsMinWidthProperty); }
            set { SetValue(ItemsMinWidthProperty, value); }
        }

        public static readonly StyledProperty<double> ItemsMinWidthProperty =
            AvaloniaProperty.Register<AutoUniformGrid, double>("ItemsMinWidth", double.NaN);
        #endregion

        #region ItemsMinHeight
        public double ItemsMinHeight
        {
            get { return GetValue(ItemsMinHeightProperty); }
            set { SetValue(ItemsMinHeightProperty, value); }
        }

        public static readonly StyledProperty<double> ItemsMinHeightProperty =
            AvaloniaProperty.Register<AutoUniformGrid, double>("ItemsMinHeight", double.NaN);
        #endregion

        public event EventHandler<RoutedEventArgs> Changed
        {
            add { AddHandler(ChangedEvent, value); }
            remove { RemoveHandler(ChangedEvent, value); }
        }

        public static readonly RoutedEvent<RoutedEventArgs> ChangedEvent =
            RoutedEvent.Register<AutoUniformGrid, RoutedEventArgs>("Changed", RoutingStrategies.Bubble);

        public AutoUniformGrid()
        {
            SizeChanged += (_, e) => OnRenderSizeChanged(e.NewSize);
        }

        protected void OnRenderSizeChanged(Size newSize)
        {
            var isChanged = false;
            if (!double.IsNaN(ItemsMinWidth))
            {
                var columns = (int)Math.Floor(newSize.Width / ItemsMinWidth);
                if (Columns != columns)
                {
                    isChanged = true;
                }
                SetCurrentValue(ColumnsProperty, columns);
            }
            if (!double.IsNaN(ItemsMinHeight))
            {
                var rows = (int)Math.Floor(newSize.Height / ItemsMinHeight);
                if (Rows != rows)
                {
                    isChanged = true;
                }
                SetCurrentValue(RowsProperty, rows);
            }
            if (isChanged)
            {
                RaiseEvent(new RoutedEventArgs(ChangedEvent));
            }
        }
    }

}
