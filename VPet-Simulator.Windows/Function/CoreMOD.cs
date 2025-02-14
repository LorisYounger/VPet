﻿using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    internal class CoreMOD : IModInfo
    {
        /// <summary>
        /// 自动启用MOD名称
        /// </summary>
        public static readonly string[] OnModDefList = new string[] { "Core", "PCat" };

        public static HashSet<string> LoadedDLL { get; } = new HashSet<string>()
        {
            "Panuon.WPF.dll","steam_api.dll","Panuon.WPF.UI.dll","steam_api64.dll",
            "LinePutScript.dll","Facepunch.Steamworks.Win32.dll", "Facepunch.Steamworks.Win64.dll",
            "VPet-Simulator.Core.dll","VPet-Simulator.Windows.Interface.dll","LinePutScript.Localization.WPF.dll",
            "NAudio.Asio.dll", "libSkiaSharp.dll","NAudio.Core.dll","NAudio.dll", "SkiaSharp.dll","NAudio.Midi.dll",
            "NAudio.Wasapi.dll","NAudio.WinForms.dll", "NAudio.WinMM.dll", "WpfAnimatedGif.dll"
        };
        public static Dictionary<string, Type> LoadPlug { get; } = new Dictionary<string, Type>();
        public static string NowLoading = null;
        public string Name { get; set; }
        public string Author { get; set; }
        /// <summary>
        /// 如果是上传至Steam,则为SteamUserID
        /// </summary>
        public long AuthorID { get; set; }
        /// <summary>
        /// 上传至Steam的ItemID
        /// </summary>
        public ulong ItemID { get; set; }
        public string Intro { get; set; }
        public DirectoryInfo Path { get; set; }
        public int GameVer { get; set; }
        public int Ver { get; set; }
        public HashSet<string> Tag { get; set; } = new HashSet<string>();
        public bool SuccessLoad = true;
        public DateTime CacheDate;
        public string ErrorMessage;
        public static string INTtoVER(int ver) => ver < 10000 ? $"{ver / 100}.{ver % 100:00}" : $"{ver / 10000}.{ver % 10000 / 100}.{ver % 100:00}";
        public static void LoadImage(MainWindow mw, DirectoryInfo di, string pre = "")
        {
            //加载其他放在文件夹的图片
            foreach (FileInfo fi in di.EnumerateFiles("*.png"))
            {
                mw.ImageSources.AddSource(pre + fi.Name.ToLower().Substring(0, fi.Name.Length - 4), fi.FullName);
            }
            //加载其他放在文件夹中文件夹的图片
            foreach (DirectoryInfo fordi in di.EnumerateDirectories())
            {
                LoadImage(mw, fordi, pre + fordi.Name + "_");
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
        public static void LoadFile(MainWindow mw, DirectoryInfo di, string pre = "")
        {
            //加载其他放在文件夹的文件
            foreach (FileInfo fi in di.EnumerateFiles())
            {
                mw.FileSources.AddSource(pre + fi.Name, fi.FullName);
            }
            //加载其他放在文件夹中文件夹的文件
            foreach (DirectoryInfo fordi in di.EnumerateDirectories())
            {
                LoadFile(mw, fordi, pre + fordi.Name + "_");
            }
        }
        public CoreMOD(DirectoryInfo directory, MainWindow mw)
        {
#if !DEBUG
            try
            {
#endif
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
            CacheDate = modlps.GetDateTime("cachedate", DateTime.MinValue);
            foreach (var skip in modlps["dllskip"])
            {
                LoadedDLL.Add(skip.Name);
            }
            if (CacheDate > DateTime.Now)
            {//去掉不合理的清理缓存日期
                CacheDate = DateTime.MinValue;
            }

            //MOD未加载时支持翻译
            foreach (var line in modlps.FindAllLine("lang"))
            {
                List<ILine> ls = new List<ILine>();
                foreach (var sub in line)
                {
                    ls.Add(new Line(sub.Name, sub.info));
                }
                LocalizeCore.AddCulture(line.info, ls);
            }

            if (mw.CoreMODs.FirstOrDefault(x => x.Name == Name) != null)
            {
                Name += $"({"MOD名称重复".Translate()})";
                ErrorMessage = "MOD名称重复".Translate();
                return;
            }

            if (!IsOnMOD(mw))
            {
                Tag.Add("该模组已停用");
                foreach (DirectoryInfo di in Path.EnumerateDirectories())
                    Tag.Add(di.Name.ToLower());
                return;
            }

            foreach (DirectoryInfo di in Path.EnumerateDirectories())
            {
                switch (di.Name.ToLower())
                {
                    case "theme":
                        Tag.Add("theme");
                        if (Directory.Exists(di.FullName + @"\fonts"))
                            foreach (var str in Directory.EnumerateFiles(di.FullName + @"\fonts", "*.ttf"))
                            {
                                mw.Fonts.Add(new IFont(new FileInfo(str)));
                            }

                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new Theme(new LpsDocument(File.ReadAllText(fi.FullName)));
                            var oldtheme = mw.Themes.Find(x => x.xName == tmp.xName);
                            if (oldtheme != null)
                                mw.Themes.Remove(oldtheme);
                            mw.Themes.Add(tmp);
                            //加载图片包
                            DirectoryInfo tmpdi = new DirectoryInfo(di.FullName + '\\' + tmp.Image);
                            if (tmpdi.Exists)
                            {
                                foreach (FileInfo tmpfi in tmpdi.EnumerateFiles("*.png"))
                                {
                                    tmp.Images.AddSource(tmpfi.Name.ToLower().Substring(0, tmpfi.Name.Length - 4), tmpfi.FullName);
                                }
                                foreach (DirectoryInfo fordi in tmpdi.EnumerateDirectories())
                                {
                                    foreach (FileInfo tmpfi in fordi.EnumerateFiles("*.png"))
                                    {
                                        tmp.Images.AddSource(fordi.Name + '_' + tmpfi.Name.ToLower().Substring(0, tmpfi.Name.Length - 4), tmpfi.FullName);
                                    }
                                }
                            }
                        }
                        break;
                    case "pet":
                        //宠物模型                           
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            LpsDocument lps = new LpsDocument(File.ReadAllText(fi.FullName));
                            if (lps.First().Name.ToLower() == "pet")
                            {
                                var name = lps.First().Info;
                                if (name == "默认虚拟桌宠")
                                    name = "vup";//旧版本名称兼容

                                var p = mw.Pets.FirstOrDefault(x => x.Name == name);
                                if (p == null)
                                {
                                    Tag.Add("pet");
                                    p = new PetLoader(lps, di);
                                    if (p.Config.Works.Count > 0)
                                        Tag.Add("work");
                                    mw.Pets.Add(p);
                                }
                                else
                                {
                                    if (lps.FindAllLine("work").Length >= 0)
                                    {
                                        Tag.Add("work");
                                    }
                                    var dis = new DirectoryInfo(di.FullName + "\\" + lps.First()["path"].Info);
                                    if (dis.Exists && dis.GetDirectories().Length > 0)
                                        Tag.Add("pet");
                                    p.path.Add(di.FullName + "\\" + lps.First()["path"].Info);
                                    p.Config.Set(lps);
                                }
                            }
                        }
                        break;
                    case "food":
                        Tag.Add("food");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
                            foreach (ILine li in tmp)
                            {
                                if (li.Name != "food")
                                    continue;
                                string tmps = li.Find("name").info;
                                mw.Foods.RemoveAll(x => x.Name == tmps);
                                mw.Foods.Add(LPSConvert.DeserializeObject<Food>(li));
                            }
                        }
                        break;
                    case "image":
                        Tag.Add("image");
                        LoadImage(mw, di);
                        break;
                    case "file":
                        Tag.Add("file");
                        LoadFile(mw, di);
                        break;
                    case "photo":
                        Tag.Add("photo");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new LPS(File.ReadAllText(fi.FullName));
                            foreach (Line li in tmp)
                            {
                                if (li.Name != "photo")
                                    continue;
                                mw.Photos.Add(new Photo(li));
                            }
                        }
                        break;
                    case "text":
                        Tag.Add("text");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
                            foreach (ILine li in tmp)
                            {
                                switch (li.Name.ToLower())
                                {
                                    case "lowfoodtext":
                                        mw.LowFoodText.Add(LPSConvert.DeserializeObject<LowText>(li));
                                        Tag.Add("lowtext");
                                        break;
                                    case "lowdrinktext":
                                        mw.LowDrinkText.Add(LPSConvert.DeserializeObject<LowText>(li));
                                        Tag.Add("lowtext");
                                        break;
                                    case "clicktext":
                                        mw.ClickTexts.Add(LPSConvert.DeserializeObject<ClickText>(li));
                                        Tag.Add("clicktext");
                                        break;
                                    case "selecttext":
                                        mw.SelectTexts.Add(LPSConvert.DeserializeObject<SelectText>(li));
                                        Tag.Add("selecttext");
                                        break;
                                    case "schedulepackage":
                                        mw.SchedulePackage.Add(LPSConvert.DeserializeObject<ScheduleTask.PackageFull>(li));
                                        break;
                                }
                            }
                        }
                        break;
                    case "lang":
                        Tag.Add("lang");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            LocalizeCore.AddCulture(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length), new LPS_D(File.ReadAllText(fi.FullName)));
                        }
                        foreach (DirectoryInfo dis in di.EnumerateDirectories())
                        {
                            foreach (FileInfo fi in dis.EnumerateFiles("*.lps"))
                            {
                                LocalizeCore.AddCulture(dis.Name, new LPS_D(File.ReadAllText(fi.FullName)));
                            }
                        }

                        if (mw.Set.Language == "null")
                        {
                            LocalizeCore.LoadDefaultCulture();
                        }
                        else
                            LocalizeCore.LoadCulture(mw.Set.Language);
                        break;
                    case "plugin":
                        Tag.Add("plugin");
                        SuccessLoad = true;
                        LpsDocument loadfile = new LpsDocument();
                        if (File.Exists(di.FullName + @"\load.lps"))
                            loadfile = new LpsDocument(File.ReadAllText(di.FullName + @"\load.lps"));
                        string authtype = "";
                        foreach (FileInfo tmpfi in di.EnumerateFiles("*.dll"))
                        {
#if X64
                            if (tmpfi.Name.Contains("x86"))
                            {
                                continue;
                            }
                            string cputype = "x64";
#else
                            if (tmpfi.Name.Contains("x64"))
                            {
                                continue;
                            }
                             string cputype = "x86";
#endif
                            if (loadfile[tmpfi.Name][(gbol)"skip"])
                                continue;

                            string dllcpu = loadfile[tmpfi.Name].GetString("cpu", "anycpu").ToLower();
                            if (dllcpu != "anycpu" && dllcpu != cputype)
                            {
                                continue;
                            }

                            try
                            {
                                var path = tmpfi.Name;
                                if (LoadPlug.ContainsKey(path))
                                {
                                    mw.Plugins.Add((MainPlugin)Activator.CreateInstance(LoadPlug[path], mw));
                                    continue;
                                }
                                if (LoadedDLL.Contains(path))
                                    continue;
                                LoadedDLL.Add(path);
                                X509Certificate2 certificate;
                                try
                                {
                                    certificate = new X509Certificate2(tmpfi.FullName);
                                }
                                catch
                                {
                                    certificate = null;
                                }
                                if (certificate != null)
                                {
                                    if (certificate.Subject == "CN=\"Shenzhen Lingban Computer Technology Co., Ltd.\", O=\"Shenzhen Lingban Computer Technology Co., Ltd.\", L=Shenzhen, S=Guangdong Province, C=CN, SERIALNUMBER=91440300MA5H8REU3K, OID.2.5.4.15=Private Organization, OID.1.3.6.1.4.1.311.60.2.1.1=Shenzhen, OID.1.3.6.1.4.1.311.60.2.1.2=Guangdong Province, OID.1.3.6.1.4.1.311.60.2.1.3=CN"
                                        && certificate.Issuer == "CN=DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1, O=\"DigiCert, Inc.\", C=US")
                                    {//LBGame 信任的证书
                                        if (authtype != "FAIL")
                                            authtype = "[认证]".Translate();
                                    }
                                    else if (!(certificate.Issuer.Contains("Microsoft Corporation") || certificate.Issuer.Contains(".NET Foundation Projects"))
                                        && !IsPassMOD(mw))
                                    {//不是通过模组,不加载
                                        SuccessLoad = false;
                                        continue;
                                    }
                                }
                                else
                                {
                                    authtype = "FAIL";
                                    if (!IsPassMOD(mw))
                                    {//不是通过模组,不加载
                                        SuccessLoad = false;
                                        Author = modlps.FindSub("author").Info.Split('[').First();
                                        continue;
                                    }
                                }
                                Assembly dll = Assembly.LoadFrom(tmpfi.FullName);
                                var v = dll.GetExportedTypes();
                                foreach (Type exportedType in v)
                                {
                                    if (exportedType.BaseType == typeof(MainPlugin))
                                    {
                                        var n = exportedType.FullName.ToLower();
                                        if (!(n.Contains("modmaker") || n.Contains("dlc")))
                                            App.MODType.Add(exportedType.FullName);
                                        LoadPlug.Add(path, exportedType);
                                        mw.Plugins.Add((MainPlugin)Activator.CreateInstance(exportedType, mw));
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                if (loadfile[tmpfi.Name][(gbol)"ignoreError"])
                                    continue;
                                ErrorMessage = e.Message + "\n" + e.InnerException.Message ?? "";
                                SuccessLoad = false;
                            }
                        }
                        if (authtype != "FAIL")
                            Author += authtype;
                        break;
                }
            }
