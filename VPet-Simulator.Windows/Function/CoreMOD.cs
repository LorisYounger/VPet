using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    public class CoreMOD
    {
        public static List<string> LoadedDLL { get; } = new List<string>()
        {
            "ChatGPT.API.Framework.dll","Panuon.WPF.dll","steam_api.dll","Panuon.WPF.UI.dll","steam_api64.dll",
            "LinePutScript.dll","Newtonsoft.Json.dll","Facepunch.Steamworks.Win32.dll", "Facepunch.Steamworks.Win64.dll",
            "VPet-Simulator.Core.dll","VPet-Simulator.Windows.Interface.dll"
        };
        public static string NowLoading = null;
        public string Name;
        public string Author;
        /// <summary>
        /// 如果是上传至Steam,则为SteamUserID
        /// </summary>
        public long AuthorID;
        /// <summary>
        /// 上传至Steam的ItemID
        /// </summary>
        public ulong ItemID;
        public string Intro;
        public DirectoryInfo Path;
        public int GameVer;
        public int Ver;
        public string Content = "";
        public bool SuccessLoad = true;
        /// <summary>
        /// LBGame 信任的MOD,自动加载
        /// </summary>
        public bool IsTrust = false;
        public static string INTtoVER(int ver) => $"{ver / 100}.{ver % 100:00}";
        public static void LoadImage(MainWindow mw, DirectoryInfo di)
        {
            //加载其他放在文件夹的图片
            foreach (FileInfo fi in di.EnumerateFiles("*.png"))
            {
                mw.ImageSources.AddSource(fi.Name.ToLower().Substring(0, fi.Name.Length - 4), fi.FullName);
            }
            //加载其他放在文件夹中文件夹的图片
            foreach (DirectoryInfo fordi in di.EnumerateDirectories())
            {
                LoadImage(mw, fordi);
            }
            //加载标志好的图片和图片设置
            foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
            {
                var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
                if (fi.Name.ToLower().StartsWith("set_"))
                    foreach (var line in tmp)
                        mw.ImageSources.ImageSetting.AddorReplaceLine(line);
                else
                    mw.ImageSources.AddImages(tmp, di.FullName);
            }
        }
        public CoreMOD(DirectoryInfo directory, MainWindow mw)
        {
            Path = directory;
            LpsDocument modlps = new LpsDocument(File.ReadAllText(directory.FullName + @"\info.lps"));
            Name = modlps.FindLine("vupmod").Info;
            NowLoading = Name;
            Intro = modlps.FindLine("intro").Info;
            GameVer = modlps.FindSub("gamever").InfoToInt;
            Ver = modlps.FindSub("ver").InfoToInt;
            Author = modlps.FindSub("author").Info.Split('[').First();
            if (modlps.FindLine("authorid") != null)
                AuthorID = modlps.FindLine("authorid").InfoToInt64;
            else
                AuthorID = 0;
            if (modlps.FindLine("itemid") != null)
                ItemID = Convert.ToUInt64(modlps.FindLine("itemid").info);
            else
                ItemID = 0;
            if (IsBanMOD(mw))
            {
                Content = "该模组已停用";
                return;
            }

            foreach (DirectoryInfo di in Path.EnumerateDirectories())
            {
                switch (di.Name.ToLower())
                {
                    case "pet":
                        //宠物模型
                        Content += "宠物形象\n";
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            LpsDocument lps = new LpsDocument(File.ReadAllText(fi.FullName));
                            if (lps.First().Name.ToLower() == "pet")
                            {
                                var name = lps.First().Info;
                                var p = mw.Pets.FirstOrDefault(x => x.Name == name);
                                if (p == null)
                                    mw.Pets.Add(new PetLoader(lps, di));
                                else
                                {
                                    p.path.Add(di.FullName + "\\" + lps.First()["path"].Info);
                                    p.Config.Set(lps);
                                }
                            }
                        }
                        break;
                    case "food":
                        Content += "食物\n";
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
                            foreach (ILine li in tmp)
                            {
                                string tmps = li.Find("name").info;
                                mw.Foods.RemoveAll(x => x.Name == tmps);
                                mw.Foods.Add(LPSConvert.DeserializeObject<Food>(li));
                            }
                        }
                        break;
                    case "image":
                        Content += "图片包\n";
                        LoadImage(mw, di);
                        break;
                    case "plugin":
                        Content += "代码插件\n";
                        SuccessLoad = false;
                        foreach (FileInfo tmpfi in di.EnumerateFiles("*.dll"))
                        {
                            try
                            {
                                var path = tmpfi.Name;
                                if (LoadedDLL.Contains(path))
                                    continue;
                                LoadedDLL.Add(path);
                                Assembly dll = Assembly.LoadFrom(tmpfi.FullName);
                                var certificate = dll.GetModules()?.First()?.GetSignerCertificate();
                                if (certificate != null && certificate.Subject == "")
                                {//LBGame 信任的证书

                                }
                                else
                                {
                                    if (!IsPassMOD(mw))
                                    {//不是通过模组,不加载
                                        continue;
                                    }
                                }
                                var v = dll.GetExportedTypes();
                                foreach (Type exportedType in v)
                                {
                                    if (exportedType.BaseType == typeof(MainPlugin))
                                    {
                                        mw.Plugins.Add((MainPlugin)Activator.CreateInstance(exportedType, mw));
                                        SuccessLoad = true;
                                    }
                                }
                            }
                            catch
                            {

                            }
                        }
                        break;
                }
            }
        }
        public bool IsBanMOD(MainWindow mw) => mw.Set.IsBanMod(Name);
        public bool IsPassMOD(MainWindow mw) => mw.Set.IsPassMOD(Name);

        public void WriteFile()
        {
            LpsDocument modlps = new LpsDocument(File.ReadAllText(Path.FullName + @"\info.lps"));
            modlps.FindLine("vupmod").Info = Name;
            modlps.FindLine("intro").Info = Intro;
            modlps.FindSub("gamever").InfoToInt = GameVer;
            modlps.FindSub("ver").InfoToInt = Ver;
            modlps.FindSub("author").Info = Author;
            modlps.FindorAddLine("authorid").InfoToInt64 = AuthorID;
            modlps.FindorAddLine("itemid").info = ItemID.ToString();
            File.WriteAllText(Path.FullName + @"\info.lps", modlps.ToString());
        }
    }
    public static class ExtensionSetting
    {
        public static bool IsBanMod(this Setting t, string ModName)
        {
            var line = t.FindLine("banmod");
            if (line == null)
                return false;
            return line.Find(ModName.ToLower()) != null;
        }
        public static bool IsPassMOD(this Setting t, string ModName)
        {
            var line = t.FindLine("passmod");
            if (line == null)
                return false;
            return line.Find(ModName.ToLower()) != null;
        }
        public static bool IsMSGMOD(this Setting t, string ModName)
        {
            var line = t.FindorAddLine("msgmod");
            if (line.GetBool(ModName))
                return false;
            line.SetBool(ModName, true);
            return true;
        }
        public static void BanMod(this Setting t, string ModName)
        {
            if (string.IsNullOrWhiteSpace(ModName))
                return;
            t.FindorAddLine("banmod").AddorReplaceSub(new Sub(ModName.ToLower()));
        }
        public static void BanModRemove(this Setting t, string ModName)
        {
            t.FindorAddLine("banmod").Remove(ModName.ToLower());
        }
        public static void PassMod(this Setting t, string ModName)
        {
            t.FindorAddLine("passmod").AddorReplaceSub(new Sub(ModName.ToLower()));
        }
        public static void PassModRemove(this Setting t, string ModName)
        {
            t.FindorAddLine("passmod").Remove(ModName.ToLower());
        }
    }
}
