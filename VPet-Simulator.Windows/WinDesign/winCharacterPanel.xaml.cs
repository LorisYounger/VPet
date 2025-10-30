using LinePutScript;
using LinePutScript.Localization.WPF;
using Microsoft.Win32;
using Panuon.WPF.UI;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using Color = System.Windows.Media.Color;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winCharacterPanel.xaml 的交互逻辑
    /// </summary>
    public partial class winCharacterPanel : WindowX
    {
        MainWindow mw;

        public winCharacterPanel(MainWindow mw)
        {
            this.mw = mw;
            InitializeComponent();
            Title = "面板".Translate() + ' ' + mw.PrefixSave;
            mw.Windows.Add(this);
            foreach (var v in mw.GameSavesData.Statistics.Data)
            {
                StatList.Add(new StatInfo(v.Key, v.Value.GetDouble()));
            }
            DataGridStatic.ItemsSource = StatList;
            mw.GameSavesData.Statistics.StatisticChanged += Statistics_StatisticChanged;

            if (mw.GameSavesData.HashCheck)
            {
                cb_NoCheat.IsEnabled = true;
                if (mw.IsSteamUser)
                    cb_AgreeUpload.IsEnabled = true;
            }
            Task.Run(Load_Log);
        }

        private void Statistics_StatisticChanged(Statistics sender, string name, SetObject value)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var v = StatList.FirstOrDefault(x => x.StatId == name);
                    if (v != null)
                    {
                        v.StatCount = value.GetDouble();
                    }
                    else
                    {
                        StatList.Add(new StatInfo(name, value.GetDouble()));
                    }
                }
                catch { }
            });
        }

        private ObservableCollection<StatInfo> StatList { get; set; } = new();

        private class StatInfo : INotifyPropertyChanged
        {
            public StatInfo(string statId, double statCount)
            {
                StatId = statId;
                StatCount = statCount;
                if (statId.StartsWith("buy_"))
                {
                    StatName = "购买次数".Translate() + '_' + statId.Substring(4).Translate();
                }
                else if (statId.StartsWith("stat_"))
                {
                    StatName = "统计".Translate() + '_' + statId.Substring(5).Translate();
                }
                else
                {
                    StatName = statId.Translate();
                }
            }

            /// <summary>
            /// 统计ID
            /// </summary>
            public string StatId { get; set; }

            /// <summary>
            /// 统计显示名称
            /// </summary>
            public string StatName { get; set; }

            private double _statCount;
            /// <summary>
            /// 统计内容
            /// </summary>
            public double StatCount
            {
                get { return Math.Round(_statCount, 2); }
                set
                {
                    if (_statCount != value)
                    {
                        _statCount = value;
                        OnPropertyChanged(nameof(StatCount));
                    }
                }
            }
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            public event PropertyChangedEventHandler PropertyChanged;
        }
        private void TextBox_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                DataGridStatic.ItemsSource = StatList;
            }
            else
            {
                DataGridStatic.ItemsSource = StatList.Where(
                    i =>
                        i.StatName.IndexOf(
                            textBox.Text,
                            StringComparison.InvariantCultureIgnoreCase
                        ) >= 0 || i.StatId.IndexOf(
                            textBox.Text,
                            StringComparison.InvariantCultureIgnoreCase
                        ) >= 0
                );
            }
        }

        private void WindowX_Closed(object sender, EventArgs e)
        {
            mw.GameSavesData.Statistics.StatisticChanged -= Statistics_StatisticChanged;
            mw.Windows.Remove(this);
        }

        private void btn_r_save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VPet_Rank.png"),
                Filter = "PNG Image File|*.png"
            };
            if (saveFileDialog.ShowDialog() != true)
                return;
            r_viewbox.ScrollToTop();
            FrameworkElement outputbox;
            if (r_output.ActualWidth > r_output_base.ActualWidth)
                outputbox = r_output;
            else
                outputbox = r_output_base;
            RenderTargetBitmap image = new RenderTargetBitmap((int)outputbox.ActualWidth, (int)outputbox.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            image.Render(outputbox);
            var path = saveFileDialog.FileName;
            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                File.WriteAllBytes(path, ms.ToArray());
                if (mw.IsSteamUser && cb_AgreeUpload.IsChecked == true)
                    SteamScreenshots.AddScreenshot(path, null, image.PixelWidth, image.PixelHeight);

                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
        }

        private void cb_AgreeUpload_Checked(object sender, RoutedEventArgs e)
        {
            cb_NoCheat.IsChecked = true;
        }

        private void cb_NoCheat_Unchecked(object sender, RoutedEventArgs e)
        {
            cb_AgreeUpload.IsChecked = false;
        }

        private void btn_r_genRank_Click(object sender, RoutedEventArgs e)
        {
            btn_r_genRank.IsEnabled = false;
            pb_r_genRank.Value = 0;
            pb_r_genRank.Visibility = Visibility.Visible;
            Task.Run(GenRank);
        }
        private async void GenRank()
        {
            mw.Set["v"][(gint)"rank"] = DateTime.Now.Year;
            bool useranking = mw.IsSteamUser && await Dispatcher.InvokeAsync(() => cb_AgreeUpload.IsChecked == true);

            string petname = mw.GameSavesData.GameSave.Name;
            string username = mw.IsSteamUser ? SteamClient.Name : Environment.UserName;

            int timelength = mw.GameSavesData.Statistics[(gint)"stat_total_time"];
            double timelength_h = (timelength / 3600.0);
            double startdatelength = (DateTime.Now - mw.GameSavesData[(gdat)"birthday"]).TotalDays;
            double startlengthrank = 0;
            if (useranking)
            {
                Leaderboard? leaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync("stat_total_time", LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
                var result = await leaderboard?.ReplaceScore(timelength);
                var length = leaderboard?.EntryCount ?? 1.0;
                startlengthrank = 1 - ((result?.NewGlobalRank - 1) ?? length) / length;
            }
            string startlengthranktext;
            if (startlengthrank < 0.5)
                startlengthranktext = '"' + "主人~多陪陪我~".Translate() + '"';
            else
                startlengthranktext = '"' + "主人~感谢陪伴~".Translate() + '"';

            double timelengthph = timelength_h / startdatelength;
            string timelengthphtext;
            string timelengthtext;
            int timelength_i;
            if (timelengthph < 2)
            {
                timelengthphtext = "同学".Translate();
                timelengthtext = '"' + "学长~前辈~".Translate() + '"';
                timelength_i = 1;
            }
            else if (timelengthph < 4)
            {
                timelengthphtext = "朋友".Translate();
                timelengthtext = '"' + "兄弟!".Translate() + '"';
                timelength_i = 2;
            }
            else if (timelengthph < 7)
            {
                timelengthphtext = "挚友".Translate();
                timelengthtext = '"' + "不求同年同月同日生，但求同年同月同日打开《虚拟桌宠模拟器》".Translate() + '"';
                timelength_i = 3;
            }
            else if (timelengthph < 10)
            {
                timelengthphtext = "家人".Translate();
                timelengthtext = '"' + "We are 伐木累~".Translate() + '"';
                timelength_i = 4;
            }
            else
            {
                timelengthphtext = "女鹅".Translate();
                timelengthtext = '"' + "爸妈~ 这么叫好像不太好".Translate() + '"';
                timelength_i = 5;
            }

            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 10);
            string studytext;
            int study_i;
            if (mw.GameSavesData.GameSave.Level < 20)
            {
                studytext = "相当于桌宠的小学学历哦\n\"肃清! {0}的安魂曲☆\"".Translate(petname);
                study_i = 1;
            }
            else if (mw.GameSavesData.GameSave.Level < 40)
            {
                studytext = "相当于桌宠的中学学历哦\n<高考桌宠100天>".Translate();
                study_i = 2;
            }
            else if (mw.GameSavesData.GameSave.Level < 60)
            {
                studytext = "相当于桌宠的大学学历哦\n\"大学生上课吃饭睡觉, {0}学习吃饭睡觉, {0}＝大学生\"".Translate(petname);
                study_i = 3;
            }
            else if (mw.GameSavesData.GameSave.Level < 80)
            {
                studytext = "相当于桌宠的博士学历哦\n\"大学生上课吃饭睡觉, 人家和那个带兜帽的没关系啦\"".Translate();
                study_i = 4;
            }
            else
            {
                studytext = "<虚拟桌宠模拟器砖家>\n\"一定是{0}干的!\"".Translate(username);
                study_i = 5;
            }

            int studyexpmax, studymoneymax;
            double studyexpmaxrank = 0, studymoneymaxrank = 0;
            if (mw.IsSteamUser)
            {
                studyexpmax = SteamUserStats.GetStatInt("stat_single_profit_exp");
                studymoneymax = SteamUserStats.GetStatInt("stat_single_profit_money");
            }
            else
            {
                studyexpmax = mw.GameSavesData.Statistics[(gint)"stat_single_profit_exp"];
                studymoneymax = mw.GameSavesData.Statistics[(gint)"stat_single_profit_money"];
            }
            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 20);
            if (useranking)
            {
                Leaderboard? leaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync("stat_single_profit_exp", LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
                var result = await leaderboard?.ReplaceScore(studyexpmax);
                var length = leaderboard?.EntryCount ?? 1.0;
                if (result?.NewGlobalRank != null)
                    studyexpmaxrank = 1 - (result.Value.NewGlobalRank - 1) / length;
                else
                    studyexpmaxrank = 0;

                leaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync("stat_single_profit_money", LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
                result = await leaderboard?.ReplaceScore(studymoneymax);
                length = leaderboard?.EntryCount ?? 1.0;
                if (result?.NewGlobalRank != null)
                    studymoneymaxrank = 1 - (result.Value.NewGlobalRank - 1) / length;
                else
                    studymoneymaxrank = 0;
            }
            string studyexptext, workmoneytext;
            int studyexp_i, workmoney_i;
            if (studyexpmaxrank < 0.25)
            {
                studyexptext = '"' + "在你这个年纪,你怎么睡得着觉的?".Translate() + '"';
                studyexp_i = 5;
            }
            else if (studyexpmaxrank < 0.4)
            {
                studyexptext = '"' + "孩子学习老不好，多半是废了，快来试试思维驰学习机".Translate() + '"';
                studyexp_i = 4;
            }
            else if (studyexpmaxrank < 0.55)
            {
                studyexptext = '"' + "孩子学习老不好，多半是废了，快来试试思维驰学习机".Translate() + '"';
                studyexp_i = 3;
            }
            else if (studyexpmaxrank < 0.75)
            {
                studyexptext = '"' + "学而不思则罔，思而不学则die".Translate() + '"';
                studyexp_i = 2;
            }
            else
            {
                studyexptext = '"' + "看我量子速读法!".Translate() + '"';
                studyexp_i = 1;
            }

            if (studymoneymaxrank < 0.25)
            {
                workmoneytext = '"' + "钱钱乃身外之物".Translate() + '"';
                workmoney_i = 4;
            }
            else if (studymoneymaxrank < 0.5)
            {
                workmoneytext = '"' + "风声雨声读书声声声入耳，日结月结次次结钱钱入账".Translate() + '"';
                workmoney_i = 3;
            }
            else if (studymoneymaxrank < 0.75)
            {
                workmoneytext = '"' + "有钱能使磨推鬼".Translate() + '"';
                workmoney_i = 2;
            }
            else
            {
                workmoneytext = '"' + "可是，我真的很需要那些钱钱!".Translate() + '"';
                workmoney_i = 1;
            }

            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 40);

            int worktime = mw.GameSavesData.Statistics[(gint)"stat_work_time"];
            double worktimeph = (double)worktime / timelength;
            double worktimephrank = 0;
            if (useranking)
            {
                Leaderboard? leaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync("stat_work_time_ph", LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
                var result = await leaderboard?.ReplaceScore((int)(worktimeph * 10000));
                var length = leaderboard?.EntryCount ?? 1.0;
                if (result?.NewGlobalRank != null)
                    worktimephrank = 1 - (result.Value.NewGlobalRank - 1) / length;
                else worktimephrank = 0;
            }
            string worktimephtext;
            int worktime_i;
            if (worktimephrank < 0.25)
            {
                worktimephtext = '"' + "干一天来歇一天, 能混一天是一天".Translate() + '"';
                worktime_i = 1;
            }
            else if (worktimephrank < 0.35)
            {
                worktimephtext = '"' + "早8晚5，快乐回家".Translate() + '"';
                worktime_i = 2;
            }
            else if (worktimephrank < 0.45)
            {
                worktimephtext = '"' + "早8晚5，快乐回家".Translate() + '"';
                worktime_i = 3;
            }
            else if (worktimephrank < 0.55)
            {
                worktimephtext = '"' + "早8晚5，快乐回家".Translate() + '"';
                worktime_i = 4;
            }
            else if (worktimephrank < 0.75)
            {
                worktimephtext = '"' + "加班没有加班费不是基本常识吗?".Translate() + '"';
                worktime_i = 5;
            }
            else
            {
                worktimephtext = '"' + "老板! 路灯已经准备好了!".Translate() + '"';
                worktime_i = 6;
            }

            int betterbuytimes = mw.GameSavesData.Statistics[(gint)"stat_buytimes"];
            int betterbuycount = (int)mw.GameSavesData.Statistics[(gdbe)"stat_betterbuy"];

            Food mostfood = new Food()
            {
                Name = "None",
            };

            foreach (var pair in mw.GameSavesData.Statistics.Data.Where(x => x.Key.StartsWith("buy_")).OrderByDescending(x => ((int)x.Value)))
            {
                var fn = pair.Key.Substring(4);
                var f = mw.Foods.FirstOrDefault(x => x.Name == fn);
                if (f != null)
                {
                    mostfood = f;
                    break;
                }
            }

            string foodtext = "啥也没吃,{0}都饿坏了".Translate(petname);
            switch (mostfood.Type)
            {
                case Food.FoodType.Meal:
                    foodtext = '"' + "人是铁饭是钢, 四菜一汤吃得香".Translate() + '"';
                    break;
                case Food.FoodType.Drug:
                    foodtext = '"' + "自动购买又忘开了吧?".Translate() + '"';
                    break;
                case Food.FoodType.Drink:
                    foodtext = '"' + "多喝热水".Translate() + '"';
                    break;
                case Food.FoodType.Functional:
                    foodtext = '"' + "不是正餐买不起, 而是功能性更有性价比".Translate() + '"';
                    break;
                case Food.FoodType.Snack:
                    foodtext = '"' + "多吃零食有益心理健康".Translate() + '"';
                    break;
                case Food.FoodType.Gift:
                    foodtext = '"' + "公若不弃，{0}愿拜为义父!".Translate(petname) + '"';
                    break;
            }

            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 60);

            int autobuytimes = mw.GameSavesData.Statistics[(gint)"stat_autobuy"];
            double autobuytimesph = (double)autobuytimes / betterbuytimes;
            double autobuytimesphrank = 0;
            if (useranking)
            {
                Leaderboard? leaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync("stat_autobuy_ph", LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
                var result = await leaderboard?.ReplaceScore((int)(autobuytimesph * 10000));
                var length = leaderboard?.EntryCount ?? 1.0;
                if (result?.NewGlobalRank != null)
                    autobuytimesphrank = 1 - (result.Value.NewGlobalRank - 1) / length;
                else
                    autobuytimesphrank = 0;
            }
            string autobuytext;
            int autobuy_i;
            if (autobuytimesph < 0.25)
            {
                autobuytext = '"' + "主人, 是担心我乱买东西嘛".Translate() + '"';
                autobuy_i = 4;
            }
            else if (autobuytimesph < 0.5)
            {
                autobuytext = '"' + "自己赚的钱自己花".Translate() + '"';
                autobuy_i = 3;
            }
            else if (autobuytimesph < 0.75)
            {
                autobuytext = '"' + "不要小看我的情报网! 你自动购买礼物没关,对不对?".Translate() + '"';
                autobuy_i = 2;
            }
            else
            {
                autobuytext = '"' + "诚招保姆,工资面议".Translate() + '"';
                autobuy_i = 1;
            }

            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 70);

            var modworkshoplist = mw.CoreMODs.FindAll(x => x.Path.FullName.Contains("workshop"));
            int modworkshop = modworkshoplist.Count;
            int modon = modworkshoplist.FindAll(x => x.IsOnMOD(mw)).Count;
            double modworkshoprank = 0;
            if (useranking)
            {
                Leaderboard? leaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync("workshop", LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
                var result = await leaderboard?.ReplaceScore(modworkshop);
                var length = leaderboard?.EntryCount ?? 1.0;
                if (result?.NewGlobalRank != null)
                    modworkshoprank = 1 - (result.Value.NewGlobalRank - 1) / length;
                else
                    modworkshoprank = 0;
            }
            string modworkshoptext;
            int modworkshop_i;
            if (modworkshop == 0)
            {
                modworkshoptext = '"' + "桌宠的steam创意工坊里有许多的mod喵, 主人快去试试吧".Translate() + '"';
                modworkshop_i = 3;
            }
            else if (modworkshoprank < 0.3)
            {
                modworkshoptext = '"' + "主人还可以再去创意工坊体验更多MOD喵".Translate() + '"';
                modworkshop_i = 3;
            }
            else if (modworkshoprank < 0.7)
            { modworkshoptext = '"' + "创意工坊又更新了很多有趣的mod喵, 主人要不要去看看?".Translate() + '"'; modworkshop_i = 2; }
            else
            { modworkshoptext = '"' + "主人已经是mod大师了喵,要不要试试mod制作器,给我做mod喵!".Translate() + '"'; modworkshop_i = 1; }

            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 80);

            int like = (int)mw.GameSavesData.GameSave.Likability;
            string liketext = "";
            while (like > 100)
            {
                like -= 100;
                liketext += '\uEE0E';
            }
            while (like > 50)
            {
                like -= 50;
                liketext += '\uEE0F';
            }
            if (liketext.Length == 0)
            {
                liketext = "\uEECA";
            }
            double likerank = 0;
            if (useranking)
            {
                Leaderboard? leaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync("stat_likability", LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
                var result = await leaderboard?.ReplaceScore((int)mw.GameSavesData.GameSave.Likability);
                var length = leaderboard?.EntryCount ?? 1.0;
                if (result?.NewGlobalRank != null)
                    likerank = 1 - (result.Value.NewGlobalRank - 1) / length;
                else
                    likerank = 0;
            }
            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 88);

            await Dispatcher.InvokeAsync(() =>
            {
                r_r_startday.Text = mw.GameSavesData[(gdat)"birthday"].ToLongDateString();
                r_r_startlength.Text = startdatelength.ToString("f1");
                r_r_length_h.Text = timelength_h.ToString("f1");
                r_r_length_p.Text = startlengthrank.ToString("p1");
                r_r_lenghranktext.Text = startlengthranktext;

                r_r_lengthph.Text = timelengthph.ToString("f1");
                r_r_lengthphtext.Text = timelengthphtext;
                r_r_lenghtext.Text = timelengthtext;
                r_i_timelength.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_timelength_{timelength_i}.png"));

                r_r_level.Text = mw.GameSavesData.GameSave.Level.ToString();
                r_r_exp.Text = mw.GameSavesData.GameSave.TotalExpGained().ToString("f0");
                r_r_studytime.Text = (mw.GameSavesData.Statistics[(gint)"stat_study_time"] / 60).ToString();
                r_r_studytext.Text = studytext;
                r_i_exp.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_level_{study_i}.png"));

                r_r_studyexpmax.Text = studyexpmax.ToString();
                r_r_studyexpmaxrank.Text = studyexpmaxrank.ToString("p1");
                r_r_studyexptext.Text = studyexptext;
                r_i_singleexp.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_singleexp_{studyexp_i}.png"));

                r_r_worktime.Text = (worktime / 60).ToString();
                r_r_worktimeps.Text = worktimeph.ToString("p1");
                r_r_worktimepsrank.Text = worktimephrank.ToString("p1");
                r_r_worktext.Text = worktimephtext;
                r_i_money.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_worktime_{worktime_i}.png"));

                r_r_workmoneymax.Text = studymoneymax.ToString();
                r_r_workmoneyrank.Text = studymoneymaxrank.ToString("p1");
                r_r_workmoneytext.Text = workmoneytext;
                r_i_singlemoney.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_singlemoney_{workmoney_i}.png"));

                r_r_username.Text = username;
                r_r_petname.Text = r_r_petname_2.Text = r_r_petname_3.Text = r_r_petname_4.Text = petname;
                r_r_now.Text = DateTime.Now.ToShortDateString();

                r_r_betterbuytimes.Text = betterbuytimes.ToString();
                r_r_betterbuycount.Text = betterbuycount.ToString();
                r_r_betterbuymosttype.Text = mostfood.Type.ToString().Translate();
                r_r_betterbuymostitem.Text = mostfood.TranslateName;
                r_r_betterbuymosttext.Text = foodtext;
                r_i_mostfood.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_mostfood_{mostfood.Type}.png"));

                r_r_autobuy.Text = autobuytimes.ToString();
                r_r_autobuypres.Text = autobuytimesph.ToString("p1");
                r_r_autobuyrank.Text = autobuytimesphrank.ToString("p1");
                r_r_autobuytext.Text = autobuytext;
                r_i_autobuy.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_autobuy_{autobuy_i}.png"));

                r_r_modcount.Text = modworkshop.ToString();
                r_r_modenablecount.Text = modon.ToString();
                r_r_modcountrank.Text = modworkshoprank.ToString("p1");
                r_r_modcounttext.Text = modworkshoptext;
                r_i_mod.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_mod_{modworkshop_i}.png"));

                r_r_sleeplength.Text = (mw.GameSavesData.Statistics[(gint)"stat_sleep_time"] / 3600.0).ToString("f1");
                r_r_movelength.Text = px_tocm(mw.GameSavesData.Statistics[(gi64)"stat_move_length"], out string cm);
                r_r_movelengthcm.Text = cm;
                r_r_saycount.Text = mw.GameSavesData.Statistics[(gint)"stat_say_times"].ToString();
                r_r_musiccount.Text = mw.GameSavesData.Statistics[(gint)"stat_music"].ToString();
                r_r_touchtotal.Text = (mw.GameSavesData.Statistics[(gint)"stat_touch_body"] + mw.GameSavesData.Statistics[(gint)"stat_touch_head"]).ToString();

                if (mw.GameSavesData.GameSave.Likability > 100)
                    r_i_like.Visibility = Visibility.Visible;
                else
                    r_i_like.Visibility = Visibility.Collapsed;

                r_r_opencount.Text = mw.GameSavesData.Statistics[(gint)"stat_open_times"].ToString();
                r_r_bettercount.Text = mw.GameSavesData.Statistics[(gint)"stat_100_all"].ToString();
                r_r_likecount.Text = liketext;
                r_r_likecountrank.Text = likerank.ToString("p1");

                r_viewbox.Visibility = Visibility.Visible;
                btn_r_genRank.IsEnabled = true;
                btn_r_save.IsEnabled = true;
                pb_r_genRank.Visibility = Visibility.Collapsed;
                Width = 800;
                Height = 800;
            });
        }
        private string px_tocm(long px, out string cm)
        {
            if (px < 37795)
            {
                cm = "px";
                return px.ToString();
            }
            else if (px < 3779527)
            {
                cm = "cm";
                return (px * 2.54 / 96).ToString("f1");
            }
            else if (px < 377952755)
            {
                cm = "m";
                return (px * 2.54 / 9600).ToString("f1");
            }
            else
            {
                cm = "km";
                return (px * 2.54 / 9600000).ToString("f1");
            }
        }

        bool load2 = false;
        List<(string name, Core.PetLoader loader)> bdpetlist = null;
        private async void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTab.SelectedIndex == 2 && load2 == false)
            {
                var petloader = mw.Pets.Find(x => x.Name == mw.Set.PetGraph);
                petloader ??= mw.Pets[0];
                var ordname = petloader.PetName.Translate();

                bdpetlist = mw.Pets.FindAll(x => x.Config.Data.FindLine("bday") != null).Select(x => (x.PetName.Translate(), x)).ToList();

                if (bdpetlist.Count == 0)
                {
                    tab_bday.IsEnabled = false;
                    return;
                }

                int sidx = 0;
                for (int i = 0; i < bdpetlist.Count; i++)
                {
                    if (bdpetlist[i].name == ordname)
                    {
                        if (petloader == bdpetlist[i].loader)
                        {
                            sidx = i;
                            bdpetlist[i] = (mw.GameSavesData.GameSave.Name, bdpetlist[i].loader);
                        }
                        else
                            bdpetlist[i] = (mw.GameSavesData.GameSave.Name + $"({bdpetlist[i].loader.Name.Translate()})", bdpetlist[i].loader);
                    }
                }
                cb_birthday.ItemsSource = bdpetlist.Select(x => x.name);
                lb_b_datetime.Content = "Shot on VPet - " + DateTime.Now.ToShortDateString();
                if (mw.IsSteamUser)
                {
                    Steamworks.Data.Image? img = await SteamFriends.GetLargeAvatarAsync(SteamClient.SteamId);
                    img_b_head.Source = winMutiPlayer.ConvertToImageSource(img);
                }
                cb_birthday.SelectedIndex = sidx;
                BDay_Load();
                Width = 800;
                Height = 675;
                load2 = true;
            }

        }
        public void BDay_Load()
        {
            var pl = bdpetlist[cb_birthday.SelectedIndex];
            img_b_background.Source = mw.ImageSources.FindImage("bday_" + pl.loader.Name);
            tb_bdiy.Text = "{0} 祝 {1} 生日快乐!".Translate(pl.name, mw.GameSavesData.GameSave.HostName);
            ILine bdinfo = pl.loader.Config.Data.FindLine("bday");
            img_b_head.Width = bdinfo[(gdbe)"w"];
            img_b_head.Margin = new Thickness(bdinfo[(gdbe)"x"], bdinfo[(gdbe)"y"], 0, 0);
            lb_b_datetime.VerticalAlignment = Enum.Parse<VerticalAlignment>(bdinfo[(gstr)"va"], true);
            lb_b_datetime.HorizontalAlignment = Enum.Parse<HorizontalAlignment>(bdinfo[(gstr)"ha"], true);
            b_b_text.Background = new SolidColorBrush(Function.HEXToColor('#' + bdinfo[(gstr)"tb"]));
            tb_b_text.Foreground = new SolidColorBrush(Function.HEXToColor('#' + bdinfo[(gstr)"tf"]));
            vb_b_text.Margin = new Thickness(bdinfo[(gdbe)"tleft"], bdinfo[(gdbe)"ttop"], bdinfo[(gdbe)"tright"], bdinfo[(gdbe)"tbottom"]);
        }

        private void cb_birthday_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (load2 == false)
            {
                return;
            }
            BDay_Load();
        }

        private void btn_b_headimage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Image File|*.png;*.jpg;*.jpeg;",
                Title = "选择头像图片".Translate()
            };
            if (openFileDialog.ShowDialog() == true)
            {
                img_b_head.Source = new BitmapImage(new Uri(openFileDialog.FileName));
            }
        }

        private void btn_b_save_Click(object sender, RoutedEventArgs e)
        {
            if (load2 == false)
            {
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VPet_BDay.png"),
                Filter = "PNG Image File|*.png"
            };
            if (saveFileDialog.ShowDialog() != true)
                return;
            RenderTargetBitmap image = new RenderTargetBitmap((int)b_Output.ActualWidth, (int)b_Output.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            image.Render(b_Output);
            var path = saveFileDialog.FileName;
            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                File.WriteAllBytes(path, ms.ToArray());
                if (mw.IsSteamUser && cb_AgreeUpload.IsChecked == true)
                    SteamScreenshots.AddScreenshot(path, null, image.PixelWidth, image.PixelHeight);

                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
        }

        private void dtp_bdiy_SelectedDateTimeChanged(object sender, Panuon.WPF.SelectedValueChangedRoutedEventArgs<DateTime?> e)
        {
            if (load2 == false)
            {
                return;
            }
            lb_b_datetime.Content = "Shot on VPet - " + dtp_bdiy.SelectedDateTime?.ToShortDateString();
        }

        private void Load_Log()
        {
            string text = "";
            if (mw.Set.DeBug)
                text = string.Join('\n', mw.ActivityLogs.Select(x => x.ToString(mw.Main)).ToList());
            else
                text = string.Join('\n', mw.ActivityLogs.Where(x => x.IsDebug == false).Select(x => x.ToString(mw.Main)).ToList());
            Dispatcher.Invoke(() =>
            {
                tb_log.Text = text;
            });
            mw.ActivityLogs.CollectionChanged += ActivityLogs_CollectionChanged;
        }

        private void ActivityLogs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.NewItems != null)
                {
                    foreach (ActivityLog log in e.NewItems)
                    {
                        if (mw.Set.DeBug || log.IsDebug == false)
                        {
                            tb_log.AppendText("\n" + log.ToString(mw.Main));
                        }
                    }
                }
            });
        }
    }
}
