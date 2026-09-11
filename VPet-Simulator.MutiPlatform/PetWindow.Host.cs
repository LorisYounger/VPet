using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Unified.Services;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 给统一契约宿主用的那一面
/// </summary>
/// 全部集中在这里而不是散在窗口各处, 是为了让"MOD 能碰到什么"一眼看得完.
/// 窗口自己的投喂、设置菜单也走这一面 —— 宿主和 MOD 用同一个门面, 门面漏了什么
/// 宿主自己先难受, 不至于等到有人写 MOD 才发现。
public partial class PetWindow
{
    /// <summary>
    /// 宿主版本号
    /// </summary>
    /// 与 Windows 版的 version 同义, 供 MOD 判断能力
    internal const int HostVersion = 12100;

    internal GameCore? HostCore => core;
    internal PetMain? HostPet => pet;
    internal AppSettings HostSettings => settings;
    internal ModResources HostResources => resources;
    internal GameSave_v2 HostGameSave => gameSavesData;
    internal string HostPrefixSave => prefixSave;

    /// <summary>
    /// 背包
    /// </summary>
    internal ItemStore HostItems { get; } = new ItemStore();

    /// <summary>
    /// 已解码的图片
    /// </summary>
    internal ImageCache HostImages => images;

    /// <summary>
    /// 照片图库
    /// </summary>
    internal PhotoStore HostPhotos => resources.Photos;

    /// <summary>
    /// 已加载的 MOD
    /// </summary>
    internal List<ModLoader> HostMods { get; } = new List<ModLoader>();

    /// <summary>
    /// Steam 能力
    /// </summary>
    /// 现在是空实现: Facepunch.Steamworks 的 Posix 绑定要额外引一个包, 往仓库里
    /// 加依赖该由项目所有者定, 不该由迁移工作顺手做掉. 接口和调用处都已经就位,
    /// 换成真实现只需要引包之后把这里换掉 —— Windows 侧的 SteamCapability 就是
    /// 照着同一个接口写的, 托管 API 三个平台一样, 那份代码几乎可以直接搬过来.
    ///
    /// 在此之前所有 Steam 功能安静地不可用, 而不是崩掉。
    internal ISteamServices HostSteam { get; set; } =
        new NullSteamServices("跨平台版还没接 Steam, 见 PetWindow.Host.cs 的说明");

    /// <summary>
    /// 联机
    /// </summary>
    /// 与 HostSteam 同一个理由: 换成真实现只需要引包之后把这里换掉,
    /// Windows 侧的 SteamMultiplayer 就是照着同一个接口写的
    internal ISteamMultiplayer HostMultiplayer { get; set; } =
        new NullSteamMultiplayer("跨平台版还没接 Steam, 联机要等它");

    /// <summary>
    /// 跨 MOD 共享的一袋子东西
    /// </summary>
    internal Dictionary<string, object> HostDynamicResources { get; } = new Dictionary<string, object>();

    /// <summary>
    /// 新的一天开始时触发
    /// </summary>
    internal event Action? HostNewDay;

    /// <summary>
    /// 有东西被喂给桌宠时触发
    /// </summary>
    internal event Action<IFoodInfo>? HostFoodTaken;

