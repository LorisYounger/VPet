using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Threading;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winInventory.xaml 的交互逻辑
    /// </summary>
    public partial class winInventory : WindowX
    {
        MainWindow mw;
        private TextBox _searchTextBox;
        private Run rTotalValue;
        private List<Item> _testItems;
        private Item _detailItem;
        private int _detailCount = 1;

        public winInventory(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            Loaded += winInventory_Loaded;

            // 临时测试物品（存档还没有物品时使用）
            _testItems = BuildTestItems();
        }

        public void ShowWindow()
        {
            Show();
            // 首次打开时控件可能还未加载完，延迟刷新避免“打开不加载、切换后才加载”
            Dispatcher.BeginInvoke(new Action(UpdateList), DispatcherPriority.Loaded);
        }

        private void winInventory_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        private List<Item> GetAllItems()
        {
            // TODO: 存档有物品后，取消下面注释，改为读取 mw.Items
            // return mw?.Items?.ToList() ?? new List<Item>();
            return _testItems ?? new List<Item>();
        }

        private List<Item> BuildTestItems()
        {
            var items = new List<Item>();
            // 食物：用于测试“更好买同款分类/收藏”
            for (int i = 0; i < 2; i++)
            {
                items.Add(new Food() { Name = "TestMeal" + i, Type = Food.FoodType.Meal, Price = 15 + i, Count = 3 + i, Desc = "测试正餐", StrengthFood = 20, Likability = 1 });
                items.Add(new Food() { Name = "TestSnack" + i, Type = Food.FoodType.Snack, Price = 8 + i, Count = 5 + i, Desc = "测试零食", StrengthFood = 10, Feeling = 1 });
                items.Add(new Food() { Name = "TestDrink" + i, Type = Food.FoodType.Drink, Price = 6 + i, Count = 4 + i, Desc = "测试饮料", StrengthDrink = 15 });
                items.Add(new Food() { Name = "TestFunc" + i, Type = Food.FoodType.Functional, Price = 30 + i, Count = 1 + i, Desc = "测试功能性", Strength = 5, Exp = 2 });
                items.Add(new Food() { Name = "TestDrug" + i, Type = Food.FoodType.Drug, Price = 40 + i, Count = 2, Desc = "测试药品", Health = 5 });
                items.Add(new Food() { Name = "TestGift" + i, Type = Food.FoodType.Gift, Price = 100 + i * 10, Count = 1, Desc = "测试礼品", Likability = 5, Feeling = 3 });
            }

            // 其他物品：只会出现在“全部”里（因为分类与更好买保持一致）
            for (int i = 0; i < 6; i++)
            {
                items.Add(new Item()
                {
                    Name = "TestTool" + i,
                    Price = 30 + i,
                    Count = 2 + (i % 3),
                    ItemType = "Tool",
                    Desc = "这是一些测试道具"
                });
            }
            for (int i = 0; i < 8; i++)
            {
                items.Add(new Item()
                {
                    Name = "TestItem" + i,
                    Price = 20 + i,
                    Count = 5,
                    ItemType = "Item",
                    Desc = "这是一些测试物品"
                });
            }
            return items;
        }

        private void UpdateList()
        {
            if (mw == null) return;

            // 全部物品（用于顶部“总价值”统计）
            var allItems = GetAllItems();

            // 搜索
            if (_searchTextBox != null && !string.IsNullOrWhiteSpace(_searchTextBox.Text))
            {
                allItems = allItems.Where(x => x.TranslateName.Contains(_searchTextBox.Text)).ToList();
            }

            // 分类（与更好买保持一致）
            // 0: 全部（显示所有物品）
            // 1: 收藏（只显示收藏食物）
            // 2..7: 正餐/零食/饮料/功能性/药品/礼品（只显示对应类型的食物）
            List<Item> items;
            if (LsbCategory.SelectedIndex == 0)
            {
                items = allItems.ToList();
            }
            else if (LsbCategory.SelectedIndex == (int)Food.FoodType.Star)
            {
                items = allItems.OfType<Food>().Where(x => x.Star).Cast<Item>().ToList();
            }
            else
            {
                var type = (Food.FoodType)LsbCategory.SelectedIndex;
                items = allItems.OfType<Food>().Where(x => x.Type == type).Cast<Item>().ToList();
            }

            // 排序
            bool asc = LsbSortAsc.SelectedIndex == 0;
            switch (LsbSortRule.SelectedIndex)
            {
                case 0: // 按数量
                    items = asc ? items.OrderBy(x => x.Count).ToList() : items.OrderByDescending(x => x.Count).ToList();
                    break;
                case 1: // 按价格
                    items = asc ? items.OrderBy(x => x.Price).ToList() : items.OrderByDescending(x => x.Price).ToList();
                    break;
            }

            IcCommodity.ItemsSource = items;

            // 计算总价值（始终统计全部物品，不受分类影响）
            double totalValue = GetAllItems().Sum(x => x.Price * x.Count);
            if (rTotalValue != null)
                rTotalValue.Text = totalValue.ToString("f2");
            
            if (items.Count == 0)
                TbNone.Visibility = Visibility.Visible;
            else
                TbNone.Visibility = Visibility.Collapsed;
        }

        private void UseItem(Item item, int count)
        {
            if (item == null) return;
            if (count <= 0) count = 1;
            count = Math.Min(Math.Max(1, item.Count), count);

            // 使用物品逻辑
            // 假设现在只是减少数量，实际效果逻辑取决于插件
            // TODO: 如果有插件处理程序，则调用它

            item.Count -= count;
            if (item.Count <= 0)
            {
                // TODO: 存档有物品后改为 mw.Items.Remove(item);
                _testItems?.Remove(item);
            }
            // Count 没有通知，直接刷新
            UpdateList();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        private void TbTitleSearch_Loaded(object sender, RoutedEventArgs e)
        {
            _searchTextBox = sender as TextBox;
        }

        private void rTotalValue_Loaded(object sender, RoutedEventArgs e)
        {
            rTotalValue = sender as Run;
            var totalValue = GetAllItems().Sum(x => x.Price * x.Count);
            rTotalValue.Text = totalValue.ToString("f2");
        }


        private void LsbSortRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateList();
        }

        private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IcCommodity.ItemsSource = null;
            HideDetail();
            Hide();
            e.Cancel = true;
        }

        private void BtnHoverUse_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var btn = sender as Button;
            var item = btn?.DataContext as Item;
            if (item == null)
                return;
            DisplayDetail(item);
        }

        private void CellRoot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var bdr = sender as Border;
            var item = bdr?.DataContext as Item;
            if (item == null)
                return;
            DisplayDetail(item);
        }

        private void DisplayDetail(Item item)
        {
            _detailItem = item;
            _detailCount = 1;

            TextItemName.Text = item.TranslateName;
            ImageItemDetail.Source = item.ImageSource;
            TextItemPrice.Text = $"$ {item.Price:f2}";

            if (item is Food food)
            {
                PanelPrefer.Visibility = Visibility.Visible;

                var percent = "100%";
                if (!string.IsNullOrWhiteSpace(food.Data))
                {
                    var idx = food.Data.LastIndexOf('\t');
                    if (idx >= 0 && idx + 1 < food.Data.Length)
                        percent = food.Data[(idx + 1)..].Trim();
                }
                TextItemPreferPercent.Text = percent;
                TextItemDesc.Text = food.Description;
            }
            else
            {
                PanelPrefer.Visibility = Visibility.Collapsed;
                TextItemPreferPercent.Text = "";
                TextItemDesc.Text = item.Desc ?? "";
            }

            TbDetailCount.Text = _detailCount.ToString();
            IsMaskVisible = true;
            IsOverlayerVisible = true;
        }

        private void HideDetail()
        {
            IsMaskVisible = false;
            IsOverlayerVisible = false;
        }

        private void BorderOutDetail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HideDetail();
        }

        private void ButtonCloseDetail_Click(object sender, RoutedEventArgs e)
        {
            HideDetail();
        }

        private void RbtnDetailDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (_detailItem == null)
                return;
            _detailCount = Math.Max(1, _detailCount - 1);
            TbDetailCount.Text = _detailCount.ToString();
        }

        private void RbtnDetailIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (_detailItem == null)
                return;
            _detailCount = Math.Min(Math.Max(1, _detailItem.Count), _detailCount + 1);
            TbDetailCount.Text = _detailCount.ToString();
        }

        private void TbDetailCount_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyDetailCountFromText();
                e.Handled = true;
            }
        }

        private void TbDetailCount_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyDetailCountFromText();
        }

        private void ApplyDetailCountFromText()
        {
            if (_detailItem == null)
                return;
            if (!int.TryParse(TbDetailCount.Text?.Trim(), out var v))
                v = _detailCount;
            v = Math.Max(1, v);
            v = Math.Min(Math.Max(1, _detailItem.Count), v);
            _detailCount = v;
            TbDetailCount.Text = _detailCount.ToString();
        }

        private void BtnDetailUse_Click(object sender, RoutedEventArgs e)
        {
            if (_detailItem == null)
                return;
            ApplyDetailCountFromText();
            UseItem(_detailItem, _detailCount);
            HideDetail();
        }
    }
}
