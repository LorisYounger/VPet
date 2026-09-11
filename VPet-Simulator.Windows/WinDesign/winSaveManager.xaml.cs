using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using VPet_Simulator.Core;
using VPet_Simulator.Unified.Services;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winSaveManager.xaml 的交互逻辑
    /// </summary>
    public partial class winSaveManager : WindowX
    {
        private readonly MainWindow mw;
        private List<SaveEntry> allEntries = new List<SaveEntry>();

        public winSaveManager(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            Owner = mw;
            Title = "存档管理器".Translate() + ' ' + mw.PrefixSave;
            DPStart.SelectedDateTime = DateTime.Now.AddYears(-1);
            RefreshSaveList();

        }

        private sealed class SaveEntry
        {
            public enum SourceType
            {
                Local,
                Backup,
                Steam
            }
            public SourceType Source { get; init; }
            public string SourceText => Source switch
            {
                SourceType.Local => "本地".Translate(),
                SourceType.Backup => "备份".Translate(),
                SourceType.Steam => "Steam云".Translate(),
                _ => string.Empty
            };
            public string SourceToolTip => Source switch
            {
                SourceType.Local => "本地存档, 会被Steam自动同步".Translate(),
                SourceType.Backup => "本地备份文件, 不会被steam云存档同步".Translate(),
                SourceType.Steam => "Steam云备份存档, 跟随Steam账号保存".Translate(),
                _ => string.Empty
            };
            public string SaveId { get; init; } = string.Empty;
            public string PetName { get; init; } = string.Empty;
            public DateTime SaveTime { get; init; } = DateTime.MinValue;
            public string SaveTimeText { get; init; } = string.Empty;
            public string LevelText { get; init; } = string.Empty;
            public string MoneyText { get; init; } = string.Empty;
            public string FullPath { get; init; } = string.Empty;
            public string SteamPath { get; init; } = string.Empty;
            public bool HashCheck { get; init; }
            public string HashCheckText => HashCheck ? "Pass" : "Fail";
        }

        private void RefreshSaveList()
        {
            var list = new List<SaveEntry>();
            foreach (var item in GetLocalSaveEntries())
            {
                list.Add(item);
            }

            foreach (var item in GetSteamSaveEntries())
            {
                list.Add(item);
            }

            allEntries = list.OrderByDescending(x => x.SaveTime).ToList();
            ApplyFilters();
        }

        private void ApplyFilters()
        {            
            IEnumerable<SaveEntry> filtered = allEntries;

            if (CBTypeFilter.SelectedItem is ComboBoxItem cbi)
            {
                var tag = cbi.Tag?.ToString();
                switch (tag)
                {
                    case "local":
                        filtered = filtered.Where(x => x.Source == SaveEntry.SourceType.Local);
                        break;
                    case "localbackup":
                        filtered = filtered.Where(x => x.Source == SaveEntry.SourceType.Backup);
                        break;
                    case "steam":
                        filtered = filtered.Where(x => x.Source == SaveEntry.SourceType.Steam);
                        break;
                }
            }

            if (DPStart.SelectedDateTime.HasValue)
            {
                var start = DPStart.SelectedDateTime.Value.Date;
                filtered = filtered.Where(x => x.SaveTime >= start);
            }

            if (DPEnd.SelectedDateTime.HasValue)
            {
                var endExclusive = DPEnd.SelectedDateTime.Value.Date.AddDays(1);
                filtered = filtered.Where(x => x.SaveTime < endExclusive);
            }

            var result = filtered.OrderByDescending(x => x.SaveTime).ToList();
            DataGridSaves.ItemsSource = result;

            var view = CollectionViewSource.GetDefaultView(DataGridSaves.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(nameof(SaveEntry.SaveTime), ListSortDirection.Descending));
                view.Refresh();
            }

            foreach (var c in DataGridSaves.Columns)
                c.SortDirection = null;
            var saveTimeColumn = DataGridSaves.Columns.FirstOrDefault(c => c.SortMemberPath == nameof(SaveEntry.SaveTime));
            if (saveTimeColumn != null)
                saveTimeColumn.SortDirection = ListSortDirection.Descending;

            if (DataGridSaves.Items.Count > 0)
                DataGridSaves.SelectedIndex = 0;
        }

        private IEnumerable<SaveEntry> GetLocalSaveEntries()
        {
            var entries = new List<SaveEntry>();
            //存档目录和备份目录的枚举、排序都走共享后端
            foreach (var file in SaveCatalog.ListAll(ExtensionValue.BaseDirectory, mw.PrefixSave))
            {
                try
                {
                    var lpsText = File.ReadAllText(file.Path);
                    var gs = new GameSave_v2(new LPS(lpsText));
                    entries.Add(new SaveEntry()
                    {
                        Source = file.IsBackup ? SaveEntry.SourceType.Backup : SaveEntry.SourceType.Local,
                        SaveId = Path.GetFileNameWithoutExtension(file.Path),
                        PetName = gs.GameSave.Name,
                        SaveTime = file.Time,
                        SaveTimeText = file.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                        LevelText = $"{gs.GameSave.Level} (x{gs.GameSave.LevelMax})",
                        MoneyText = gs.GameSave.Money.ToString("f2"),
                        FullPath = file.Path,
                        HashCheck = gs.HashCheck,
                    });
                }
                catch
                {
                }
            }
            return entries;
        }

        private IEnumerable<SaveEntry> GetSteamSaveEntries()
        {
            var entries = new List<SaveEntry>();
            if (!mw.IsSteamUser)
                return entries;

            //筛选与时间解析都走共享后端, 与 MainWindow.Save 用的是同一份规则
            var steamFiles = SaveCatalog.ListCloud(SteamRemoteStorage.Files, mw.PrefixSave);

            foreach (var file in steamFiles)
            {
                string lpsText;
                try
                {
                    var data = SteamRemoteStorage.FileRead(file);
                    if (data == null || data.Length == 0)
                        continue;
                    lpsText = Encoding.UTF8.GetString(data);
                }
                catch
                {
                    continue;
                }

                try
                {
                    var gs = new GameSave_v2(new LPS(lpsText));
                    var saveTime = SaveCatalog.ParseCloudTime(file);
                    entries.Add(new SaveEntry()
                    {
                        Source = SaveEntry.SourceType.Steam,
                        SaveId = Path.GetFileNameWithoutExtension(file),
                        PetName = gs.GameSave.Name,
                        SaveTime = saveTime,
                        SaveTimeText = saveTime == DateTime.MinValue ? "-" : saveTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        LevelText = $"{gs.GameSave.Level} (x{gs.GameSave.LevelMax})",
                        MoneyText = gs.GameSave.Money.ToString("f2"),
                        SteamPath = file,
                        HashCheck = gs.HashCheck,
                    });
                }
                catch
                {
                }
            }
            return entries;
        }

        private void LoadSelectedSave()
        {
            if (DataGridSaves.SelectedItem is not SaveEntry selected)
                return;

            string lpsText;
            if (!string.IsNullOrEmpty(selected.SteamPath))
            {
                try
                {
                    var data = SteamRemoteStorage.FileRead(selected.SteamPath);
                    if (data == null || data.Length == 0)
                    {
                        MessageBoxX.Show("Steam云存档文件不存在,请刷新后重试".Translate(), "加载失败".Translate(), MessageBoxIcon.Warning);
                        return;
                    }
                    lpsText = Encoding.UTF8.GetString(data);
                }
                catch
                {
                    MessageBoxX.Show("读取Steam云存档失败,请稍后重试".Translate(), "加载失败".Translate(), MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                if (!File.Exists(selected.FullPath))
                {
                    MessageBoxX.Show("存档文件不存在,请刷新后重试".Translate(), "加载失败".Translate(), MessageBoxIcon.Warning);
                    return;
                }
                lpsText = File.ReadAllText(selected.FullPath);
            }

            var message = "存档名称:{0}\n保存时间:{1}\n存档等级:{2}\n存档金钱:{3}\nHashCheck:{4}\n是否加载该备份存档? 当前游戏数据会丢失"
                .Translate(selected.PetName, selected.SaveTimeText, selected.LevelText, selected.MoneyText, selected.HashCheck);
            if (MessageBoxX.Show(message, "是否加载该备份存档? 当前游戏数据会丢失".Translate(), MessageBoxButton.YesNo, MessageBoxIcon.Info) != MessageBoxResult.Yes)
                return;

            try
            {
                if (mw.Main.State != Main.WorkingState.Nomal)
                {
                    mw.Main.WorkTimer.Visibility = Visibility.Collapsed;
                    mw.Main.State = Main.WorkingState.Nomal;
                }

                if (!mw.SavesLoad(new LPS(lpsText)))
                    MessageBoxX.Show("存档损毁,无法加载该存档\n可能是上次储存出错或Steam云同步导致的\n请在设置中加载备份还原存档", "存档损毁".Translate());
                else
                    MessageBoxX.Show("加载成功".Translate());
            }
            catch (Exception ex)
            {
                MessageBoxX.Show("存档损毁,无法加载该存档\n可能是数据溢出/超模导致的" + '\n' + ex.Message, "存档损毁".Translate());
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshSaveList();
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadSelectedSave();
        }

        private void Filter_Changed(object sender, Panuon.WPF.SelectedValueChangedRoutedEventArgs<DateTime?> e)
        {
            if (!IsLoaded) return; ApplyFilters();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return; ApplyFilters();
        }
    }
}
