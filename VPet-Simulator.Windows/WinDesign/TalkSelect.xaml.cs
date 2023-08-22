using LinePutScript.Localization.WPF;
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
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// TalkSelect.xaml 的交互逻辑
    /// </summary>
    public partial class TalkSelect : UserControl
    {// 使用新的选项方式的聊天框

        /// <summary>
        /// 当前存在在列表的选项
        /// </summary>
        List<SelectText> textList = new List<SelectText>();
        /// <summary>
        /// 已经说过的话
        /// </summary>
        HashSet<string> textSaid = new HashSet<string>();
        /// <summary>
        /// 下次刷新时间
        /// </summary>
        public DateTime RelsTime;

        MainWindow mw;
        public TalkSelect(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            mw.Main.ToolBar.EventShow += RelsSelect;
            RelsSelect();
        }


        /// <summary>
        /// 刷新当前所有选项
        /// </summary>
        public void RelsSelect()
        {
            if (RelsTime < DateTime.Now)
            {
                //刷新选项
                RelsTime = DateTime.Now.AddMinutes(10);//10分钟刷新一次, 每次聊天增加5分钟
                textList.Clear();
                textSaid.Clear();
                //随机选取选项
                var list = mw.SelectTexts.ToList();
                while (list.Count > 0 && textList.Count < 5)
                {
                    int sid = Function.Rnd.Next(list.Count);
                    var select = list[sid];
                    list.RemoveAt(sid);
                    if (textList.Find(x => x.Choose == select.Choose) == null && select.CheckState(mw.Main))
                    {
                        textList.Add(select);
                    }
                }
            }
            //刷新显示
            if (textList.Count > 0)
            {
                tbTalk.Items.Clear();
                foreach (var item in textList)
                {
                    if (!textSaid.Contains(item.Choose))
                    {
                        tbTalk.Items.Add(item.Choose.Translate());
                    }
                }
                btn_Send.IsEnabled = true;
            }
            else
            {
                tbTalk.Items.Clear();
                tbTalk.Items.Add("没有可以说的话".Translate());
                btn_Send.IsEnabled = false;
            }
            double min = (RelsTime - DateTime.Now).TotalMinutes;
            double prograss = 1 - min / 10;
            if (prograss > 1)
            {
                prograss = 1;
            }
            else if (prograss < 0)
            {
                prograss = Math.Min(1, Math.Max(0, min % 10)) / 2;
            }
            PrograssUsed.Value = prograss;
            PrograssUsed.ToolTip = "下次刷新剩余时间: {0:f1}分钟".Translate(min);
        }

        private void btn_Send_Click(object sender, RoutedEventArgs e)
        {
            if (tbTalk.SelectedIndex == -1 || tbTalk.Text == "没有可以说的话".Translate())
            {
                return;
            }
            var say = textList[tbTalk.SelectedIndex];
            textList.RemoveAt(tbTalk.SelectedIndex);

            textSaid.Add(say.Choose);
            RelsTime = RelsTime.AddMinutes(5);

            mw.Main.SayRnd(say.ConverText(mw.Main));
            if (say.ToTags.Count > 0)
            {
                var list = mw.SelectTexts.FindAll(x => x.ContainsTag(say.ToTags)).ToList();
                while (list.Count > 0)
                {
                    int sid = Function.Rnd.Next(list.Count);
                    var select = list[sid];
                    list.RemoveAt(sid);
                    if (textList.Find(x => x.Choose == select.Choose) == null && select.CheckState(mw.Main))
                    {
                        textList.Add(select);
                        break;
                    }
                }
            }
            RelsSelect();

        }
    }
}