    /// <summary>
    /// 上次喂东西的时间
    /// </summary>
    internal DateTime HostLastTakeItemTime { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// 上一次触发"新的一天"是哪一天
    /// </summary>
    private int lastNewDayOfYear = -1;

    /// <summary>
    /// 该不该触发"新的一天"
    /// </summary>
    /// 由逻辑心跳定期调用: 桌宠常常挂一整夜, 靠启动时判一次是不够的
    internal void HostCheckNewDay()
    {
        int today = DateTime.Now.DayOfYear;
        if (lastNewDayOfYear == today)
            return;
        // 第一次调用只是记下今天, 不该在启动时白触发一次
        bool first = lastNewDayOfYear < 0;
        lastNewDayOfYear = today;
        if (first)
            return;
        try { HostNewDay?.Invoke(); }
        catch (Exception ex) { Log($"新的一天回调出错: {ex.Message}"); }
    }

    // ---- 物品 ----

    /// <summary>
    /// 往背包里加一件并把图片路径解析好
    /// </summary>
    internal void HostAddItem(UnifiedItem item)
    {
        var made = HostItems.Add(item);
        made.ImagePath = resources.FindImagePath(
            made.ItemType + "_" + (made.Image ?? made.Name), "food");
    }

    // ---- 投喂 ----

    /// <summary>
    /// 喂一份食物给桌宠 (结算数值, 不扣钱)
    /// </summary>
    internal void HostTakeFood(IFoodInfo food)
    {
        if (food is FoodItem item)
            FeedNoCharge(item);
        else
            Log($"喂不了 {food.Name}: 它不是本宿主的食物");
    }

    /// <summary>
    /// 通知宿主"用掉了某样东西", 供统计和成就用
    /// </summary>
    /// <summary>
    /// 通知挂在契约上的 MOD: 有东西被喂了
    /// </summary>
    internal void HostRaiseFoodTaken(IFoodInfo food)
    {
        HostLastTakeItemTime = DateTime.Now;
        try { HostFoodTaken?.Invoke(food); }
        catch (Exception ex) { Log($"投喂回调出错: {ex.Message}"); }
    }

    internal void HostTakeFoodHandle(IFoodInfo food, int count, string from)
    {
        HostLastTakeItemTime = DateTime.Now;
        Log($"投喂: {food.Name} x{count} ({from})");
    }

    /// <summary>
    /// 播一段带图片的动画
    /// </summary>
    internal void HostDisplayFoodAnimation(string graphName, string imagePath)
    {
        if (pet == null)
            return;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var bitmap = images.Get(imagePath);
            if (bitmap == null)
                pet.DisplayToNomal();
            else
                pet.Display(graphName, bitmap, pet.DisplayToNomal);
        });
    }

    // ---- 说话文本 ----

    /// <summary>
    /// 按当前时间和状态挑一条点击文本
    /// </summary>
    internal ClickText? HostPickClickText() => GetClickText();

    // ---- 界面挂点 ----

    /// <summary>
    /// 往工具栏里加按钮
    /// </summary>
    internal void HostAddMenuButton(PetMenuType menu, string? groupName, string displayName, Action click)
    {
        var toolbar = pet?.ToolBar;
        if (toolbar == null)
        {
            Log($"工具栏还没建好, 按钮 {displayName} 没加上");
            return;
        }
        var target = (ToolBar.MenuType)(int)menu;
        if (groupName == null)
            toolbar.AddMenuButton(target, displayName, click);
        else
            toolbar.AddMenuButton(target, groupName, displayName, click);
    }

    /// <summary>
    /// 给玩家看一条提示
    /// </summary>
    /// 用不打断玩家的那种(说一声就走), 同时记一条日志方便事后查
    internal void ShowHostMessage(string title, string text)
    {
        Log($"[{title}] {text}");
        DialogService.Notice(text, title, owner: this);
    }

    /// <summary>
    /// 弹个输入框
    /// </summary>
    /// 契约给的是回调而不是返回值, 因为 MOD 不该被迫写异步; 取消时不回调,
    /// 与 Windows 版 ShowInputBox 的语义一致
    internal void ShowHostInputBox(string title, string text, string defaultText, Action<string> end, bool allowMultiLine)
    {
        _ = InputAsync();

        async System.Threading.Tasks.Task InputAsync()
        {
            try
            {
                var result = await DialogService.InputAsync(text, defaultText, title, allowMultiLine, this);
                if (result != null)
                    end(result);
            }
            catch (Exception ex)
            {
                Log($"输入框出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 缩放
    /// </summary>
    internal void HostSetZoomLevel(double level) => SetZoomLevel(level);

    /// <summary>
    /// 把移动设置重新应用一遍
    /// </summary>
    internal void HostApplyMoveMode() => ApplyMoveMode();

    /// <summary>
    /// 重新应用主题与字体
    /// </summary>
    internal void HostApplyTheme() => ApplyTheme();

    /// <summary>
    /// 重启
    /// </summary>
    /// 起一个新进程再退掉自己. 参数原样带过去, 多开时重启的还是同一只桌宠。
    /// 起不来就只关不开 —— 那比留一个假装重启了其实什么都没做的按钮好。
    internal void HostRestart()
    {
        try
        {
            var path = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(path))
            {
                Log("重启失败: 找不到自己的可执行文件");
                return;
            }
            var info = new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true };
            if (!string.IsNullOrEmpty(prefixSave))
            {
                info.ArgumentList.Add("--prefix");
                info.ArgumentList.Add(MultiPetStore.DisplayName(prefixSave));
            }
            System.Diagnostics.Process.Start(info);
        }
        catch (Exception ex)
        {
            Log($"重启失败: {ex.Message}");
            return;
        }
        Close();
    }

    /// <summary>
    /// 立刻存档
    /// </summary>
    internal void SaveNowFromHost() => SaveNow();

    // ---- 语音 ----

    /// <summary>
    /// MOD 提供的语音播放器
    /// </summary>
    /// Avalonia 没有内置音频后端, 引一个原生音频库不该由迁移工作单方面替项目决定,
    /// 所以语音由 MOD 实现, 宿主只负责转发
    /// 插件可能在 PetMain 建好之前就登记了播放器, 先存着, 等桌宠建好再交过去
    private IVoicePlayer? voicePlayer;

    internal void HostSetVoicePlayer(IVoicePlayer player)
    {
        voicePlayer = player;
        //交给 PetMain: 消息栏的口型同步要从那里读还剩多久
        if (pet != null)
            pet.VoicePlayer = player;
    }

    /// <summary>
    /// 桌宠建好之后把攒下的播放器交过去
    /// </summary>
    private void AttachVoicePlayer()
    {
        if (pet != null && voicePlayer != null)
            pet.VoicePlayer = voicePlayer;
    }

    internal void HostPlayVoice(string path)
    {
        if (voicePlayer == null)
        {
            Log("没有语音播放器, 装一个跨平台语音 MOD 就能出声");
            return;
        }
        pet?.PlayVoice(path);
    }

    internal double HostVoiceVolume
    {
        get => pet?.PlayVoiceVolume ?? voicePlayer?.Volume ?? 1;
        set
        {
            if (pet != null)
                pet.PlayVoiceVolume = value;
            else if (voicePlayer != null)
                voicePlayer.Volume = value;
        }
    }

    internal bool HostPlayingVoice => pet?.PlayingVoice ?? false;
}
