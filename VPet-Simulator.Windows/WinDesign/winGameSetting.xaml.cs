﻿using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using Steamworks;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winGameSetting.xaml 的交互逻辑
    /// </summary>
    public partial class winGameSetting : WindowX
    {
        MainWindow mw;
        private bool AllowChange = false;
        public winGameSetting(MainWindow mw)
        {
            this.mw = mw;
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            InitializeComponent();
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            //var bit = new BitmapImage(new Uri("pack://application:,,,/Res/TopLogo2019.png"));
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            ////ImageWHY.Source = bit;
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            TopMostBox.IsChecked = mw.Set.TopMost;
            if (mw.Set.IsBiggerScreen)
            {
                FullScreenBox.IsChecked = true;
                ZoomSlider.Maximum = 8;
            }
            else
            {
                FullScreenBox.IsChecked = false;
                ZoomSlider.Maximum = 3;
            }
            ZoomSlider.Value = mw.Set.ZoomLevel * 2;
            //this.Width = 400 * Math.Sqrt(ZoomSlider.Value);
            //this.Height = 450 * Math.Sqrt(ZoomSlider.Value);

            CalFunctionBox.IsChecked = mw.Set.EnableFunction;
            CalSlider.Value = mw.Set.LogicInterval;
            InteractionSlider.Value = mw.Set.InteractionCycle;
            MoveEventBox.IsChecked = mw.Set.AllowMove;
            SmartMoveEventBox.IsChecked = mw.Set.SmartMove;
            PressLengthSlider.Value = mw.Set.PressLength / 1000.0;
            SwitchMsgOut.IsChecked = mw.Set.MessageBarOutside;
            SwitchHideFromTaskControl.IsChecked = mw.Set.HideFromTaskControl;

            StartUpBox.IsChecked = mw.Set.StartUPBoot;
            StartUpSteamBox.IsChecked = mw.Set.StartUPBootSteam;
            TextBoxPetName.Text = mw.Core.Save.Name;
            foreach (PetLoader pl in mw.Pets)
            {
                PetBox.Items.Add(pl.Name.Translate());
            }
            int petboxid = mw.Pets.FindIndex(x => x.Name == mw.Set.PetGraph);
            if (petboxid == -1)
                petboxid = 0;
            PetBox.SelectedIndex = petboxid;
            PetIntor.Text = mw.Pets[petboxid].Intor.Translate();

            TextBoxStartUpX.Text = mw.Set.StartRecordPoint.X.ToString();
            TextBoxStartUpY.Text = mw.Set.StartRecordPoint.Y.ToString();
            numBackupSaveMaxNum.Value = mw.Set.BackupSaveMaxNum;
            combCalFunState.SelectedIndex = (int)mw.Set.CalFunState;
            combCalFunState.IsEnabled = !mw.Set.EnableFunction;
            CalTimeInteraction();

            swAutoCal.IsChecked = !mw.Set["gameconfig"].GetBool("noAutoCal");


            LanguageBox.Items.Add("null");
            foreach (string v in LocalizeCore.AvailableCultures)
            {
                LanguageBox.Items.Add(v);
            }
            LanguageBox.SelectedItem = LocalizeCore.CurrentCulture;

            HitThroughBox.IsChecked = mw.Set.HitThrough;
            PetHelperBox.IsChecked = mw.Set.PetHelper;

            if (mw.Set.StartRecordLast == true)
            {
                StartPlace.IsChecked = true;
                TextBoxStartUpX.IsEnabled = false;
                TextBoxStartUpY.IsEnabled = false;
                BtnStartUpGet.IsEnabled = false;
            }
            else
            {
                StartPlace.IsChecked = false;
                TextBoxStartUpX.IsEnabled = true;
                TextBoxStartUpY.IsEnabled = true;
                BtnStartUpGet.IsEnabled = true;
            }

            if (mw.Set.Diagnosis)
                RBDiagnosisYES.IsChecked = true;

            List<int> cbDiagnosis = new List<int> { 200, 500, 1000, 2000, 5000, 10000, 20000 };
            int ds = cbDiagnosis.IndexOf(mw.Set.DiagnosisInterval);
            if (ds == -1)
                ds = 1;
            CBDiagnosis.SelectedIndex = ds;

            foreach (ComboBoxItem v in CBAutoSave.Items)
            {
                if ((int)v.Tag == mw.Set.AutoSaveInterval)
                {
                    CBAutoSave.SelectedItem = v;
                    break;
                }
            }
            foreach (ComboBoxItem v in CBSmartMove.Items)
            {
                if ((int)v.Tag == mw.Set.SmartMoveInterval)
                {
                    CBSmartMove.SelectedItem = v;
                    break;
                }
            }

            VoiceCatchSilder.Value = mw.Set.MusicCatch;
            VoiceMaxSilder.Value = mw.Set.MusicMax;

            foreach (Sub sub in mw.Set["diy"])
                StackDIY.Children.Add(new DIYViewer(sub));

            SliderResolution.Maximum = Math.Min(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
            SliderResolution.Value = mw.Set.Resolution;

#if X64
            GameVerison.Content = "游戏版本".Translate() + $"v{mw.Version} x64";
#else
            GameVerison.Content = "游戏版本".Translate() + $"v{mw.Version} x86";
#endif
            //关于ui
            if (mw.IsSteamUser)
            {
                runUserName.Text = Steamworks.SteamClient.Name;
                runActivate.Text = "已通过Steam[{0}]激活服务注册".Translate(Steamworks.SteamClient.SteamId.Value.ToString("x").Substring(6));
            }
            else
            {
                runUserName.Text = Environment.UserName;
                runActivate.Text = "尚未激活 您可能需要启动Steam或去Steam上免费领个".Translate();
            }
            //CGPT
            if (mw.TalkAPI.Count > 0)
            {
                foreach (var v in mw.TalkAPI)
                    cbChatAPISelect.Items.Add(v.APIName.Translate());
                if (mw.TalkAPIIndex != -1)
                    cbChatAPISelect.SelectedIndex = mw.TalkAPIIndex;
            }
            else
            {
                cbChatAPISelect.Items.Add("暂无聊天API, 您可以通过订阅MOD添加".Translate());
                cbChatAPISelect.SelectedIndex = 0;
                cbChatAPISelect.IsEnabled = false;
            }
            switch (mw.Set["CGPT"][(gstr)"type"])
            {
                //case "API":
                //    RBCGPTUseAPI.IsChecked = true;
                //    BtnCGPTReSet.Content = "打开 ChatGPT API 设置".Translate();
                //    break;
                case "DIY":
                    RBCGPTDIY.IsChecked = true;
                    BtnCGPTReSet.Content = "打开 {0} 设置".Translate(mw.TalkBoxCurr?.APIName ?? "Steam Workshop");
                    break;
                case "LB":
                    RBCGPTUseLB.IsChecked = true;
                    BtnCGPTReSet.Content = "初始化桌宠聊天程序".Translate();
                    //if (!mw.IsSteamUser)
                    //    BtnCGPTReSet.IsEnabled = false;
                    break;
                case "OFF":
                default:
                    RBCGPTClose.IsChecked = true;
                    BtnCGPTReSet.Content = "聊天框已关闭".Translate();
                    break;
            }
            runabVer.Text = $"v{mw.Version} ({mw.version})";

            //mod列表
            ShowModList();
            ListMod.SelectedIndex = 0;
            ShowMod((string)((ListBoxItem)ListMod.SelectedItem).Content);

            voicetimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(0.2)
            };
            voicetimer.Tick += Voicetimer_Tick;

            AllowChange = true;

            UpdateMoveAreaText();
        }

        private void Voicetimer_Tick(object sender, EventArgs e)
        {
            var v = mw.AudioPlayingVolume();
            RVoice.Text = v.ToString("p2");
            if (v > mw.Set.MusicCatch)
            {
                RVoice.Foreground = new SolidColorBrush(Colors.Green);
                if (v > mw.Set.MusicMax)
                {
                    RVoice.FontWeight = FontWeights.Bold;
                }
                else
                    RVoice.FontWeight = FontWeights.Normal;
            }
            else
                RVoice.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
        }

        public void ShowModList()
        {
            ListMod.Items.Clear();
            foreach (CoreMOD mod in mw.CoreMODs)
            {
                ListBoxItem moditem = (ListBoxItem)ListMod.Items[ListMod.Items.Add(new ListBoxItem())];
                moditem.Padding = new Thickness(5, 0, 5, 0);
                moditem.Content = mod.Name;
                if (!mod.IsOnMOD(mw))
                {
                    moditem.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
                else
                {
                    if (mod.GameVer / 10 == mw.version / 10)
                    {
                        moditem.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
                    }
                    else
                    {
                        moditem.Foreground = new SolidColorBrush(Color.FromRgb(190, 0, 0));
                    }
                }
            }
        }
        CoreMOD mod;
        private void ShowMod(string modname)
        {
            mod = mw.CoreMODs.Find(x => x.Name == modname);
            LabelModName.Content = mod.Name.Translate();
            runMODAuthor.Text = mod.Author;
            runMODGameVer.Text = CoreMOD.INTtoVER(mod.GameVer);
            runMODGameVer.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
            ImageMOD.Source = new BitmapImage(new Uri(mod.Path.FullName + @"\icon.png"));
            if (mod.GameVer < mw.version)
            {
                if (mod.GameVer / 10 == mw.version / 10)
                {
                    runMODGameVer.Text += " (兼容)".Translate();
                }
                else
                {
                    runMODGameVer.Text += " (版本低)".Translate();
                    runMODGameVer.Foreground = new SolidColorBrush(Color.FromRgb(190, 0, 0));
                }
            }
            else if (mod.GameVer > mw.version)
            {
                if (mod.GameVer / 10 == mw.version / 10)
                {
                    runMODGameVer.Text += " (兼容)".Translate();
                    runMODGameVer.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
                }
                else
                {
                    runMODGameVer.Text += " (版本高)".Translate();
                    runMODGameVer.Foreground = new SolidColorBrush(Color.FromRgb(190, 0, 0));
                }
            }
            if (!mod.IsOnMOD(mw))
            {
                LabelModName.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonEnable.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker);
                ButtonDisEnable.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonEnable.IsEnabled = true;
                ButtonDisEnable.IsEnabled = false;
            }
            else
            {
                LabelModName.Foreground = runMODGameVer.Foreground;
                ButtonDisEnable.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker);
                ButtonEnable.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonEnable.IsEnabled = false;
                ButtonDisEnable.IsEnabled = true;
            }
            //发布steam等功能
            if (mw.IsSteamUser)
            {
                if (mod.ItemID == 1)
                {
                    ButtonSteam.IsEnabled = false;
                    ButtonPublish.Text = "系统自带".Translate();
                    ButtonSteam.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
                else if (mod.ItemID == 0)
                {
                    ButtonSteam.IsEnabled = false;
                    ButtonPublish.Text = "上传至Steam".Translate();
                    ButtonSteam.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
                else
                {
                    ButtonSteam.IsEnabled = true;
                    ButtonPublish.Text = "更新至Steam".Translate();
                    ButtonSteam.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker);
                }
                if (mod.ItemID != 1 && (mod.AuthorID == Steamworks.SteamClient.SteamId.AccountId || mod.AuthorID == 0))
                {
                    ButtonPublish.IsEnabled = true;
                    ButtonPublish.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker);
                }
                else
                {
                    ButtonPublish.IsEnabled = false;
                    ButtonPublish.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
            }
            else
            {
                ButtonSteam.IsEnabled = false;
                ButtonPublish.Text = "未登录".Translate();
                ButtonPublish.IsEnabled = false;
                ButtonPublish.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonSteam.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            }
            runMODVer.Text = CoreMOD.INTtoVER(mod.Ver);
            GameInfo.Text = mod.Intro.Translate();
            string content = "";
            foreach (string tag in mod.Tag)
            {
                content += tag.Translate() + "\n";
            }
            GameHave.Text = content;
            ButtonAllow.Visibility = mod.SuccessLoad || mw.Set.IsPassMOD(mod.Name) ? Visibility.Collapsed : Visibility.Visible;

            foreach (var mainplug in mw.Plugins)
            {
                try
                {
                    if (mainplug.PluginName == mod.Name &&
                        mainplug.GetType().GetMethod("Setting").DeclaringType != typeof(MainPlugin)
                    && mainplug.GetType().Assembly.Location.Contains(mod.Path.FullName))
                    {
                        ButtonSetting.Visibility = Visibility.Visible;
                        return;
                    }
                }
                finally { }
            }
            ButtonSetting.Visibility = Visibility.Collapsed;
        }
        private void FullScreenBox_Check(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (FullScreenBox.IsChecked == true)
            {
                mw.Set.IsBiggerScreen = true;
                ZoomSlider.Maximum = 8;
            }
            else
            {
                mw.Set.IsBiggerScreen = false;
                ZoomSlider.Maximum = 3;
            }
        }

        private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void FontBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CBAutoSave_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.AutoSaveInterval = (int)((ComboBoxItem)CBAutoSave.SelectedItem).Tag;
            if (mw.Set.AutoSaveInterval > 0)
            {
                mw.AutoSaveTimer.Interval = mw.Set.AutoSaveInterval * 60000;
                mw.AutoSaveTimer.Start();
            }
            else
            {
                mw.AutoSaveTimer.Stop();
            }
        }


        private void RBDiagnosisYES_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.Diagnosis = true;
            CBDiagnosis.IsEnabled = true;
        }

        private void RBDiagnosisNO_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.Diagnosis = false;
            CBDiagnosis.IsEnabled = false;
        }

        private void CBDiagnosis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            List<int> cbDiagnosis = new List<int> { 200, 500, 1000, 2000, 5000, 10000, 20000 };
            mw.Set.DiagnosisInterval = cbDiagnosis[CBDiagnosis.SelectedIndex];
        }

        private void ListMod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange || ListMod.SelectedItem == null)
                return;

            ShowMod((string)((ListBoxItem)ListMod.SelectedItem).Content);
        }

        private void ButtonOpenModFolder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(mod.Path.FullName);
        }

        private void ButtonEnable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mw.Set.OnMod(mod.Name);
            ShowMod(mod.Name);
            ButtonRestart.Visibility = Visibility.Visible;
            //int seleid = ListMod.SelectedIndex();
            ShowModList();
        }

        private void ButtonDisEnable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mod.Name.ToLower() == "core")
            {
                MessageBoxX.Show("模组 Core 为<虚拟桌宠模拟器>核心文件,无法停用".Translate(), "停用失败".Translate());
                return;
            }
            mw.Set.OnModRemove(mod.Name);
            ShowMod(mod.Name);
            ButtonRestart.Visibility = System.Windows.Visibility.Visible;
            ShowModList();
        }
        class ProgressClass : IProgress<float>
        {
            float lastvalue = 0;
            ProgressBar pb;
            public ProgressClass(ProgressBar p) => pb = p;
            public void Report(float value)
            {
                if (lastvalue >= value) return;
                lastvalue = value;
                pb.Dispatcher.Invoke(new Action(() => pb.Value = (int)(lastvalue * 100)));
            }
        }
        private async void ButtonPublish_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var mods = mod;
            if (!mw.IsSteamUser)
            {
                MessageBoxX.Show("请先登录Steam后才能上传文件".Translate(), "上传MOD需要Steam登录".Translate(), MessageBoxIcon.Warning);
                return;
            }
            if (mods.Name.ToLower() == "core")
            {
                MessageBoxX.Show("模组 Core 为<虚拟桌宠模拟器>核心文件,无法发布\n如需发布自定义内容,请复制并更改名称".Translate(), "MOD上传失败".Translate(), MessageBoxIcon.Error);
                return;
            }
            if (mods.Path.FullName.Contains("workshop"))
            {
                MessageBoxX.Show("创意工坊物品无法进行上传,请移动到mod文件夹后重试".Translate(), "MOD上传失败".Translate(), MessageBoxIcon.Error);
                return;
            }
            if (!File.Exists(mods.Path.FullName + @"\icon.png") || new FileInfo(mods.Path.FullName + @"\icon.png").Length > 524288)
            {
                MessageBoxX.Show("封面图片(icon.png)大于500kb,请修改后重试".Translate(), "MOD上传失败".Translate(), MessageBoxIcon.Error);
                return;
            }
