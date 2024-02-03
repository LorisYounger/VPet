using IWshRuntimeLibrary;
using LinePutScript;
using LinePutScript.Localization.WPF;
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
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static System.Windows.Forms.LinkLabel;
using static VPet_Simulator.Core.GraphCore;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.IGraph;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winBetterBuy.xaml 的交互逻辑
    /// </summary>
    public partial class winBetterBuy : WindowX
    {
        private TextBox _searchTextBox;
        MainWindow mw;
        private bool AllowChange = false;
        private Switch _puswitch;
        private int _columns;
        private int _rows;

        public winBetterBuy(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            Title = "更好买".Translate() + ' ' + mw.PrefixSave;
            LsbSortRule.SelectedIndex = mw.Set["betterbuy"].GetInt("lastorder");
            LsbSortAsc.SelectedIndex = mw.Set["betterbuy"].GetBool("lastasc") ? 0 : 1;
            AllowChange = true;
        }
        Run rMoney;
        public void Show(Food.FoodType type)
        {
            if (!AllowChange)
                return;
            showeatanm = true;//逃出
            if (_searchTextBox != null)
                _searchTextBox.Text = "";
            if (LsbCategory.SelectedIndex == (int)type)
                OrderItemSource(type, LsbSortRule.SelectedIndex, LsbSortAsc.SelectedIndex == 0);
            else
                LsbCategory.SelectedIndex = (int)type;
            if (rMoney != null)
                rMoney.Text = mw.Core.Save.Money.ToString("f2");

            //喜好度刷新
            foreach (var sub in mw.GameSavesData["buytime"])
            {
                var name = sub.Name;
                var food = mw.Foods.FirstOrDefault(x => x.Name == name);
                if (food != null)
                {
                    food.LoadEatTimeSource(mw);
                    food.NotifyOfPropertyChange("Description");
                }
            }
            //没钱了,宠物给你私房钱 (开罗传统)
            if (mw.Core.Save.Money <= 1)
            {
                if (mw.GameSavesData[(gbol)"self"])
                {
                    MessageBoxX.Show("更好买老顾客大优惠!桌宠的食物钱我来出!\n更好买提示您:$10以下的食物/药品等随便赊账".Translate());
                }
                else
                {
                    MessageBoxX.Show("看到您囊中羞涩,{0}拿出了1000块私房钱出来给你".Translate(mw.Core.Save.Name));
                    mw.GameSavesData[(gbol)"self"] = true;
                    mw.Core.Save.Money += 1000;
                }
            }
            else if (mw.Core.Save.Money >= 11000 && mw.GameSavesData[(gbol)"self"])
            {
                mw.Core.Save.Money -= 1000;
                mw.GameSavesData[(gbol)"self"] = false;
                MessageBoxX.Show("{0}偷偷藏了1000块私房钱".Translate());
            }

            Show();
        }
        public void OrderItemSource(Food.FoodType type, int sortrule, bool sortasc, string searchtext = null)
        {
            Task.Run(() =>
            {
                List<Food> foods;
                switch (type)
                {
                    case Food.FoodType.Food:
                        foods = mw.Foods;
                        break;
                    case Food.FoodType.Star:
                        //List<Food> lf = new List<Food>();
                        //foreach (var sub in mw.Set["betterbuy"].FindAll("star"))
                        //{
                        //    var str = sub.Info;
                        //    var food = mw.Foods.FirstOrDefault(x => x.Name == str);
                        //    if (food != null)
                        //        lf.Add(food);
                        //}
                        //foods = lf;
                        foods = mw.Foods.FindAll(x => x.Star);
                        break;
                    default:
                        foods = mw.Foods.FindAll(x => x.Type == type);// || x.Type == Food.FoodType.Limit);
                        break;
                }
                if (!string.IsNullOrEmpty(searchtext))
                {
                    foods = foods.FindAll(x => x.TranslateName.Contains(searchtext));
                }
                IOrderedEnumerable<Food> ordered;
                switch (sortrule)
                {
                    default:
                    case 0:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.TranslateName);
                        else
                            ordered = foods.OrderByDescending(x => x.TranslateName);
                        break;
                    case 1:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.Price);
                        else
                            ordered = foods.OrderByDescending(x => x.Price);
                        break;
                    case 2:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.StrengthFood);
                        else
                            ordered = foods.OrderByDescending(x => x.StrengthFood);
                        break;
                    case 3:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.StrengthDrink);
                        else
                            ordered = foods.OrderByDescending(x => x.StrengthDrink);
                        break;
                    case 4:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.Strength);
                        else
                            ordered = foods.OrderByDescending(x => x.Strength);
                        break;
                    case 5:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.Feeling);
                        else
                            ordered = foods.OrderByDescending(x => x.Feeling);
                        break;
                    case 6:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.Health);
                        else
                            ordered = foods.OrderByDescending(x => x.Health);
                        break;
                    case 7:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.Exp);
                        else
                            ordered = foods.OrderByDescending(x => x.Exp);
                        break;
                    case 8:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.Likability);
                        else
                            ordered = foods.OrderByDescending(x => x.Likability);
                        break;
                }
                Dispatcher.Invoke(() =>
                {
                    var totalCount = ordered.Count();
                    var pageSize = _rows * _columns;
                    pagination.MaxPage = (int)Math.Ceiling(totalCount * 1.0 / pageSize);
                    var currentPage = Math.Max(0, Math.Min(pagination.MaxPage, pagination.CurrentPage) - 1);
                    pagination.CurrentPage = currentPage + 1;
                    IcCommodity.ItemsSource = ordered.Skip(pageSize * currentPage).Take(pageSize);
                });
            });
        }

        //private void RbtnIncrease_Click(object sender, RoutedEventArgs e)
        //{
        //    var repeatButton = sender as RepeatButton;
        //    var item = repeatButton.DataContext as BetterBuyItem;
        //    item.Quantity = Math.Max(1, item.Quantity + 1);
        //}

        //private void RbtnDecrease_Click(object sender, RoutedEventArgs e)
        //{
        //    var repeatButton = sender as RepeatButton;
        //    var item = repeatButton.DataContext as BetterBuyItem;
        //    item.Quantity = Math.Max(1, item.Quantity - 1);
        //}
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            //eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            //eventArg.Source = sender;
            //PageDetail.RaiseEvent(eventArg);
        }
        bool showeatanm = true;

        private void BtnBuy_Click(object sender, RoutedEventArgs e)
        {
            var Button = sender as Button;
            var item = Button.DataContext as Food;
            //看是什么模式
            if (mw.Set.EnableFunction)
            {//$10以内的食物允许赊账
                if (item.Price >= 10 && item.Price >= mw.Core.Save.Money)
                {//买不起
                    MessageBoxX.Show("您没有足够金钱来购买 {0}\n您需要 {1:f2} 金钱来购买\n您当前 {2:f2} 拥有金钱"
                        .Translate(item.TranslateName, item.Price, mw.Core.Save.Money)
                        , "金钱不足".Translate());
                    return;
                }
                //看看是否超模
                if (mw.HashCheck && item.IsOverLoad())
                {
                    if (MessageBoxX.Show("当前食物/物品属性超模,是否继续使用?\n使用超模食物可能会导致游戏发生不可预料的错误\n使用超模食物不影响大部分成就解锁\n本物品推荐价格为{0:f0}"
                        .Translate(item.RealPrice), "超模食物/物品使用提醒".Translate(), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                    mw.HashCheck = false;
                }

                mw.TakeItem(item);
            }
            if (showeatanm)
            {//显示动画
                showeatanm = false;
                mw.Main.Display(item.GetGraph(), item.ImageSource, () =>
                {
                    showeatanm = true;
                    mw.Main.DisplayToNomal();
                    mw.Main.EventTimer_Elapsed();
                });
            }
            if (!_puswitch.IsChecked.Value)
            {
                TryClose();
            }
            else
            {
                rMoney.Text = mw.Core.Save.Money.ToString("f2");
            }
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
            if (!AllowChange)
                return;
            var searchText = "";
            if (_searchTextBox != null)
            {
                searchText = _searchTextBox.Text;
            }
            OrderItemSource((Food.FoodType)LsbCategory.SelectedIndex, LsbSortRule.SelectedIndex, LsbSortAsc.SelectedIndex == 0, searchText);
        }

        private void TbTitleSearch_Loaded(object sender, RoutedEventArgs e)
        {
            _searchTextBox = sender as TextBox;
        }

        private void LsbSortRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            int order = LsbSortRule.SelectedIndex;
            bool asc = LsbSortAsc.SelectedIndex == 0;
            mw.Set["betterbuy"].SetInt("lastorder", order);
            mw.Set["betterbuy"].SetBool("lastasc", asc);
            OrderItemSource((Food.FoodType)LsbCategory.SelectedIndex, order, asc, _searchTextBox?.Text);
        }
        public void TryClose()
        {
            IcCommodity.ItemsSource = null;
            //mw.Topmost = mw.Set.TopMost;
            Hide();
        }
        private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TryClose();
            e.Cancel = mw.CloseConfirm;
        }

        private void Switch_Loaded(object sender, RoutedEventArgs e)
        {
            _puswitch = sender as Switch;
            _puswitch.IsChecked = mw.Set["betterbuy"].GetBool("noautoclose");
            _puswitch.Click += Switch_Checked;
        }

        private void Switch_Checked(object sender, RoutedEventArgs e)
        {
            mw.Set["betterbuy"].SetBool("noautoclose", _puswitch.IsChecked.Value);
        }

        private void AutoUniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var rows = Math.Max(0, (int)Math.Floor(IcCommodity.ActualHeight / 150d));
            if (rows != _rows)
            {
                _rows = rows;
                Search();
            }
            _rows = rows;
        }

        private void AutoUniformGrid_Changed(object sender, RoutedEventArgs e)
        {
            var uniformGrid = e.OriginalSource as AutoUniformGrid;
            var columns = uniformGrid.Columns;
            if (columns != _columns)
            {
                _columns = columns;
                Search();
            }
            _columns = columns;
        }

        private void pagination_CurrentPageChanged(object sender, SelectedValueChangedRoutedEventArgs<int> e)
        {
            if (!AllowChange)
                return;
            Search();
            TbPage.Text = e.NewValue.ToString();
        }

        private void rMoney_Loaded(object sender, RoutedEventArgs e)
        {
            rMoney = sender as Run;
            rMoney.Text = mw.Core.Save.Money.ToString("f2");
        }
        private Switch _puswitchautobuy;
        private void Switch_Loaded_1(object sender, RoutedEventArgs e)
        {
            _puswitchautobuy = sender as Switch;
            _puswitchautobuy.IsChecked = mw.Set.AutoBuy;
            _puswitchautobuy.Click += Switch_AutoBuy_Checked;
        }
        private void Switch_AutoBuy_Checked(object sender, RoutedEventArgs e)
        {
            if (_puswitchautobuy.IsChecked.Value && mw.Core.Save.Money < 100)
            {
                _puswitchautobuy.IsChecked = false;
                MessageBoxX.Show(mw, "余额不足100，无法开启自动购买".Translate(), "更好买".Translate());
                return;
            }
            if (_puswitchautobuy.IsChecked.Value)
            {
                mw.Set.AutoBuy = true;
                _puswitchautogift.Visibility = Visibility.Visible;
            }
            else
            {
                mw.Set.AutoBuy = false;
                _puswitchautogift.Visibility = Visibility.Collapsed;
            }
        }
        private Switch _puswitchautogift;
        private void Switch_Loaded_2(object sender, RoutedEventArgs e)
        {
            _puswitchautogift = sender as Switch;
            _puswitchautogift.IsChecked = mw.Set.AutoGift;
            _puswitchautogift.Click += Switch_AutoGift_Checked;
            if (mw.Set.AutoBuy)
            {
                _puswitchautogift.Visibility = Visibility.Visible;
            }
        }
        private void Switch_AutoGift_Checked(object sender, RoutedEventArgs e)
        {
            mw.Set.AutoGift = _puswitchautogift.IsChecked.Value;
        }

        private void Button_Loaded(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Content = "更好买".Translate() + mw.PrefixSave;
            ;
        }

        private void TbPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key ==  Key.Enter 
                && int.TryParse(TbPage.Text?.Trim(), out int page))
            {
                pagination.CurrentPage = Math.Max(0, Math.Min(pagination.MaxPage, page));
            }
        }
    }
}
