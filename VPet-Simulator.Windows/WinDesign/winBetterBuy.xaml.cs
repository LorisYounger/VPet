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
            LsbSortRule.SelectedIndex = mw.Set["betterbuy"].GetInt("lastorder");
            LsbSortAsc.SelectedIndex = mw.Set["betterbuy"].GetBool("lastasc") ? 0 : 1;
            AllowChange = true;
        }
        Run rMoney;
        public void Show(Food.FoodType type)
        {
            mw.Topmost = false;
            IsEnabled = true;//逃出
            if (_searchTextBox != null)
                _searchTextBox.Text = "";
            if (LsbCategory.SelectedIndex == (int)type)
                OrderItemSource(type, LsbSortRule.SelectedIndex, LsbSortAsc.SelectedIndex == 0);
            else
                LsbCategory.SelectedIndex = (int)type;
            if (rMoney != null)
                rMoney.Text = mw.Core.Save.Money.ToString("f2");

            //喜好度刷新
            foreach (var sub in mw.Set.PetData)
            {
                if (sub.Name.StartsWith("buytime_"))
                {
                    var name = sub.Name.Substring(8);
                    var food = mw.Foods.FirstOrDefault(x => x.Name == name);
                    if (food != null)
                    {
                        food.LoadEatTimeSource(mw);
                        food.NotifyOfPropertyChange("Eattime");
                    }
                }
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
                        foods = mw.Foods.FindAll(x => x.Type == type);
                        break;
                }
                if (!string.IsNullOrEmpty(searchtext))
                {
                    foods = foods.FindAll(x => x.TranslateName.Contains(searchtext));
                }
                IOrderedEnumerable<Food> ordered;
                switch (sortrule)
                {
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
                    default:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.Health);
                        else
                            ordered = foods.OrderByDescending(x => x.Health);
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
        private void BtnBuy_Click(object sender, RoutedEventArgs e)
        {

            var Button = sender as Button;
            var item = Button.DataContext as Food;
            IsEnabled = false;
            //看是什么模式
            if (mw.Set.EnableFunction)
            {
                if (item.Price >= mw.Core.Save.Money)
                {//买不起
                    MessageBoxX.Show("您没有足够金钱来购买 {0}\n您需要 {1:f2} 金钱来购买\n您当前 {2:f2} 拥有金钱"
                        .Translate(item.TranslateName, item.Price, mw.Core.Save.Money)
                        , "金钱不足".Translate());
                    return;
                }

                //获取吃腻时间

                DateTime now = DateTime.Now;
                DateTime eattime = mw.Set.PetData.GetDateTime("buytime_" + item.Name, now);
                double eattimes = 0;
                if (eattime <= now)
                {
                    eattime = now;
                }
                else
                {
                    eattimes = (eattime - now).TotalHours;
                }
                //开始加点
                mw.Core.Save.EatFood(item, Math.Max(0.5, 1 - Math.Pow(eattimes, 2) * 0.01));
                //吃腻了
                eattimes += 2;
                mw.Set.PetData.SetDateTime("buytime_" + item.Name, now.AddHours(eattimes));
                //通知
                item.LoadEatTimeSource(mw);
                item.NotifyOfPropertyChange(DateTime.Now.ToString());

                mw.Core.Save.Money -= item.Price;
                //统计
                mw.Set.Statistics[(gint)("buy_" + item.Name)]++;
                mw.Set.Statistics[(gdbe)"stat_betterbuy"] += item.Price;
                switch (item.Type)
                {
                    case Food.FoodType.Food:
                        mw.Set.Statistics[(gdbe)"stat_bb_food"] += item.Price;
                        break;
                    case Food.FoodType.Drink:
                        mw.Set.Statistics[(gdbe)"stat_bb_drink"] += item.Price;
                        break;
                    case Food.FoodType.Drug:
                        mw.Set.Statistics[(gdbe)"stat_bb_drug"] += item.Price;
                        break;
                    case Food.FoodType.Snack:
                        mw.Set.Statistics[(gdbe)"stat_bb_snack"] += item.Price;
                        break;
                    case Food.FoodType.Functional:
                        mw.Set.Statistics[(gdbe)"stat_bb_functional"] += item.Price;
                        break;
                    case Food.FoodType.Meal:
                        mw.Set.Statistics[(gdbe)"stat_bb_meal"] += item.Price;
                        break;
                    case Food.FoodType.Gift:
                        mw.Set.Statistics[(gdbe)"stat_bb_gift"] += item.Price;
                        break;
                }

            }
            GraphType gt;
            switch (item.Type)
            {
                default:
                    gt = GraphType.Eat;
                    break;
                case Food.FoodType.Drink:
                    gt = GraphType.Drink;
                    break;
                case Food.FoodType.Gift:
                    gt = GraphType.Gift;
                    break;
            }
            var name = mw.Core.Graph.FindName(gt);
            var ig = mw.Core.Graph.FindGraph(name, AnimatType.Single, mw.Core.Save.Mode);
            if (ig != null)
            {
                var b = mw.Main.FindDisplayBorder(ig);
                ig.Run(b, item.ImageSource, () =>
                {
                    Dispatcher.Invoke(() => IsEnabled = true);
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
            mw.Topmost = mw.Set.TopMost;
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
            Search();
        }

        private void rMoney_Loaded(object sender, RoutedEventArgs e)
        {
            rMoney = sender as Run;
            rMoney.Text = mw.Core.Save.Money.ToString("f2");
        }
    }
}
