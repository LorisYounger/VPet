using Panuon.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winBetterBuy.xaml 的交互逻辑
    /// </summary>
    public partial class winBetterBuy : WindowX
    {
        private TextBox _searchTextBox;

        public winBetterBuy(MainWindow mw)
        {
            InitializeComponent();

            IcCommodity.ItemsSource = new List<BetterBuyItem>()
            {
                new BetterBuyItem()
                {
                    Name = "商品A",
                    Description = "一件商品",
                    ImageShot = new BitmapImage(new Uri("/VPet-Simulator.Windows;component/Res/tony.bmp", UriKind.RelativeOrAbsolute)),
                },
                new BetterBuyItem()
                {
                    Name = "商品B",
                    Description = "一件商品",
                    ImageShot = new BitmapImage(new Uri("/VPet-Simulator.Windows;component/Res/tony.bmp", UriKind.RelativeOrAbsolute)),
                },
            };
        }

        private void RbtnIncrease_Click(object sender, RoutedEventArgs e)
        {
            var repeatButton = sender as RepeatButton;
            var item = repeatButton.DataContext as BetterBuyItem;
            item.Quantity = Math.Max(1, item.Quantity + 1);
        }

        private void RbtnDecrease_Click(object sender, RoutedEventArgs e)
        {
            var repeatButton = sender as RepeatButton;
            var item = repeatButton.DataContext as BetterBuyItem;
            item.Quantity = Math.Max(1, item.Quantity - 1);
        }

        private void BtnBuy_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void BtnTitle_Click(object sender, RoutedEventArgs e)
        {
            _searchTextBox.Text = "";
            Search();
        }

        private void Search()
        {
            var searchText = _searchTextBox.Text;
            var category = LsbCategory.SelectedIndex;
            var sortRule = LsbSortRule.SelectedIndex;
            var sortAsc = LsbSortAsc.SelectedIndex == 0;
            //搜索商品
        }

        private void TbTitleSearch_Loaded(object sender, RoutedEventArgs e)
        {
            _searchTextBox = sender as TextBox;
        }
    }

    public class BetterBuyItem
        : NotifyPropertyChangedBase
    {
        /// <summary>
        /// 物品图像
        /// </summary>
        public ImageSource ImageShot { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// 物品描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 物品分类
        /// </summary>
        public string[] Categories { get; set; }
        /// <summary>
        /// 物品价格
        /// </summary>
        public double Price { get; set; }
        /// <summary>
        /// 商品实际价格
        /// </summary>
        public double RealPrice { get; set; }
        /// <summary>
        /// 选择的物品个数
        /// </summary>
        public int Quantity { get => _quantity; set => Set(ref _quantity, value); }
        private int _quantity;
        /// <summary>
        /// 商品折扣 (100%)
        /// </summary>
        public int Discount { get; set; }
    }
}