#if !DEBUG
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Tag.Add("该模组已损坏");
                SuccessLoad = false;
            }
#endif
        }
        public bool IsOnMOD(MainWindow mw) => mw.Set.IsOnMod(Name);
#if DEBUG
        public bool IsPassMOD(MainWindow mw) => true;
#else
        public bool IsPassMOD(MainWindow mw) => mw.Set.IsPassMOD(Name);
#endif

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
        internal static bool IsOnMod(this Setting t, string ModName)
        {
            if (CoreMOD.OnModDefList.Contains(ModName))
                return true;
            var line = t.FindLine("onmod");
            if (line == null)
                return false;
            return line.Find(ModName.ToLower()) != null;
        }
        internal static bool IsPassMOD(this Setting t, string ModName)
        {
            var line = t.FindLine("passmod");
            if (line == null)
                return false;
            return line.Find(ModName.ToLower()) != null;
        }
        internal static bool IsMSGMOD(this Setting t, string ModName)
        {
            var line = t.FindorAddLine("msgmod");
            if (line.GetBool(ModName))
                return false;
            line.SetBool(ModName, true);
            return true;
        }
        internal static void OnMod(this Setting t, string ModName)
        {
            if (string.IsNullOrWhiteSpace(ModName))
                return;
            t.FindorAddLine("onmod").AddorReplaceSub(new Sub(ModName.ToLower()));
        }
        internal static void OnModRemove(this Setting t, string ModName)
        {
            t.FindorAddLine("onmod").Remove(ModName.ToLower());
        }
        internal static void PassMod(this Setting t, string ModName)
        {
            t.FindorAddLine("passmod").AddorReplaceSub(new Sub(ModName.ToLower()));
        }
        internal static void PassModRemove(this Setting t, string ModName)
        {
            t.FindorAddLine("passmod").Remove(ModName.ToLower());
        }
    }
}
