using IWshRuntimeLibrary;
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
using static VPet_Simulator.Core.GraphCore;
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

        public winBetterBuy(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            LsbSortRule.SelectedIndex = mw.Set["betterbuy"].GetInt("lastorder");
            LsbSortAsc.SelectedIndex = mw.Set["betterbuy"].GetBool("lastasc") ? 0 : 1;
            AllowChange = true;
        }
        public void Show(Food.FoodType type)
        {
            mw.Topmost = false;
            if (_searchTextBox != null)
                _searchTextBox.Text = "";
            if (LsbCategory.SelectedIndex == (int)type)
                OrderItemSource(type, LsbSortRule.SelectedIndex, LsbSortAsc.SelectedIndex == 0);
            else
                LsbCategory.SelectedIndex = (int)type;
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
                    foods = foods.FindAll(x => x.Name.Contains(searchtext));
                }
                IOrderedEnumerable<Food> ordered;
                switch (sortrule)
                {
                    case 0:
                        if (sortasc)
                            ordered = foods.OrderBy(x => x.Name);
                        else
                            ordered = foods.OrderByDescending(x => x.Name);
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
                    IcCommodity.ItemsSource = ordered;
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
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            PageDetail.RaiseEvent(eventArg);
        }
        private void BtnBuy_Click(object sender, RoutedEventArgs e)
        {

            var Button = sender as Button;
            var item = Button.DataContext as Food;

            //看是什么模式
            if (mw.Set.EnableFunction)
            {
                if (item.Price >= mw.Core.Save.Money)
                {//买不起
                    MessageBoxX.Show($"您没有足够金钱来购买 {item.Name}\n您需要 {item.Price:f2} 金钱来购买\n您当前 {mw.Core.Save.Money:f2} 拥有金钱"
                        , "金钱不足");
                    return;
                }
                //开始加点
                mw.Core.Save.EatFood(item);
                mw.Core.Save.Money -= item.Price;
            }
            TryClose();
            IRunImage eat = (IRunImage)mw.Core.Graph.FindGraph(GraphType.Eat, GameSave.ModeType.Nomal);
            var b = mw.Main.FindDisplayBorder(eat);
            eat.Run(b, item.ImageSource, mw.Main.DisplayToNomal);
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
            OrderItemSource((Food.FoodType)LsbCategory.SelectedIndex, LsbSortRule.SelectedIndex, LsbSortAsc.SelectedIndex == 0, _searchTextBox.Text);
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
            e.Cancel = true;
        }
    }
}
