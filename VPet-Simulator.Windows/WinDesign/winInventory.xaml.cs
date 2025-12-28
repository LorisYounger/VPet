using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
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
        private Item _detailItem;
        private int _detailCount = 1;

        /// <summary>
        /// 构造函数，初始化物品栏窗口
        /// </summary>
        /// <param name="mw">主窗口实例</param>
        public winInventory(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            Loaded += winInventory_Loaded;
            mw.Items.Add(mw.Foods[0].Clone());
        }

        /// <summary>
        /// 显示物品栏窗口
        /// </summary>
        public void ShowWindow()
        {
            Show();
            // 首次打开时控件可能还未加载完，延迟刷新避免"打开不加载、切换后才加载"
            Dispatcher.BeginInvoke(new Action(UpdateList), DispatcherPriority.Loaded);
            foreach (string itemType in Item.ItemTypes)
            {
                LsbCategory.Items.Add(new ListBoxItem() { Content = ("Item_" + itemType).Translate() });
            }
        }

        private void winInventory_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        /// <summary>
        /// 更新物品列表
        /// </summary>
        /// <remarks>
        /// 根据搜索条件、分类、排序规则更新显示的物品列表，并计算总价值
        /// </remarks>
        private void UpdateList()
        {
            if (mw == null) return;

            // 全部物品（用于顶部"总价值"统计）
            IEnumerable<Item> items = mw.Items.Where(x => x.Visibility);

            // 搜索
            if (_searchTextBox != null && !string.IsNullOrWhiteSpace(_searchTextBox.Text))
            {
                items = items.Where(x => x.TranslateName.Contains(_searchTextBox.Text));
            }

            //收藏
            if (_puswitch?.IsChecked == true)
            {
                items = items.Where(x => x.Star == true);
            }

            // 分类
            // 0: 全部（显示所有物品）
            if (LsbCategory.SelectedIndex != 0)
            {
                items = items.Where(x => x.ItemType == Item.ItemTypes[LsbCategory.SelectedIndex]);
            }

            // 排序
            bool asc = LsbSortAsc.SelectedIndex == 0;
            switch (LsbSortRule.SelectedIndex)
            {
                default:
                    break;
                case 1: // 按名称
                    items = asc ? items.OrderBy(x => x.Name) : items.OrderByDescending(x => x.Name);
                    break;
                case 2: // 按数量
                    items = asc ? items.OrderBy(x => x.Count) : items.OrderByDescending(x => x.Count);
                    break;
                case 3: // 按价格
                    items = asc ? items.OrderBy(x => x.Price) : items.OrderByDescending(x => x.Price);
                    break;
            }

            IcCommodity.ItemsSource = items;

            // 计算总价值（始终统计全部物品，不受分类影响）
            double totalValue = mw.Items.Sum(x => x.Price * x.Count);
            if (rTotalValue != null)
                rTotalValue.Text = totalValue.ToString("f2");

            if (!items.Any())
                TbNone.Visibility = Visibility.Visible;
            else
                TbNone.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 使用物品
        /// </summary>
        private void UseItem(Item item, int count)
        {
            if (item == null) return;
            while (count-- > 0)
                item.Use();
            // 没有通知，直接刷新
            UpdateList();
        }

        /// <summary>
        /// 搜索按钮点击事件处理
        /// </summary>
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        /// <summary>
        /// 标题搜索框加载完成事件处理
        /// </summary>
        private void TbTitleSearch_Loaded(object sender, RoutedEventArgs e)
        {
            _searchTextBox = sender as TextBox;
        }

        /// <summary>
        /// 总价值文本加载完成事件处理
        /// </summary>
        private void rTotalValue_Loaded(object sender, RoutedEventArgs e)
        {
            rTotalValue = sender as Run;
            var totalValue = mw.Items.Sum(x => x.Price * x.Count);
            rTotalValue.Text = totalValue.ToString("f2");
        }


        /// <summary>
        /// 排序规则选择改变事件处理
        /// </summary>
        private void LsbSortRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateList();
        }

        ///// <summary>
        ///// 窗口关闭事件处理
        ///// </summary>
        //private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        //{
        //    IcCommodity.ItemsSource = null;
        //    HideDetail();
        //    Hide();
        //    e.Cancel = true;
        //}

        /// <summary>
        /// 鼠标悬停时使用按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void BtnHoverUse_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var btn = sender as Button;
            var item = btn?.DataContext as Item;
            if (item == null)
                return;
            DisplayDetail(item);
        }

        /// <summary>
        /// 物品格子单击事件处理
        /// </summary>
        private void CellRoot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var bdr = sender as Border;
            var item = bdr?.DataContext as Item;
            if (item == null)
                return;
            DisplayDetail(item);
        }

        /// <summary>
        /// 显示物品详情界面
        /// </summary>
        /// <param name="item">要显示的物品</param>
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

        /// <summary>
        /// 隐藏物品详情界面
        /// </summary>
        private void HideDetail()
        {
            IsMaskVisible = false;
            IsOverlayerVisible = false;
        }

        /// <summary>
        /// 详情界面外部区域点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// 点击详情界面外部区域时关闭详情界面
        /// </remarks>
        private void BorderOutDetail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HideDetail();
        }

        /// <summary>
        /// 关闭详情按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void ButtonCloseDetail_Click(object sender, RoutedEventArgs e)
        {
            HideDetail();
        }

        /// <summary>
        /// 详情界面减少数量按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void RbtnDetailDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (_detailItem == null)
                return;
            _detailCount = Math.Max(1, _detailCount - 1);
            TbDetailCount.Text = _detailCount.ToString();
        }

        /// <summary>
        /// 详情界面增加数量按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void RbtnDetailIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (_detailItem == null)
                return;
            _detailCount = Math.Min(Math.Max(1, _detailItem.Count), _detailCount + 1);
            TbDetailCount.Text = _detailCount.ToString();
        }

        /// <summary>
        /// 详情界面数量文本框按键事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// 当按下回车键时应用输入的数量
        /// </remarks>
        private void TbDetailCount_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyDetailCountFromText();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 详情界面数量文本框失去焦点事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void TbDetailCount_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyDetailCountFromText();
        }

        /// <summary>
        /// 从文本框应用数量设置
        /// </summary>
        /// <remarks>
        /// 验证并限制输入的数量值在有效范围内（1到物品拥有数量）
        /// </remarks>
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

        /// <summary>
        /// 详情界面使用按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void BtnDetailUse_Click(object sender, RoutedEventArgs e)
        {
            if (_detailItem == null)
                return;
            ApplyDetailCountFromText();
            UseItem(_detailItem, _detailCount);
            HideDetail();
        }

        private CheckBox _puswitch;
        private void Switch_Loaded(object sender, RoutedEventArgs e)
        {
            _puswitch = sender as CheckBox;
        }

    }
}