#if DEMO
            MessageBoxX.Show("经测试,除正式版均无创意工坊权限,此功能仅作为展示", "特殊版无法上传创意工坊");
#endif
            ButtonPublish.IsEnabled = false;
            ButtonPublish.Text = "正在上传";
            ProgressBarUpload.Visibility = Visibility.Visible;
            ProgressBarUpload.Value = 0;
            if (mods.ItemID == 0)
            {
                var result = Editor.NewCommunityFile
                        .WithTitle(mods.Name)
                        .WithDescription(mods.Intro)
                        .WithPublicVisibility()
                        .WithPreviewFile(mods.Path.FullName + @"\icon.png")
                        .WithContent(mods.Path.FullName);
                foreach (string tag in mods.Tag)
                    result = result.WithTag(tag);
                var r = await result.SubmitAsync(new ProgressClass(ProgressBarUpload));
                mods.AuthorID = Steamworks.SteamClient.SteamId.AccountId;
                mods.WriteFile();
                if (r.Success)
                {
                    mods.ItemID = r.FileId.Value;
                    mods.WriteFile();
                    //ProgressBarUpload.Value = 0;
                    //await result.SubmitAsync(new ProgressClass(ProgressBarUpload));
                    if (MessageBoxX.Show("{0} 成功上传至WorkShop服务器\n是否跳转至创意工坊页面进行编辑详细介绍和图标?".Translate(mods.Name), "MOD上传成功".Translate(), MessageBoxButton.YesNo, MessageBoxIcon.Success) == MessageBoxResult.Yes)
                    {
                        ExtensionSetting.StartURL("https://steamcommunity.com/sharedfiles/filedetails/?id=" + r.FileId);
                    }
                }
                else
                {
                    mods.AuthorID = 0; mods.WriteFile();
                    MessageBoxX.Show("{0} 上传至WorkShop服务器失败\n请检查网络后重试\n请注意:上传和下载工坊物品可能需要良好的网络条件\n失败原因:{1}"
                        .Translate(mods.Name, r.Result), "MOD上传失败 {0}".Translate(r.Result));
                }
            }
            else if (mods.AuthorID == Steamworks.SteamClient.SteamId.AccountId)
            {
                var item = await Item.GetAsync(mod.ItemID);
                Editor result;
                if (item == null)
                {
                    result = new Editor(new Steamworks.Data.PublishedFileId() { Value = mods.ItemID })
                        .WithTitle(mods.Name)
                        .WithDescription(mods.Intro)
                        .WithPreviewFile(mods.Path.FullName + @"\icon.png")
                        .WithContent(mods.Path);
                }
                else
                {
                    result = new Editor(new Steamworks.Data.PublishedFileId() { Value = mods.ItemID })
                        .WithTitle(item.Value.Title)
                        .WithDescription(item.Value.Description)
                        .WithPreviewFile(mods.Path.FullName + @"\icon.png")
                        .WithContent(mods.Path);
                }

                foreach (string tag in mods.Tag)
                    result = result.WithTag(tag);
                var r = await result.SubmitAsync(new ProgressClass(ProgressBarUpload));
                if (r.Success)
                {
                    mods.AuthorID = Steamworks.SteamClient.SteamId.AccountId;
                    mods.ItemID = r.FileId.Value;
                    mods.WriteFile();
                    if (MessageBoxX.Show("{0} 成功上传至WorkShop服务器\n是否跳转至创意工坊页面进行编辑新内容?".Translate(mods.Name)
                        , "MOD更新成功".Translate(), MessageBoxButton.YesNo, MessageBoxIcon.Success) == MessageBoxResult.Yes)
                        ExtensionSetting.StartURL("https://steamcommunity.com/sharedfiles/filedetails/?id=" + r.FileId);
                }
                else
                    MessageBoxX.Show("{0} 上传至WorkShop服务器失败\n请检查网络后重试\n请注意:上传和下载工坊物品可能需要良好的网络条件\n失败原因:{1}"
                        .Translate(mods.Name, r.Result), "MOD上传失败 {0}".Translate(r.Result));
            }
            ButtonPublish.IsEnabled = true;
            ButtonPublish.Text = "任务完成".Translate();
            ProgressBarUpload.Visibility = Visibility.Collapsed;
        }

        private void ButtonSteam_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!AllowChange)
                return;
            ExtensionSetting.StartURL("https://steamcommunity.com/sharedfiles/filedetails/?id=" + mod.ItemID);
        }

        private void ButtonAllow_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxX.Show("是否启用 {0} 的代码插件?\n一经启用,该插件将会允许访问该系统(包括外部系统)的所有数据\n如果您不确定,请先使用杀毒软件查杀检查".Translate(mod.Name),
                "启用 {0} 的代码插件?".Translate(mod.Name), MessageBoxButton.YesNo, MessageBoxIcon.Warning) == MessageBoxResult.Yes)
            {
                mw.Set.PassMod(mod.Name);
                ShowMod(mod.Name);
                ButtonRestart.Visibility = Visibility.Visible;
            }
        }

        private void ButtonRestart_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxX.Show("是否退出游戏<虚拟桌宠模拟器>?\n请注意保存游戏".Translate(), "重启游戏".Translate(), MessageBoxButton.YesNo, MessageBoxIcon.Warning) == MessageBoxResult.Yes)
            {
                mw.Restart();
            }
        }

        public void UpdateMoveAreaText()
        {
            var mwCtrl = mw.Core.Controller as MWController;
            if (mwCtrl.IsPrimaryScreen)
            {
                textMoveArea.Text = "主屏幕".Translate();
                return;
            }
            var rect = mwCtrl.ScreenBorder;
            textMoveArea.Text = $"X:{rect.X};Y:{rect.Y};W:{rect.Width};H:{rect.Height}";
        }

        private void BtnSetMoveArea_Default_Click(object sender, RoutedEventArgs e)
        {
            var mwCtrl = mw.Core.Controller as MWController;
            mwCtrl.ResetScreenBorder();
            UpdateMoveAreaText();
        }

        private void BtnSetMoveArea_DetectScreen_Click(object sender, RoutedEventArgs e)
        {
            var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(mw);
            var currentScreen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);
            var mwCtrl = mw.Core.Controller as MWController;
            mwCtrl.ScreenBorder = currentScreen.Bounds;
            UpdateMoveAreaText();
        }

        internal static System.Reflection.FieldInfo leftGetter, topGetter;
        private void BtnSetMoveArea_Window_Click(object sender, RoutedEventArgs e)
        {
            var wma = new winMoveArea(mw);
            var mwCtrl = mw.Core.Controller as MWController;
            if (!mwCtrl.IsPrimaryScreen)
            {
                var rect = mwCtrl.ScreenBorder;
                wma.Width = rect.Width;
                wma.Height = rect.Height;
                wma.Left = rect.X;
                wma.Top = rect.Y;
            }
            wma.ShowDialog();
        }

        private void hyper_moreInfo(object sender, RoutedEventArgs e)
        {
            ExtensionSetting.StartURL("https://www.exlb.net/Diagnosis");
        }

        public new void Show()
        {
            if (MainTab.SelectedIndex == 2)
            {
                voicetimer.Start();
            }
            base.Show();
        }

        private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mw.Topmost = mw.Set.TopMost;
            e.Cancel = mw.CloseConfirm;
            voicetimer.Stop();
            Hide();
        }

        private void TopMostBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.TopMost = true;
        }

        private void TopMostBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.TopMost = false;
        }

        private void ZoomSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.SetZoomLevel(ZoomSlider.Value / 2);
            //this.Width = 400 * Math.Sqrt(ZoomSlider.Value);
            //this.Height = 450 * Math.Sqrt(ZoomSlider.Value);
        }

        private void PressLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.PressLength = (int)(PressLengthSlider.Value * 1000);
        }

        private void InteractionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.InteractionCycle = (int)(InteractionSlider.Value);
            CalTimeInteraction();
        }
        private void CalTimeInteraction()
        {
            var interact = (60 / mw.Set.LogicInterval);
            rTimeMinute.Text = interact.ToString("f2");
            RInter.Text = ((0.08 * mw.Set.InteractionCycle - 0.9) / interact * 2).ToString("f2");
        }
        #region Link
        private void Git_Click(object sender, RoutedEventArgs e)
        {
            ExtensionSetting.StartURL("https://github.com/LorisYounger/VPet/graphs/contributors");
        }

        private void Steam_Click(object sender, RoutedEventArgs e)
        {
            ExtensionSetting.StartURL("https://store.steampowered.com/app/1920960/_/");
        }

        private void Github_Click(object sender, RoutedEventArgs e)
        {
            ExtensionSetting.StartURL("https://github.com/LorisYounger/VPet");
        }

        private void LB_Click(object sender, RoutedEventArgs e)
        {
            ExtensionSetting.StartURL("https://space.bilibili.com/609610777");
        }

        private void VPET_Click(object sender, RoutedEventArgs e)
        {
            ExtensionSetting.StartURL("https://www.exlb.net/");
        }
        private void VUP_Click(object sender, RoutedEventArgs e)
        {
            ExtensionSetting.StartURL("https://store.steampowered.com/app/1352140/_/");
        }

        private void Group_Click(object sender, RoutedEventArgs e)
        {
            if (LocalizeCore.CurrentCulture.StartsWith("zh"))
                ExtensionSetting.StartURL("https://space.bilibili.com/690425399");
            else
                ExtensionSetting.StartURL("https://github.com/LorisYounger/VPet");
        }
        private void sendkey_click(object sender, RoutedEventArgs e)
        {
            if (LocalizeCore.CurrentCulture.StartsWith("zh"))
                ExtensionSetting.StartURL("https://www.exlb.net/SendKeys");
            else if (LocalizeCore.CurrentCulture == "null")
                ExtensionSetting.StartURL("https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks");
            else
                ExtensionSetting.StartURL($"https://learn.microsoft.com/{LocalizeCore.CurrentCulture}/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks");
        }
        #endregion        

        private void CalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.LogicInterval = CalSlider.Value;
            mw.Main.SetLogicInterval((int)(CalSlider.Value * 1000));
            CalTimeInteraction();
        }

        private void MoveEventBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.AllowMove = MoveEventBox.IsChecked == true;
            SetSmartMove();
        }

        private void SmartMoveEventBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.SmartMove = SmartMoveEventBox.IsChecked == true;
            SetSmartMove();
        }

        private void CBSmartMove_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.SmartMoveInterval = (int)((ComboBoxItem)CBSmartMove.SelectedItem).Tag;
            SetSmartMove();
        }
        public void SetSmartMove()
        {
            if (!AllowChange)
                return;
            mw.Main.SetMoveMode(mw.Set.AllowMove, mw.Set.SmartMove, mw.Set.SmartMoveInterval * 1000);
        }
        public void GenStartUP()
        {
            mw.Set["v"][(gbol)"newverstartup"] = true;
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\VPET_Simulator.lnk";
            if (mw.Set.StartUPBoot)
            {
                if (File.Exists(path))
                    File.Delete(path);
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(path);
                if (mw.Set.StartUPBootSteam)
                {
                    shortcut.TargetPath = ExtensionValue.BaseDirectory + @"\VPet.Solution.exe";
                    shortcut.Arguments = "launchsteam";
                }
                else
                    shortcut.TargetPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                shortcut.Description = "VPet Simulator";
                shortcut.WorkingDirectory = ExtensionValue.BaseDirectory;
                shortcut.IconLocation = ExtensionValue.BaseDirectory + @"vpeticon.ico";
                try
                {
                    shortcut.Save();
                }
                catch
                {
                    MessageBox.Show("创建快捷方式失败,权限不足\n请以管理员身份运行后重试".Translate(), "权限不足".Translate());
                }
            }
            else
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
        private void StartUpBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (StartUpBox.IsChecked == true)
                if (MessageBoxX.Show("该游戏随着开机启动该程序\r如需卸载游戏\r请关闭该选项".Translate() + "\n------\n" + "我已确认,并在卸载游戏前会关闭该功能".Translate(), "开机启动重要消息".Translate(),
                    MessageBoxButton.YesNo, MessageBoxIcon.Warning) != MessageBoxResult.Yes)
                    return;
            //else
            //{
            //    mw.Set["SingleTips"][(gint)"open"] = 1;
            //    MessageBoxX.Show("游戏开机启动的实现方式是创建快捷方式,不是注册表,更健康,所以游戏卸了也不知道\n如果游戏打不开,可以去这里手动删除游戏开机启动快捷方式:\n%appdata%\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\".Translate()
            //        , "关于卸载不掉的问题是因为开启了开机启动".Translate(), MessageBoxIcon.Info);
            //}

            mw.Set.StartUPBoot = StartUpBox.IsChecked == true;
            GenStartUP();
        }

        private void StartUpSteamBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.StartUPBootSteam = StartUpSteamBox.IsChecked == true;
            GenStartUP();
        }

        private void PetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;

            var petloader = mw.Pets.Find(x => x.Name == mw.Set.PetGraph);
            petloader ??= mw.Pets[0];
            bool ischangename = mw.Core.Save.Name == petloader.PetName.Translate();

            mw.Set.PetGraph = mw.Pets[PetBox.SelectedIndex].Name;
            PetIntor.Text = mw.Pets[PetBox.SelectedIndex].Intor.Translate();
            ButtonRestartGraph.Visibility = Visibility.Visible;

            if (ischangename)
            {
                mw.Core.Save.Name = mw.Pets[PetBox.SelectedIndex].PetName.Translate();
                TextBoxPetName.Text = mw.Core.Save.Name;
                if (mw.IsSteamUser)
                    SteamFriends.SetRichPresence("username", mw.Core.Save.Name);
            }
        }

        private void TextBoxPetName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Core.Save.Name = TextBoxPetName.Text;
            if (mw.IsSteamUser)
                SteamFriends.SetRichPresence("username", mw.Core.Save.Name);
        }

        private void DIY_ADD_Click(object sender, RoutedEventArgs e)
        {
            StackDIY.Children.Add(new DIYViewer());
        }

        private void DIY_Save_Click(object sender, RoutedEventArgs e)
        {
            mw.Set["diy"].Clear();
            foreach (DIYViewer dv in StackDIY.Children)
            {
                mw.Set["diy"].Add(dv.ToSub());
            }
            mw.LoadDIY();
        }


        private void ChatGPT_Reset_Click(object sender, RoutedEventArgs e)
        {
            switch (mw.Set["CGPT"][(gstr)"type"])
            {
                //case "API":
                //    new winCGPTSetting(mw).ShowDialog();
                //    break;
                case "DIY":
                    if (mw.TalkBoxCurr != null)
                        mw.TalkBoxCurr.Setting();
                    else
                        ExtensionSetting.StartURL("https://steamcommunity.com/app/1920960/workshop/");
                    break;
                case "LB":
                    //Task.Run(() =>
                    //{
                    //    if (((TalkBox)mw.TalkBox).ChatGPT_Reset())
                    //    {
                    //        ((TalkBox)mw.TalkBox).btn_startup.Visibility = Visibility.Visible;
                    //        MessageBoxX.Show("桌宠重置成功".Translate());
                    //    }
                    //});
                    //((TalkSelect)mw.TalkBox).RelsTime
                    break;
                case "OFF":
                default:
                    break;
            }
        }

        private void CGPType_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (RBCGPTUseLB.IsChecked == true)
            {
                mw.Set["CGPT"][(gstr)"type"] = "LB";
            }
            else if (RBCGPTDIY.IsChecked == true)
            {
                mw.Set["CGPT"][(gstr)"type"] = "DIY";
            }
            //else if (RBCGPTUseAPI.IsChecked == true)
            //{
            //    mw.Set["CGPT"][(gstr)"type"] = "API";
            //}
            else
            {
                mw.Set["CGPT"][(gstr)"type"] = "OFF";
            }


            switch (mw.Set["CGPT"][(gstr)"type"])
            {
                //case "API":
                //    BtnCGPTReSet.IsEnabled = true;
                //    BtnCGPTReSet.Content = "打开 ChatGPT API 设置".Translate();
                //    if (mw.TalkBox != null)
                //        mw.Main.ToolBar.MainGrid.Children.Remove(mw.TalkBox);
                //    mw.TalkBox = new TalkBoxAPI(mw);
                //    mw.Main.ToolBar.MainGrid.Children.Add(mw.TalkBox);
                //    break;
                case "DIY":
                    BtnCGPTReSet.IsEnabled = true;
                    mw.RemoveTalkBox();
                    BtnCGPTReSet.Content = "打开 {0} 设置".Translate(mw.TalkBoxCurr?.APIName ?? "Steam Workshop");
                    mw.LoadTalkDIY();
                    break;
                case "LB":
                    mw.RemoveTalkBox();
                    BtnCGPTReSet.IsEnabled = true;
                    BtnCGPTReSet.Content = "初始化桌宠聊天程序".Translate();
                    mw.TalkBox = new TalkSelect(mw);
                    mw.Main.ToolBar.MainGrid.Children.Add(mw.TalkBox);
                    break;
                case "OFF":
                default:
                    mw.RemoveTalkBox();
                    BtnCGPTReSet.IsEnabled = false;
                    BtnCGPTReSet.Content = "聊天框已关闭".Translate();
                    break;
            }
        }

        private void ButtonSetting_MouseDown(object sender, MouseButtonEventArgs e)
        {
            foreach (var mainplug in mw.Plugins)
            {
                try
                {
                    if (mainplug.PluginName == mod.Name && mainplug.GetType().GetMethod("Setting").DeclaringType != typeof(MainPlugin)
                    && mainplug.GetType().Assembly.Location.Contains(mod.Path.FullName))
                    {
                        mainplug.Setting();
                        return;
                    }
                }
                finally { }
            }
        }

        private void StartPlace_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (StartPlace.IsChecked == true)
            {
                mw.Set.StartRecordLast = true;
                TextBoxStartUpX.IsEnabled = false;
                TextBoxStartUpY.IsEnabled = false;
                BtnStartUpGet.IsEnabled = false;
            }
            else
            {
                mw.Set.StartRecordLast = false;
                TextBoxStartUpX.IsEnabled = true;
                TextBoxStartUpY.IsEnabled = true;
                BtnStartUpGet.IsEnabled = true;
            }
        }

        private void TextBoxStartUp_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (double.TryParse(TextBoxStartUpX.Text, out double x) && double.TryParse(TextBoxStartUpY.Text, out double y))
                mw.Set.StartRecordPoint = new Point(x, y);
        }

        private void BtnStartUpGet_Click(object sender, RoutedEventArgs e)
        {
            AllowChange = false;
            TextBoxStartUpX.Text = mw.Left.ToString();
            TextBoxStartUpY.Text = mw.Top.ToString();
            mw.Set.StartRecordPoint = new Point(mw.Left, mw.Top);
            AllowChange = true;
        }

        private void CalFunctionBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            //MessageBoxX.Show("由于没做完,暂不支持数据计算\n敬请期待后续更新", "没做完!", MessageBoxButton.OK, MessageBoxIcon.Warning);
            if (CalFunctionBox.IsChecked == true)
            {
                mw.Set.EnableFunction = true;
                combCalFunState.IsEnabled = false;
            }
            else
            {
                mw.Set.EnableFunction = false;
                combCalFunState.IsEnabled = true;
            }
        }

        private void SwitchMsgOut_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.MessageBarOutside = SwitchMsgOut.IsChecked.Value;
            if (SwitchMsgOut.IsChecked.Value)
                mw.Main.MsgBar.SetPlaceOUT();
            else
                mw.Main.MsgBar.SetPlaceIN();
        }

        private void numBackupSaveMaxNum_ValueChanged(object sender, Panuon.WPF.SelectedValueChangedRoutedEventArgs<double?> e)
        {
            if (!AllowChange)
                return;
            mw.Set.BackupSaveMaxNum = (int)numBackupSaveMaxNum.Value;
        }
        int reloadid = 0;
        private void CBSaveReLoad_MouseEnter(object sender, MouseEventArgs e)
        {
            if (reloadid != mw.Set.SaveTimes)
            {
                reloadid = mw.Set.SaveTimes;
                CBSaveReLoad.SelectedItem = null;
                CBSaveReLoad.Items.Clear();
                if (Directory.Exists(ExtensionValue.BaseDirectory + @"\BackUP"))
                {
                    foreach (var file in new DirectoryInfo(ExtensionValue.BaseDirectory + @"\BackUP")
                        .GetFiles().OrderByDescending(x => x.LastWriteTime))
                    {
                        if (file.Extension.ToLower() == ".lps")
                        {
                            CBSaveReLoad.Items.Add(file.Name.Split('.').First());
                        }
                    }
                    CBSaveReLoad.SelectedIndex = 0;
                }
            }
        }

        private void BtnSaveReload_Click(object sender, RoutedEventArgs e)
        {
            if (CBSaveReLoad.SelectedItem != null)
            {
                string txt = (string)CBSaveReLoad.SelectedItem;
                string path = ExtensionValue.BaseDirectory + @"\BackUP\" + txt + ".lps";
                if (File.Exists(path))
                {
                    try
                    {
                        var l = new LPS(File.ReadAllText(path));
                        GameSave_v2 gs = new GameSave_v2(l);
                        if (MessageBoxX.Show("存档名称:{0}\n存档等级:{1}\n存档金钱:{2}\nHashCheck:{3}\n是否加载该备份存档? 当前游戏数据会丢失"
                            .Translate(gs.GameSave.Name, gs.GameSave.Level, gs.GameSave.Money, gs.HashCheck), "是否加载该备份存档? 当前游戏数据会丢失".Translate(), MessageBoxButton.YesNo, MessageBoxIcon.Info) == MessageBoxResult.Yes)
                        {
                            try
                            {
                                if (!mw.GameLoad(l))
                                    MessageBoxX.Show("存档损毁,无法加载该存档\n可能是上次储存出错或Steam云同步导致的\n请在设置中加载备份还原存档", "存档损毁".Translate());
                            }
                            catch (Exception ex)
                            {
                                MessageBoxX.Show("存档损毁,无法加载该存档\n可能是数据溢出/超模导致的" + '\n' + ex.Message, "存档损毁".Translate());
                            }
                        }
                    }
                    catch (Exception exp)
                    {
                        MessageBoxX.Show("存档损毁,无法加载该备份\n请更换备份重试".Translate() + '\n' + exp.ToString(), "存档损毁".Translate());
                    }
                }
            }
        }

        private void Mod_Click(object sender, RoutedEventArgs e)
        {
            List<string> list = new List<string>();
            foreach (CoreMOD mod in mw.CoreMODs)
            {
                foreach (string str in mod.Author.Split(','))
                    list.Add(str.Trim());
            }
            list = list.Distinct().ToList();
            MessageBoxX.Show(string.Join("\n", list), "感谢以下MOD开发人员".Translate());
        }

        private void Using_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxX.Show(string.Join("\n", CoreMOD.LoadedDLL), "DLL引用名单".Translate());
        }

        private void combCalFunState_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.CalFunState = (GameSave.ModeType)combCalFunState.SelectedIndex;
            mw.Main.NoFunctionMOD = (GameSave.ModeType)combCalFunState.SelectedIndex;
            mw.Main.EventTimer_Elapsed();
        }

        private void HitThroughBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set["v"][(gbol)"HitThrough"] = true;
            mw.Set.HitThrough = HitThroughBox.IsChecked.Value;
            if (HitThroughBox.IsChecked.Value != mw.HitThrough)
                mw.SetTransparentHitThrough();
            if (HitThroughBox.IsChecked.Value)
                PetHelperBox.IsChecked = true;
        }

        private void PetHelperBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (PetHelperBox.IsChecked == true)
            {
                mw.Set.PetHelper = true;
                mw.LoadPetHelper();
            }
            else
            {
                mw.Set.PetHelper = false;
                mw.petHelper?.Close();
                mw.petHelper = null;
            }
        }

        private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            var petloader = mw.Pets.Find(x => x.Name == mw.Set.PetGraph);
            petloader ??= mw.Pets[0];
            bool ischangename = mw.Core.Save.Name == petloader.PetName.Translate();
            LocalizeCore.LoadCulture((string)LanguageBox.SelectedItem);
            mw.Set.Language = LocalizeCore.CurrentCulture;
            if (ischangename)
            {
                mw.Core.Save.Name = petloader.PetName.Translate();
                TextBoxPetName.Text = mw.Core.Save.Name;
                if (mw.IsSteamUser)
                    SteamFriends.SetRichPresence("username", mw.Core.Save.Name);
            }
        }

        private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            voicetimer.Stop();
            switch (MainTab.SelectedIndex)
            {
                case 2://启动音量探测
                    voicetimer.Start();
                    break;
                case 4:
                    if (mw.HashCheck)
                    {
                        RHashCheck.Text = "通过".Translate();
                    }
                    else
                    {
                        RHashCheck.Text = "失败".Translate();
                    }
                    break;
            }
        }
        DispatcherTimer voicetimer;

        private void VoiceCatchSilder_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.MusicCatch = VoiceCatchSilder.Value;
            mw.Set.MusicMax = VoiceMaxSilder.Value;
        }

        private void cleancache_click(object sender, RoutedEventArgs e)
        {
            mw.Set.LastCacheDate = DateTime.MinValue;
            MessageBoxX.Show("清理指令已下达,下次启动桌宠时生效".Translate());
        }

        private void SliderResolution_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mw.Set.Resolution = (int)SliderResolution.Value;
            ButtonRestartGraph.Visibility = Visibility.Visible;
        }

        private void save_click(object sender, RoutedEventArgs e)
        {
            mw.Save();
            MessageBoxX.Show("保存成功".Translate());
        }

        private void swAutoCal_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set["gameconfig"].SetBool("noAutoCal", !swAutoCal.IsChecked.Value);
        }

        private void restart_click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxX.Show("是否重置游戏数据重新开始?\n该操作无法撤回".Translate(), "重新开始".Translate(), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                mw.GameSavesData = new GameSave_v2(mw.Core.Save.Name);
                mw.Core.Save = mw.GameSavesData.GameSave;
                if (!mw.HashCheck)
                    mw.GameSavesData.Statistics = new Statistics();
                mw.HashCheck = true;
                CBSaveReLoad.IsEnabled = false;
                BtnSaveReload.IsEnabled = false;
                MessageBoxX.Show("重置成功".Translate());
            }
        }

        private void cbChatAPISelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.TalkAPIIndex = cbChatAPISelect.SelectedIndex;
            mw.Set["CGPT"][(gstr)"DIY"] = mw.TalkBoxCurr?.APIName ?? "";
            if (RBCGPTDIY.IsChecked == true)
                mw.LoadTalkDIY();
            BtnCGPTReSet.Content = "打开 {0} 设置".Translate(mw.TalkBoxCurr?.APIName ?? "Steam Workshop");

        }

        private void SwitchHideFromTaskControl_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.HideFromTaskControl = SwitchHideFromTaskControl.IsChecked == true;
            ButtonRestartGraph.Visibility = Visibility.Visible;
        }
    }
}
