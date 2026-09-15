//跨平台: 对应 VPet-Simulator.Windows.Interface/ExtensionFunction.cs 里的 ExtensionValue (Windows 半).
//BaseDirectory 与 Windows 版一样, 统一使用运行文件所在目录.
using System;
using System.Collections.Generic;
using System.IO;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 扩展值
    /// </summary>
    public static partial class ExtensionValue
    {
        /// <summary>
        /// 当前运行目录
        /// </summary>
        public static string BaseDirectory = VPet_Simulator.Core.MutiPlatform.AppPaths.InstallDirectory;
        /// <summary>
        /// 获取MOD存储目录 (会自动创建)
        /// 但是还是建议以LPS形式存在Setting/Save里 不保证完整可靠性(可能会因为切换电脑等导致数据丢失)
        /// </summary>
        /// <param name="modName">MOD名字</param>
        /// <returns>目录地址</returns>
        public static string GetMODStorage(string modName)
        {
            var path = Path.Combine(BaseDirectory, "ModData");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, modName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        //跨平台: 原文复制自 Windows.Interface/ExtensionFunction.cs, 加上 Avalonia 与共享程序集那几行
        public static readonly IReadOnlyDictionary<string, string> DllReferenceDescriptions =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["VPet-Simulator.Core"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet-Simulator.Windows.Interface"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet-Simulator.Windows.Interface.Base"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet_Simulator.Core.MutiPlatform"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet_Simulator.Core.Base"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet-Simulator.Unified.Interface"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet-Simulator.Unified.Services"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet.ModMaker"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet.Plugin.VPetTTS"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet.Plugin.ChatGPTPlus.x64"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet.Plugin.DoingDisplay"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet.Plugin.CloudSaves"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet.Plugin.MutiRedEnvelope"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet.Plugin.Monitor"] = "License: Apache-2.0 | Copyright © VPet Group",
                ["VPet.Plugin.SearchBoxForMod"] = "License: Apache-2.0 | Copyright © VPet Group",

                ["NAudio"] = "License: MIT | Copyright © Mark Heath",
                ["NAudio.Asio"] = "License: MIT | Copyright © Mark Heath",
                ["NAudio.Core"] = "License: MIT | Copyright © Mark Heath",
                ["NAudio.Midi"] = "License: MIT | Copyright © Mark Heath",
                ["NAudio.Wasapi"] = "License: MIT | Copyright © Mark Heath",
                ["NAudio.WinForms"] = "License: MIT | Copyright © Mark Heath",
                ["NAudio.WinMM"] = "License: MIT | Copyright © Mark Heath",
                ["NAudio.SoundFont"] = "License: MIT | Copyright © Mark Heath",

                ["Steamworks"] = "License: MIT | Copyright © Facepunch Studios LTD",
                ["Steamworks.Ugc"] = "License: MIT | Copyright © Facepunch Studios LTD",
                ["Facepunch.Steamworks.Win32"] = "License: MIT | Copyright © Facepunch Studios LTD",
                ["Facepunch.Steamworks.Win64"] = "License: MIT | Copyright © Facepunch Studios LTD",
                ["Facepunch.Steamworks.Posix"] = "License: MIT | Copyright © Facepunch Studios LTD",
                ["steam_api"] = "License: Proprietary | Copyright © Valve Corporation",
                ["steam_api64"] = "License: Proprietary | Copyright © Valve Corporation",

                ["Avalonia"] = "License: MIT | Copyright © AvaloniaUI OÜ",
                ["Avalonia.Base"] = "License: MIT | Copyright © AvaloniaUI OÜ",
                ["Avalonia.Controls"] = "License: MIT | Copyright © AvaloniaUI OÜ",
                ["Avalonia.Controls.DataGrid"] = "License: MIT | Copyright © AvaloniaUI OÜ",
                ["Avalonia.Skia"] = "License: MIT | Copyright © AvaloniaUI OÜ",
                ["Avalonia.Themes.Simple"] = "License: MIT | Copyright © AvaloniaUI OÜ",
                ["SkiaSharp"] = "License: MIT | Copyright © Xamarin, Inc. / Microsoft Corporation",
                ["libSkiaSharp"] = "License: MIT | Copyright © Xamarin, Inc. / Microsoft Corporation",
                ["WpfAnimatedGif"] = "License: MIT | Copyright © Thomas Levesque",
                ["Live2DCubismCore"] = "License: Live2D Proprietary | Copyright © Live2D Inc.",
                ["glfw3"] = "License: zlib/libpng | Copyright © Marcus Geelnard, Camilla Löwy",

                ["Newtonsoft.Json"] = "License: MIT | Copyright © James Newton-King",
                ["YamlDotNet"] = "License: MIT | Copyright © YamlDotNet contributors",

                ["HanumanInstitute.MvvmDialogs"] = "License: MIT | Copyright © HanumanInstitute (mysteryx93)",
                ["HanumanInstitute.MvvmDialogs.Wpf"] = "License: MIT | Copyright © HanumanInstitute (mysteryx93)",

                ["ReactiveUI"] = "License: MIT | Copyright © ReactiveUI Association and Contributors",
                ["ReactiveUI.Wpf"] = "License: MIT | Copyright © ReactiveUI Association and Contributors",
                ["Splat"] = "License: MIT | Copyright © ReactiveUI / .NET Foundation and Contributors",
                ["Splat.NLog"] = "License: MIT | Copyright © ReactiveUI / .NET Foundation and Contributors",
                ["System.Reactive"] = "License: MIT | Copyright © .NET Foundation and Contributors",

                ["Microsoft.Bcl.AsyncInterfaces"] = "License: MIT | Copyright © .NET Foundation and Contributors",
                ["Microsoft.Extensions.DependencyInjection"] = "License: MIT | Copyright © .NET Foundation and Contributors",
                ["Microsoft.Extensions.DependencyInjection.Abstractions"] = "License: MIT | Copyright © .NET Foundation and Contributors / Microsoft Corporation",
                ["Microsoft.Extensions.Logging.Abstractions"] = "License: MIT | Copyright © .NET Foundation and Contributors",
                ["Microsoft.Xaml.Behaviors"] = "License: MIT | Copyright © Microsoft Corporation",
                ["System.IO.Pipelines"] = "License: MIT | Copyright © .NET Foundation and Contributors",
                ["System.Drawing.Common"] = "License: MIT | Copyright © .NET Foundation",

                ["NLog"] = "License: BSD 3-Clause | Copyright © Jaroslaw Kowalski, Kim Christensen, Julian Verdurmen",
                ["Serilog"] = "License: Apache-2.0 | Copyright © Serilog Contributors",

                ["DynamicData"] = "License: MIT | Copyright © Roland Pheasant",
                ["LinePutScript"] = "License: Apache-2.0 | Copyright © LorisYounger",
                ["LinePutScript.Localization"] = "License: Apache-2.0 | Copyright © LorisYounger",
                ["EdgeTTS.Net"] = "License: GPL-3.0 | Copyright © rany2 & LorisYounger",
                ["CloudSaves.Client"] = "License: MIT | Copyright © David DeSimone",

                ["HKW.CommonValueConverters"] = "Copyright © Hakoyu",
                ["HKW.Mapper"] = "Copyright © Hakoyu",
                ["HKW.MVVMDialogs"] = "Copyright © Hakoyu",
                ["HKW.ReactiveUI"] = "Copyright © Hakoyu",
                ["HKW.Utils"] = "Copyright © Hakoyu",
                ["HKW.WPF"] = "Copyright © Hakoyu",
            };
    }
}
