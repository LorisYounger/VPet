using LinePutScript;
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
using VPet_Simulator.Unified.Services;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    internal class CoreMOD : IModInfo
    {
        /// <summary>
        /// 自动启用MOD名称
        /// </summary>
        public static readonly string[] OnModDefList = ModSwitchStore.AlwaysOn.ToArray();

        public static HashSet<string> LoadedDLL { get; } = new HashSet<string>()
        {
            "Panuon.WPF.dll","steam_api.dll","Panuon.WPF.UI.dll","steam_api64.dll",
            "LinePutScript.dll","Facepunch.Steamworks.Win32.dll", "Facepunch.Steamworks.Win64.dll",
            "VPet-Simulator.Core.dll","VPet-Simulator.Windows.Interface.dll","LinePutScript.Localization.WPF.dll",
            "VPet-Simulator.Unified.dll",
            "NAudio.Asio.dll", "libSkiaSharp.dll","NAudio.Core.dll","NAudio.dll", "SkiaSharp.dll","NAudio.Midi.dll",
            "NAudio.Wasapi.dll","NAudio.WinForms.dll", "NAudio.WinMM.dll", "WpfAnimatedGif.dll"
        };
        public static Dictionary<string, Type> LoadPlug { get; } = new Dictionary<string, Type>();
        /// <summary>
        /// 已加载的统一契约插件类型
        /// </summary>
        /// 与 LoadPlug 分开放: 一个 dll 里同时有旧插件和新插件时两边都要认得出来
        internal static Dictionary<string, Type> LoadUnifiedPlug { get; } = new Dictionary<string, Type>();
        public static string? NowLoading = null;
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        /// <summary>
        /// 如果是上传至Steam,则为SteamUserID
        /// </summary>
        public long AuthorID { get; set; } = 0;
        /// <summary>
        /// 上传至Steam的ItemID
        /// </summary>
        public ulong ItemID { get; set; } = 0;
        public string Intro { get; set; } = string.Empty;
        public DirectoryInfo Path { get; set; } = null!;
        public int GameVer { get; set; }
        public int Ver { get; set; }
        public HashSet<string> Tag { get; set; } = new HashSet<string>();
        public bool SuccessLoad = true;
        public DateTime CacheDate;
        public string ErrorMessage = string.Empty;
        public static string INTtoVER(int ver) => ver < 10000 ? $"{ver / 100}.{ver % 100:00}" : $"{ver / 10000}.{ver % 10000 / 100}.{ver % 100:00}";
        public static void LoadImage(MainWindow mw, DirectoryInfo di, string pre = "")
        {
            //目录遍历、键名拼法、覆盖顺序都走共享后端
            var index = new ResourceIndex();
            var settings = new List<string>();
            index.AddImages(di, pre, settings);
            foreach (var source in index.Sources)
            {
                mw.ImageSources.AddSource(source.Key, source.Value);
            }
            //图片设置(定位锚点之类)是 Resources 自己的存储, 后端不碰界面相关的东西
            foreach (var path in settings)
            {
                foreach (var line in new LpsDocument(File.ReadAllText(path)))
                    mw.ImageSources.ImageSetting.AddorReplaceLine(line);
            }
        }
        public static void LoadFile(MainWindow mw, DirectoryInfo di, string pre = "")
        {
            //与图片同一套遍历规则, 只是文件的键**带**扩展名
            var index = new ResourceIndex();
            index.AddFiles(di, pre);
            foreach (var source in index.Sources)
            {
                mw.FileSources.AddSource(source.Key, source.Value);
            }
        }
        public CoreMOD(DirectoryInfo directory, MainWindow mw)
        {
#if !DEBUG
            try
            {
#endif
            Path = directory;
            //info.lps 的解析走共享后端, 跨平台版读出来的 MOD 列表与这边逐字一致
            var meta = ModInfoReader.Parse(directory)
                ?? throw new FileNotFoundException("找不到 info.lps", directory.FullName + @"\info.lps");
            if (meta.Warnings.Count != 0)
                throw new Exception(string.Join("\n", meta.Warnings));
            if (string.IsNullOrEmpty(meta.Name))
                throw new Exception("info.lps 里没有 vupmod 行");

            Name = meta.Name;
            NowLoading = Name;
            Intro = meta.Intro;
            GameVer = meta.GameVer;
            Ver = meta.Ver;
            Author = meta.Author;
            AuthorID = meta.AuthorID;
            ItemID = meta.ItemID;
            CacheDate = meta.CacheDate;
            foreach (var skip in meta.DllSkip)
            {
                LoadedDLL.Add(skip);
            }

            //MOD未加载时支持翻译
            foreach (var (culture, line) in meta.PreTranslations)
            {
                List<ILine> ls = new List<ILine>();
                foreach (var sub in line)
                {
                    ls.Add(new Line(sub.Name, sub.info));
                }
                LocalizeCore.AddCulture(culture, ls);
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
                foreach (var tag in meta.ContentTags)
                    Tag.Add(tag);
                return;
            }

            foreach (DirectoryInfo di in Path.EnumerateDirectories())
            {
                switch (di.Name.ToLowerInvariant())
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
                                    tmp.Images.AddSource(tmpfi.Name.ToLowerInvariant().Substring(0, tmpfi.Name.Length - 4), tmpfi.FullName);
                                }
                                foreach (DirectoryInfo fordi in tmpdi.EnumerateDirectories())
                                {
                                    foreach (FileInfo tmpfi in fordi.EnumerateFiles("*.png"))
                                    {
                                        tmp.Images.AddSource(fordi.Name + '_' + tmpfi.Name.ToLowerInvariant().Substring(0, tmpfi.Name.Length - 4), tmpfi.FullName);
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
                            if (lps.First()!.Name.ToLowerInvariant() == "pet")
                            {
                                var name = lps.First()!.Info;
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
                                    var dis = new DirectoryInfo(di.FullName + "\\" + lps.First()!["path"].Info);
                                    if (dis.Exists && dis.GetDirectories().Length > 0)
                                        Tag.Add("pet");
                                    p.path.Add(di.FullName + "\\" + lps.First()!["path"].Info);
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
                                string tmps = li.Find("name")!.info;
                                mw.Foods.RemoveAll(x => x.Name == tmps);
                                mw.Foods.Add(LPSConvert.DeserializeObject<Food>(li)!);
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
                                switch (li.Name.ToLowerInvariant())
                                {
                                    case "lowfoodtext":
                                        mw.LowFoodText.Add(LPSConvert.DeserializeObject<LowText>(li)!);
                                        Tag.Add("lowtext");
                                        break;
                                    case "lowdrinktext":
                                        mw.LowDrinkText.Add(LPSConvert.DeserializeObject<LowText>(li)!);
                                        Tag.Add("lowtext");
                                        break;
                                    case "clicktext":
                                        mw.ClickTexts.Add(LPSConvert.DeserializeObject<ClickText>(li)!);
                                        Tag.Add("clicktext");
                                        break;
                                    case "selecttext":
                                        mw.SelectTexts.Add(LPSConvert.DeserializeObject<SelectText>(li)!);
                                        Tag.Add("selecttext");
                                        break;
                                    case "schedulepackage":
                                        mw.SchedulePackage.Add(LPSConvert.DeserializeObject<ScheduleTask.PackageFull>(li)!);
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
                            //x86/x64 标记与 load.lps 的 skip/cpu 都走共享后端, 设置窗口那边核对
                            //证书时用的是同一份规则, 免得两处对不上
                            if (PluginClassifier.ShouldSkip(tmpfi.Name, loadfile))
                                continue;

                            try
                            {
                                var path = tmpfi.Name;
                                //统一契约的插件: 给第二个及以后的窗口补出各自的实例
                                if (LoadUnifiedPlug.TryGetValue(path, out var unifiedType))
                                {
                                    UnifiedPluginHost.Create(mw, this, unifiedType);
                                    //同一个 dll 里还有旧插件的话, 接着往下走那条老路
                                    if (!LoadPlug.ContainsKey(path))
                                        continue;
                                }
                                if (LoadPlug.ContainsKey(path))
                                {
                                    var Instance = (MainPlugin?)Activator.CreateInstance(LoadPlug[path], mw);
                                    if (Instance != null)
                                        mw.Plugins.Add(Instance);
                                    continue;
                                }
                                if (LoadedDLL.Contains(path))
                                    continue;
                                LoadedDLL.Add(path);
                                X509Certificate2? certificate;
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
                                    if (IsLBGameCertificate(certificate))
                                    {//LBGame 信任的证书
                                        if (authtype != "FAIL")
                                            authtype = "[认证]".Translate();
                                    }
                                    else if (!IsTrustedCertificate(certificate)
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
                                        //Author 在构造函数开头就已经按同样的规则取过了, 这里不用再读一遍
                                        continue;
                                    }
                                }
                                Assembly dll = Assembly.LoadFrom(tmpfi.FullName);
                                var v = dll.GetExportedTypes();
                                foreach (Type exportedType in v)
                                {
                                    if (exportedType.BaseType == typeof(MainPlugin))
                                    {
                                        if (exportedType == null) continue;
                                        if (exportedType.FullName == null) continue;
                                        var n = exportedType.FullName.ToLowerInvariant();
                                        if (!(n.Contains("modmaker") || n.Contains("dlc")))
                                            App.MODType.Add(exportedType.FullName);
                                        LoadPlug.Add(path, exportedType);
                                        var Instance = (MainPlugin?)Activator.CreateInstance(exportedType!, mw!);
                                        if (Instance == null) continue;
                                        mw.Plugins.Add(Instance);
                                    }
                                    //统一契约的插件: 同一个 dll 在跨平台版上也能加载.
                                    //旧的 MainPlugin 那条路一字未动, 这里纯属新增.
                                    else if (exportedType.BaseType == typeof(VPet_Simulator.Unified.Interface.UnifiedPlugin))
                                    {
                                        if (exportedType.FullName == null) continue;
                                        var n = exportedType.FullName.ToLowerInvariant();
                                        if (!(n.Contains("modmaker") || n.Contains("dlc")))
                                            App.MODType.Add(exportedType.FullName);
                                        LoadUnifiedPlug.Add(path, exportedType);
                                        UnifiedPluginHost.Create(mw, this, exportedType);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                if (loadfile[tmpfi.Name][(gbol)"ignoreError"])
                                    continue;
                                Console.WriteLine($"{Name}:Load DLL {tmpfi.FullName} failed: {e.Message}");
                                ErrorMessage = e.ToString();
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
                Console.WriteLine("{Name}:Error {e.Message}");
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
            modlps.FindLine("vupmod")!.Info = Name;
            modlps.FindLine("intro")!.Info = Intro;
            modlps.FindSub("gamever")!.InfoToInt = GameVer;
            modlps.FindSub("ver")!.InfoToInt = Ver;
            modlps.FindSub("author")!.Info = Author;
            modlps.FindorAddLine("authorid").InfoToInt64 = AuthorID;
            modlps.FindorAddLine("itemid").info = ItemID.ToString();
            File.WriteAllText(Path.FullName + @"\info.lps", modlps.ToString());
        }

        public static bool IsLBGameCertificate(X509Certificate2 certificate)
        {
            return (certificate.Subject == "CN=\"Shenzhen Lingban Computer Technology Co., Ltd.\", O=\"Shenzhen Lingban Computer Technology Co., Ltd.\", L=Shenzhen, S=Guangdong Province, C=CN, SERIALNUMBER=91440300MA5H8REU3K, OID.2.5.4.15=Private Organization, OID.1.3.6.1.4.1.311.60.2.1.1=Shenzhen, OID.1.3.6.1.4.1.311.60.2.1.2=Guangdong Province, OID.1.3.6.1.4.1.311.60.2.1.3=CN"
                                        && certificate.Issuer == "CN=DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1, O=\"DigiCert, Inc.\", C=US")
                                        || (certificate.Subject == "CN=\"Shenzhen Zero Edition Computer Technology Co., Ltd.\", O=\"Shenzhen Zero Edition Computer Technology Co., Ltd.\", L=Shenzhen, S=Guangdong, C=CN, SERIALNUMBER=91440300MA5H8REU3K, OID.1.3.6.1.4.1.311.60.2.1.1=Shenzhen, OID.1.3.6.1.4.1.311.60.2.1.2=Guangdong, OID.1.3.6.1.4.1.311.60.2.1.3=CN, OID.2.5.4.15=Private Organization"
                                        && certificate.Issuer == "CN=Certum Extended Validation Code Signing 2021 CA, O=Asseco Data Systems S.A., C=PL");
        }
        public static bool IsTrustedCertificate(X509Certificate2 certificate)
        {
            return certificate.Issuer.Contains("Microsoft Corporation") ||
                                        certificate.Issuer.Contains(".NET Foundation Projects") ||
                                        certificate.Issuer == "CN=DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1, O=\"DigiCert, Inc.\", C=US" ||
                                        certificate.Issuer == "CN=Certum Extended Validation Code Signing 2021 CA, O=Asseco Data Systems S.A., C=PL";
        }
    }
    /// <summary>
    /// MOD 开关
    /// </summary>
    /// 键名和取值方式都在共享后端 ModSwitchStore 里, 这里只留调用起来顺手的壳.
    /// 两个平台共用同一份规则, 所以 Setting.lps 可以在两边直接拷来拷去.
    public static class ExtensionSetting
    {

        internal static bool IsOnMod(this Setting t, string ModName) => ModSwitchStore.IsOn(t, ModName);
        internal static bool IsPassMOD(this Setting t, string ModName) => ModSwitchStore.IsPassed(t, ModName);
        internal static bool IsMSGMOD(this Setting t, string ModName) => ModSwitchStore.ShouldNotice(t, ModName);
        internal static void OnMod(this Setting t, string ModName) => ModSwitchStore.On(t, ModName);
        internal static void OnModRemove(this Setting t, string ModName) => ModSwitchStore.Off(t, ModName);
        internal static void PassMod(this Setting t, string ModName) => ModSwitchStore.Pass(t, ModName);
        internal static void PassModRemove(this Setting t, string ModName) => ModSwitchStore.PassRemove(t, ModName);
    }
}
