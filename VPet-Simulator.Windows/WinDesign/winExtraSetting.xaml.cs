using LinePutScript;
using LinePutScript.Localization.WPF;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Windows;

/// <summary>
/// 额外附加设置窗口
/// </summary>
public partial class winExtraSetting : WindowX
{
    private readonly MainWindow mw;
    private bool allowChange;
    private bool allowProviderChange;
    private MiMoAsrRecorder testAsrRecorder;
    private bool isTestingAsr;
    private string cachedAsrTestPath;
    private readonly Dictionary<string, CheckBox> actionLinkChecks = new(StringComparer.OrdinalIgnoreCase);

    public winExtraSetting(MainWindow mw, int page = 0)
    {
        this.mw = mw;
        InitializeComponent();

        Title = "额外附加设置".Translate() + ' ' + mw.PrefixSave;
        InitMenu();
        LoadCheatValues();
        LoadChatSettings();
        LoadLlmConfig(LlmChatConfig.Load(mw.Set));
        LoadActionLinkConfig(LlmActionLinkConfig.Load(mw.Set));
        LoadMiMoVoiceConfig(MiMoVoiceConfig.Load(mw.Set));
        allowChange = true;
        SelectPage(page);
    }

    public void SelectPage(int page)
    {
        if (page < 0 || page >= MainTab.Items.Count)
            page = 0;
        MainTab.SelectedIndex = page;
        ListMenu.SelectedIndex = page;
    }

    private void InitMenu()
    {
        ListMenu.Items.Add("金手指".Translate());
        ListMenu.Items.Add("大语言模型".Translate());
        ListMenu.Items.Add("动作联动".Translate());
        ListMenu.Items.Add("语音合成".Translate());
        ListMenu.Items.Add("语音输入".Translate());
        ListMenu.SelectedIndex = 0;
    }

    private void LoadChatSettings()
    {
        cbChatAPISelect.Items.Clear();
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
            case "API":
                RBCGPTUseAPI.IsChecked = true;
                break;
            case "DIY":
                RBCGPTDIY.IsChecked = true;
                break;
            case "LB":
                RBCGPTUseLB.IsChecked = true;
                break;
            case "OFF":
            default:
                RBCGPTClose.IsChecked = true;
                break;
        }
    }

    private void LoadLlmConfig(LlmChatConfig config)
    {
        allowProviderChange = false;
        var provider = config.Provider is LlmChatProvider.DeepSeek or LlmChatProvider.MiMo
            ? config.Provider
            : LlmChatProvider.Ollama;
        bool providerChanged = config.Provider != provider;
        for (int i = 0; i < cbLlmProvider.Items.Count; i++)
        {
            if (cbLlmProvider.Items[i] is ComboBoxItem item && item.Tag?.ToString() == provider.ToString())
            {
                cbLlmProvider.SelectedIndex = i;
                break;
            }
        }

        string selectedModel = providerChanged || string.IsNullOrWhiteSpace(config.Model)
            ? LlmChatConfig.DefaultModel(provider)
            : config.Model;
        SetLlmModelOptions(provider, selectedModel);
        tbLlmBaseUrl.Text = providerChanged || string.IsNullOrWhiteSpace(config.BaseUrl)
            ? LlmChatConfig.DefaultBaseUrl(provider)
            : config.BaseUrl;
        pbLlmApiKey.Password = provider == LlmChatProvider.Ollama ? "" : LlmEnvironmentKeys.ResolveApiKey(provider, config.ApiKey);
        tbLlmSystemPrompt.Text = config.SystemPrompt;
        numLlmTemperature.Value = config.Temperature;
        numLlmMaxTokens.Value = config.MaxTokens;
        numLlmHistoryTurns.Value = config.HistoryTurns;
        numLlmTimeoutSeconds.Value = config.TimeoutSeconds;
        cbLlmBubbleAutoClose.IsChecked = config.BubbleAutoCloseEnabled;
        numLlmBubbleAutoCloseSeconds.Value = config.BubbleAutoCloseSeconds;
        cbOllamaPreloadBeforeSend.IsChecked = config.OllamaPreloadBeforeSend;
        cbOllamaThink.IsChecked = config.OllamaThink;
        numOllamaKeepAliveMinutes.Value = config.OllamaKeepAliveMinutes;
        numOllamaContextLength.Value = config.OllamaContextLength;
        allowProviderChange = true;
        UpdateProviderControls(provider);
        UpdateLlmConfigSummary(new LlmChatConfig { Provider = provider, Model = cbLlmModel.Text });
    }

    private LlmChatConfig ReadLlmConfig()
    {
        var provider = GetSelectedProvider();
        return new LlmChatConfig
        {
            Provider = provider,
            BaseUrl = tbLlmBaseUrl.Text,
            Model = cbLlmModel.Text,
            ApiKey = provider == LlmChatProvider.Ollama ? "" : LlmEnvironmentKeys.ResolveApiKey(provider, pbLlmApiKey.Password),
            SystemPrompt = tbLlmSystemPrompt.Text,
            Temperature = numLlmTemperature.Value ?? 0.8,
            MaxTokens = (int)(numLlmMaxTokens.Value ?? 1024),
            HistoryTurns = (int)(numLlmHistoryTurns.Value ?? 8),
            TimeoutSeconds = (int)(numLlmTimeoutSeconds.Value ?? 60),
            BubbleAutoCloseEnabled = cbLlmBubbleAutoClose.IsChecked == true,
            BubbleAutoCloseSeconds = (int)(numLlmBubbleAutoCloseSeconds.Value ?? 12),
            OllamaPreloadBeforeSend = cbOllamaPreloadBeforeSend.IsChecked == true,
            OllamaKeepAliveMinutes = (int)(numOllamaKeepAliveMinutes.Value ?? 5),
            OllamaContextLength = (int)(numOllamaContextLength.Value ?? 0),
            OllamaThink = cbOllamaThink.IsChecked == true
        };
    }

    private LlmChatProvider GetSelectedProvider()
    {
        if (cbLlmProvider.SelectedItem is ComboBoxItem item
            && Enum.TryParse(item.Tag?.ToString(), true, out LlmChatProvider provider)
            && provider is LlmChatProvider.DeepSeek or LlmChatProvider.MiMo)
        {
            return provider;
        }
        return LlmChatProvider.Ollama;
    }

    private bool TrySaveLlmConfig(bool showMessage)
    {
        var config = ReadLlmConfig();
        string savedApiKey = mw.Set["LLMChat"].GetString("apiKey", "");
        if (string.IsNullOrWhiteSpace(config.Model))
        {
            if (showMessage)
                MessageBoxX.Show("请先填写模型名称".Translate());
            return false;
        }
        if (config.Provider != LlmChatProvider.Ollama && string.IsNullOrWhiteSpace(config.ApiKey))
        {
            if (showMessage)
                MessageBoxX.Show(GetLlmApiKeyMissingMessage(config.Provider));
            return false;
        }

        config.ApiKey = GetPersistableLlmApiKey(config.Provider, config.ApiKey, savedApiKey);
        config.Save(mw.Set);
        UpdateLlmConfigSummary(config);
        return true;
    }

    private void LoadActionLinkConfig(LlmActionLinkConfig config)
    {
        config.InitializeDefaultActions(LlmActionDescriptor.FromGraphCore(mw.Core?.Graph));
        cbActionLinkEnabled.IsChecked = config.Enabled;
        cbActionLinkAutoEnabled.IsChecked = config.AutoActionsEnabled;
        cbActionLinkManualEnabled.IsChecked = config.ManualActionsEnabled;
        cbActionLinkSkipBusy.IsChecked = config.SkipWhenBusy;
        cbActionLinkMoveChatter.IsChecked = config.MoveChatterEnabled;
        numActionLinkMoveMinSeconds.Value = config.MoveChatterMinSeconds;
        numActionLinkMoveMaxSeconds.Value = config.MoveChatterMaxSeconds;
        cbActionLinkDebugLog.IsChecked = config.DebugLogEnabled;
        BuildActionLinkList(config);
    }

    private LlmActionLinkConfig ReadActionLinkConfig()
    {
        var config = new LlmActionLinkConfig
        {
            Enabled = cbActionLinkEnabled.IsChecked == true,
            AutoActionsEnabled = cbActionLinkAutoEnabled.IsChecked == true,
            ManualActionsEnabled = cbActionLinkManualEnabled.IsChecked == true,
            SkipWhenBusy = cbActionLinkSkipBusy.IsChecked == true,
            MoveChatterEnabled = cbActionLinkMoveChatter.IsChecked == true,
            MoveChatterMinSeconds = (int)(numActionLinkMoveMinSeconds.Value ?? 35),
            MoveChatterMaxSeconds = (int)(numActionLinkMoveMaxSeconds.Value ?? 95),
            DebugLogEnabled = cbActionLinkDebugLog.IsChecked == true,
            Initialized = true
        };
        if (config.MoveChatterMaxSeconds < config.MoveChatterMinSeconds)
            config.MoveChatterMaxSeconds = config.MoveChatterMinSeconds;
        foreach (var pair in actionLinkChecks)
        {
            if (pair.Value.IsChecked == true && pair.Value.IsEnabled)
                config.EnabledActionKeys.Add(pair.Key);
        }
        return config;
    }

    private void SaveActionLinkConfig()
    {
        var config = ReadActionLinkConfig();
        config.Save(mw.Set);
    }

    private void BuildActionLinkList(LlmActionLinkConfig config)
    {
        actionLinkChecks.Clear();
        icActionLinkActions.Items.Clear();
        foreach (var action in LlmActionDescriptor.FromGraphCore(mw.Core?.Graph))
        {
            var row = CreateActionLinkRow(action, config.IsActionEnabled(action.Key));
            icActionLinkActions.Items.Add(row);
        }
    }

    private FrameworkElement CreateActionLinkRow(LlmActionDescriptor action, bool isChecked)
    {
        var grid = new Grid
        {
            MinHeight = 42,
            MaxWidth = 878
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(245) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var checkBox = new CheckBox
        {
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsEnabled = action.CanTrigger,
            IsChecked = action.CanTrigger && isChecked
        };
        actionLinkChecks[action.Key] = checkBox;
        grid.Children.Add(checkBox);

        AddActionLinkText(grid, 1, TranslateGraphType(action.Type), action.Type.ToString());
        AddActionLinkText(grid, 2, TranslateActionName(action), string.IsNullOrWhiteSpace(action.Name) ? "" : "资源名: {0}".Translate(action.Name));
        AddActionLinkText(grid, 3, string.Join(", ", action.Animats.Select(TranslateAnimatType)),
            string.Join(", ", action.Animats.Select(x => x.ToString())));
        string modes = string.Join(", ", action.Modes.Select(TranslateModeType));
        string rawModes = string.Join(", ", action.Modes.Select(x => x.ToString()));
        AddActionLinkText(grid, 4, GetActionStatusText(action, modes), action.CanTrigger ? rawModes : "");

        if (!action.CanTrigger)
            grid.Opacity = 0.65;

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid
        };
    }

    private static void AddActionLinkText(Grid grid, int column, string text, string detail = "")
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(8, 4, 8, 4),
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(new TextBlock
        {
            Text = text ?? "",
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        if (!string.IsNullOrWhiteSpace(detail) && detail != text)
        {
            panel.Children.Add(new TextBlock
            {
                Text = detail,
                FontSize = 12,
                Opacity = 0.62,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }
        Grid.SetColumn(panel, column);
        grid.Children.Add(panel);
    }

    private static string TranslateGraphType(GraphType type)
    {
        return type switch
        {
            GraphType.Raised_Dynamic => "提起移动".Translate(),
            GraphType.Raised_Static => "提起静止".Translate(),
            GraphType.Common => "通用动作".Translate(),
            GraphType.Move => "移动".Translate(),
            GraphType.Default => "默认呼吸".Translate(),
            GraphType.Touch_Head => "摸头".Translate(),
            GraphType.Touch_Body => "摸身体".Translate(),
            GraphType.Idel => "空闲动作".Translate(),
            GraphType.Sleep => "睡觉".Translate(),
            GraphType.Say => "说话".Translate(),
            GraphType.StateONE => "待机一段".Translate(),
            GraphType.StateTWO => "待机二段".Translate(),
            GraphType.StartUP => "开机".Translate(),
            GraphType.Shutdown => "关机".Translate(),
            GraphType.Work => "工作/学习".Translate(),
            GraphType.Switch_Up => "状态变好".Translate(),
            GraphType.Switch_Down => "状态变差".Translate(),
            GraphType.Switch_Thirsty => "口渴提示".Translate(),
            GraphType.Switch_Hunger => "饥饿提示".Translate(),
            GraphType.SideHide_Left_Main => "左侧躲藏".Translate(),
            GraphType.SideHide_Left_Rise => "左侧探出".Translate(),
            GraphType.SideHide_Right_Main => "右侧躲藏".Translate(),
            GraphType.SideHide_Right_Rise => "右侧探出".Translate(),
            _ => type.ToString().Translate()
        };
    }

    private static string GetActionStatusText(LlmActionDescriptor action, string modes)
    {
        if (action.IsReserved)
            return "系统保留, 不触发".Translate();
        if (action.Type == GraphType.Move)
            return "移动碎碎念单独控制".Translate();
        return modes;
    }

    private static string TranslateAnimatType(AnimatType type)
    {
        return type switch
        {
            AnimatType.Single => "单段".Translate(),
            AnimatType.A_Start => "开始".Translate(),
            AnimatType.B_Loop => "循环".Translate(),
            AnimatType.C_End => "结束".Translate(),
            _ => type.ToString().Translate()
        };
    }

    private static string TranslateModeType(VPet_Simulator.Core.IGameSave.ModeType mode)
    {
        return mode.ToString().Translate();
    }

    private static string TranslateActionName(LlmActionDescriptor action)
    {
        string name = action.Name ?? "";
        string translated = KnownActionName(name);
        if (!string.IsNullOrWhiteSpace(translated))
            return translated.Translate();
        string localized = name.Translate();
        if (!string.Equals(localized, name, StringComparison.OrdinalIgnoreCase))
            return localized;
        return "自定义动作".Translate();
    }

    private static string KnownActionName(string name)
    {
        return (name ?? "").Trim().ToLowerInvariant() switch
        {
            "amusement" => "找乐子",
            "aside" => "侧身发呆",
            "boring" => "无聊",
            "bubbles" => "吹泡泡",
            "like520" => "好感彩蛋",
            "meow" => "喵喵",
            "meowlook" => "喵喵看",
            "squat" => "蹲下",
            "tonic" => "伸懒腰",
            "sleep" => "睡觉",
            "walk" => "走路",
            "run" => "跑动",
            "raise" => "提起",
            "think" => "思考",
            "pinch" => "捏脸",
            "music" => "听音乐",
            "drink" => "喝水",
            "eat" => "吃饭",
            "gift" => "收礼物",
            "work" => "工作",
            "study" => "学习",
            _ => ""
        };
    }

    private void LoadMiMoVoiceConfig(MiMoVoiceConfig config)
    {
        cbMimoTtsEnabled.IsChecked = config.TtsEnabled;
        cbMimoAsrEnabled.IsChecked = config.AsrEnabled;
        pbMimoApiKeyTts.Password = LlmEnvironmentKeys.ResolveMiMoVoiceApiKey(config.ApiKey);
        pbMimoApiKeyAsr.Password = LlmEnvironmentKeys.ResolveMiMoVoiceApiKey(config.ApiKey);
        tbMimoBaseUrl.Text = config.BaseUrl;
        tbMimoTtsModel.Text = config.TtsModel;
        tbMimoTtsStylePrompt.Text = config.TtsStylePrompt;
        cbMimoTtsVoice.Text = config.TtsVoice;
        cbMimoTtsSegmentEnabled.IsChecked = config.TtsSegmentEnabled;
        numMimoTtsMaxSentences.Value = config.TtsMaxSentences;
        numMimoTtsSoftMaxChars.Value = config.TtsSoftMaxChars;
        cbMimoTtsCutAtComma.IsChecked = config.TtsCutAtCommaAfterLimit;
        tbMimoAsrModel.Text = config.AsrModel;
        LoadAsrInputDevices(config);
        SelectComboBoxByTag(cbMimoAsrLanguage, config.AsrLanguage);
        SelectComboBoxByTag(cbMimoAsrSubmitMode, config.AsrSubmitMode.ToString());
        tbMimoPushToTalkHotkey.Text = config.PushToTalkHotkey;
    }

    private MiMoVoiceConfig ReadMiMoVoiceConfig()
    {
        string apiKey = !string.IsNullOrWhiteSpace(pbMimoApiKeyTts.Password)
            ? pbMimoApiKeyTts.Password
            : pbMimoApiKeyAsr.Password;
        var config = new MiMoVoiceConfig
        {
            ApiKey = LlmEnvironmentKeys.ResolveMiMoVoiceApiKey(apiKey),
            BaseUrl = string.IsNullOrWhiteSpace(tbMimoBaseUrl.Text) ? "https://api.xiaomimimo.com/v1" : tbMimoBaseUrl.Text.Trim(),
            TimeoutSeconds = (int)(numLlmTimeoutSeconds.Value ?? 60),
            TtsEnabled = cbMimoTtsEnabled.IsChecked == true,
            TtsModel = string.IsNullOrWhiteSpace(tbMimoTtsModel.Text) ? "mimo-v2.5-tts" : tbMimoTtsModel.Text.Trim(),
            TtsVoice = string.IsNullOrWhiteSpace(cbMimoTtsVoice.Text) ? "冰糖" : cbMimoTtsVoice.Text.Trim(),
            TtsStylePrompt = tbMimoTtsStylePrompt.Text ?? "",
            TtsSegmentEnabled = cbMimoTtsSegmentEnabled.IsChecked == true,
            TtsMaxSentences = (int)(numMimoTtsMaxSentences.Value ?? 2),
            TtsSoftMaxChars = (int)(numMimoTtsSoftMaxChars.Value ?? 80),
            TtsCutAtCommaAfterLimit = cbMimoTtsCutAtComma.IsChecked == true,
            AsrEnabled = cbMimoAsrEnabled.IsChecked == true,
            AsrModel = string.IsNullOrWhiteSpace(tbMimoAsrModel.Text) ? "mimo-v2.5-asr" : tbMimoAsrModel.Text.Trim(),
            AsrLanguage = GetSelectedTag(cbMimoAsrLanguage, "auto"),
            AsrInputDeviceNumber = GetSelectedAsrInputDevice().DeviceNumber,
            AsrInputDeviceName = GetSelectedAsrInputDevice().Name,
            PushToTalkHotkey = string.IsNullOrWhiteSpace(tbMimoPushToTalkHotkey.Text)
                ? "Ctrl+Alt+M"
                : tbMimoPushToTalkHotkey.Text.Trim()
        };
        if (Enum.TryParse(GetSelectedTag(cbMimoAsrSubmitMode, "FillInput"), true, out MiMoAsrSubmitMode submitMode))
            config.AsrSubmitMode = submitMode;
        return config;
    }

    private bool TrySaveMiMoVoiceConfig(bool showMessage)
    {
        var config = ReadMiMoVoiceConfig();
        string savedApiKey = mw.Set["MiMoVoice"].GetString("apiKey", "");
        if ((config.TtsEnabled || config.AsrEnabled) && string.IsNullOrWhiteSpace(config.ApiKey))
        {
            if (showMessage)
                MessageBoxX.Show("请先填写 MiMo API Key, 或设置环境变量 MIMO_API / MIMO_API_KEY 后重启应用。".Translate());
            return false;
        }
        if (config.TtsEnabled && string.IsNullOrWhiteSpace(config.TtsModel))
        {
            if (showMessage)
                MessageBoxX.Show("请先填写语音合成模型".Translate());
            return false;
        }
        if (config.AsrEnabled && string.IsNullOrWhiteSpace(config.AsrModel))
        {
            if (showMessage)
                MessageBoxX.Show("请先填写语音识别模型".Translate());
            return false;
        }

        config.ApiKey = GetPersistableMiMoApiKey(config.ApiKey, savedApiKey);
        config.Save(mw.Set);
        pbMimoApiKeyTts.Password = LlmEnvironmentKeys.ResolveMiMoVoiceApiKey(config.ApiKey);
        pbMimoApiKeyAsr.Password = LlmEnvironmentKeys.ResolveMiMoVoiceApiKey(config.ApiKey);
        if (mw.TalkBox is LlmTalkBox llmTalkBox)
            llmTalkBox.RefreshVoiceConfig();
        return true;
    }

    private void UpdateProviderControls(LlmChatProvider provider)
    {
        bool isOllama = provider == LlmChatProvider.Ollama;
        pbLlmApiKey.IsEnabled = !isOllama;
        if (isOllama)
            pbLlmApiKey.Password = "";
        else if (string.IsNullOrWhiteSpace(pbLlmApiKey.Password))
            pbLlmApiKey.Password = LlmEnvironmentKeys.ResolveApiKey(provider, "");
        gridOllamaOptions.Visibility = isOllama ? Visibility.Visible : Visibility.Collapsed;
        BtnPreloadOllama.Visibility = isOllama ? Visibility.Visible : Visibility.Collapsed;
        BtnUnloadOllama.Visibility = isOllama ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetLlmModelOptions(LlmChatProvider provider, string selectedModel)
    {
        cbLlmModel.Items.Clear();
        foreach (string model in GetLlmModelOptions(provider))
            cbLlmModel.Items.Add(new ComboBoxItem { Content = model });
        cbLlmModel.Text = string.IsNullOrWhiteSpace(selectedModel)
            ? LlmChatConfig.DefaultModel(provider)
            : selectedModel;
    }

    private static string[] GetLlmModelOptions(LlmChatProvider provider)
    {
        return provider switch
        {
            LlmChatProvider.DeepSeek => new[]
            {
                "deepseek-v4-flash",
                "deepseek-v4-pro"
            },
            LlmChatProvider.MiMo => new[]
            {
                "mimo-v2.5-pro",
                "mimo-v2.5"
            },
            _ => new[]
            {
                "qwen3:30b-instruct",
                "qwen3.5:35b"
            }
        };
    }

    private void CheatNumber_ValueChanged(object sender, Panuon.WPF.SelectedValueChangedRoutedEventArgs<double?> e)
    {
        if (!allowChange)
            return;
        ApplyCheatValues();
    }

    private void LoadCheatValues()
    {
        var save = mw.GameSavesData.GameSave;
        numCheatMoney.Value = Math.Min(Math.Max(save.Money, 0), 1000000000);
        numCheatExp.Value = Math.Min(Math.Max(save.Exp, 0), 100000000);
        numCheatLevelMax.Value = Math.Min(Math.Max(save.LevelMax, 0), 100);
        numCheatStrength.Value = Math.Min(Math.Max(save.Strength, 0), 100000);
        numCheatStrengthFood.Value = Math.Min(Math.Max(save.StrengthFood, 0), 100000);
        numCheatStrengthDrink.Value = Math.Min(Math.Max(save.StrengthDrink, 0), 100000);
        numCheatFeeling.Value = Math.Min(Math.Max(save.Feeling, 0), 100000);
        numCheatHealth.Value = Math.Min(Math.Max(save.Health, 0), 100);
        numCheatLikability.Value = Math.Min(Math.Max(save.Likability, 0), 100000);
        numCheatLikabilityMax.Value = Math.Min(Math.Max(save.LikabilityMax, 0), 100000);
    }

    private void ApplyCheatValues()
    {
        var save = mw.GameSavesData.GameSave;
        save.Money = ClampNumber(numCheatMoney.Value, 0, 1000000000);
        save.LevelMax = (int)ClampNumber(numCheatLevelMax.Value, 0, 100);
        save.Exp = ClampNumber(numCheatExp.Value, 0, 100000000);
        save.LikabilityMax = ClampNumber(numCheatLikabilityMax.Value, 0, 100000);
        save.Strength = ClampNumber(numCheatStrength.Value, 0, save.StrengthMax);
        save.StrengthFood = ClampNumber(numCheatStrengthFood.Value, 0, save.StrengthMax);
        save.StrengthDrink = ClampNumber(numCheatStrengthDrink.Value, 0, save.StrengthMax);
        save.Feeling = ClampNumber(numCheatFeeling.Value, 0, save.FeelingMax);
        save.Health = ClampNumber(numCheatHealth.Value, 0, 100);
        save.Likability = ClampNumber(numCheatLikability.Value, 0, save.LikabilityMax);
        save.Mode = save.CalMode();
        mw.Core.Save = save;
    }

    private static double ClampNumber(double? value, double min, double max)
    {
        if (value == null || double.IsNaN(value.Value))
            return min;
        return Math.Min(Math.Max(value.Value, min), max);
    }

    private void CGPType_Checked(object sender, RoutedEventArgs e)
    {
        if (!allowChange)
            return;
        if (RBCGPTUseLB.IsChecked == true)
        {
            mw.Set["CGPT"][(gstr)"type"] = "LB";
        }
        else if (RBCGPTDIY.IsChecked == true)
        {
            mw.Set["CGPT"][(gstr)"type"] = "DIY";
        }
        else if (RBCGPTUseAPI.IsChecked == true)
        {
            if (!TrySaveLlmConfig(true))
                return;
            mw.Set["CGPT"][(gstr)"type"] = "API";
        }
        else
        {
            mw.Set["CGPT"][(gstr)"type"] = "OFF";
        }

        ApplyChatMode();
    }

    private void ApplyChatMode()
    {
        switch (mw.Set["CGPT"][(gstr)"type"])
        {
            case "API":
                mw.LoadTalkLlm();
                mw.LoadDIY();
                break;
            case "DIY":
                mw.RemoveTalkBox();
                mw.LoadTalkDIY();
                mw.LoadDIY();
                break;
            case "LB":
                mw.RemoveTalkBox();
                mw.TalkBox = new TalkSelect(mw);
                mw.Main.ToolBar.MainGrid.Children.Add(mw.TalkBox);
                mw.LoadDIY();
                break;
            case "OFF":
            default:
                mw.RemoveTalkBox();
                mw.LoadDIY();
                break;
        }
    }

    private void UpdateLlmConfigSummary()
    {
        UpdateLlmConfigSummary(LlmChatConfig.Load(mw.Set));
    }

    private void UpdateLlmConfigSummary(LlmChatConfig config)
    {
        tbLlmConfigSummary.Text = "{0} / {1}".Translate(config.Provider.ToString(),
            string.IsNullOrWhiteSpace(config.Model) ? "未设置模型".Translate() : config.Model);
    }

    private void cbChatAPISelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!allowChange)
            return;
        mw.TalkAPIIndex = cbChatAPISelect.SelectedIndex;
        mw.Set["CGPT"][(gstr)"DIY"] = mw.TalkBoxCurr?.APIName ?? "";
        if (RBCGPTDIY.IsChecked == true)
            mw.LoadTalkDIY();
    }

    private void cbLlmProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!allowProviderChange)
            return;

        var provider = GetSelectedProvider();
        tbLlmBaseUrl.Text = LlmChatConfig.DefaultBaseUrl(provider);
        SetLlmModelOptions(provider, LlmChatConfig.DefaultModel(provider));
        pbLlmApiKey.Password = provider == LlmChatProvider.Ollama ? "" : LlmEnvironmentKeys.ResolveApiKey(provider, "");
        UpdateProviderControls(provider);
        UpdateLlmConfigSummary(new LlmChatConfig
        {
            Provider = provider,
            Model = cbLlmModel.Text
        });
    }

    private async void BtnPreloadOllama_Click(object sender, RoutedEventArgs e)
    {
        var config = ReadLlmConfig();
        if (config.Provider != LlmChatProvider.Ollama)
            return;
        if (string.IsNullOrWhiteSpace(config.Model))
        {
            MessageBoxX.Show("请先填写模型名称".Translate());
            return;
        }

        BtnPreloadOllama.IsEnabled = false;
        string oldText = BtnPreloadOllama.Content?.ToString() ?? "";
        BtnPreloadOllama.Content = "装载中".Translate();
        try
        {
            config.Save(mw.Set);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, config.TimeoutSeconds + 5)));
            await Task.Run(() => new OllamaChatClient().PreloadAsync(config, cts.Token));
            UpdateLlmConfigSummary(config);
            MessageBoxX.Show("Ollama 模型已装载".Translate());
        }
        catch (Exception ex)
        {
            MessageBoxX.Show("Ollama 模型装载失败: {0}".Translate(GetFriendlyMessage(ex)));
        }
        finally
        {
            BtnPreloadOllama.Content = oldText;
            BtnPreloadOllama.IsEnabled = true;
        }
    }

    private async void BtnUnloadOllama_Click(object sender, RoutedEventArgs e)
    {
        var config = ReadLlmConfig();
        if (config.Provider != LlmChatProvider.Ollama)
            return;
        if (string.IsNullOrWhiteSpace(config.Model))
        {
            MessageBoxX.Show("请先填写模型名称".Translate());
            return;
        }

        BtnUnloadOllama.IsEnabled = false;
        string oldText = BtnUnloadOllama.Content?.ToString() ?? "";
        BtnUnloadOllama.Content = "卸载中".Translate();
        try
        {
            config.Save(mw.Set);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, config.TimeoutSeconds + 5)));
            await Task.Run(() => new OllamaChatClient().UnloadAsync(config, cts.Token));
            UpdateLlmConfigSummary(config);
            MessageBoxX.Show("Ollama 模型已卸载".Translate());
        }
        catch (Exception ex)
        {
            MessageBoxX.Show("Ollama 模型卸载失败: {0}".Translate(GetFriendlyMessage(ex)));
        }
        finally
        {
            BtnUnloadOllama.Content = oldText;
            BtnUnloadOllama.IsEnabled = true;
        }
    }

    private static string GetFriendlyMessage(Exception ex)
    {
        string message = ex.InnerException?.Message ?? ex.Message;
        if (string.IsNullOrWhiteSpace(message))
            return "未知错误";
        return message.Length > 180 ? message[..180] : message;
    }

    private async void BtnTestMimoTts_Click(object sender, RoutedEventArgs e)
    {
        var config = ReadMiMoVoiceConfig();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            MessageBoxX.Show("请先填写 MiMo API Key".Translate());
            return;
        }

        BtnTestMimoTts.IsEnabled = false;
        string oldText = BtnTestMimoTts.Content?.ToString() ?? "";
        BtnTestMimoTts.Content = "合成中".Translate();
        try
        {
            config.Save(mw.Set);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds, 5, 600)));
            string path = await new MiMoAudioClient().SynthesizeToWavFileAsync(config,
                "这是一段小米 MiMo 语音合成测试。".Translate(), cts.Token);
            await PlayTestAudioAsync(path);
            TryDelete(path);
        }
        catch (Exception ex)
        {
            MessageBoxX.Show("MiMo 语音合成失败: {0}".Translate(GetFriendlyMessage(ex)));
        }
        finally
        {
            BtnTestMimoTts.Content = oldText;
            BtnTestMimoTts.IsEnabled = true;
        }
    }

    private void BtnTestMimoAsr_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (isTestingAsr)
            return;
        var config = ReadMiMoVoiceConfig();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            MessageBoxX.Show("请先填写 MiMo API Key".Translate());
            return;
        }

        try
        {
            config.Save(mw.Set);
            ClearCachedAsrTestRecording();
            testAsrRecorder = new MiMoAsrRecorder(config.AsrInputDeviceNumber);
            testAsrRecorder.Start();
            isTestingAsr = true;
            BtnTestMimoAsr.CaptureMouse();
            tbMimoAsrStatus.Text = "录音中...".Translate();
            e.Handled = true;
        }
        catch (Exception ex)
        {
            testAsrRecorder?.Dispose();
            testAsrRecorder = null;
            isTestingAsr = false;
            MessageBoxX.Show("语音录制失败: {0}".Translate(GetFriendlyMessage(ex)));
        }
    }

    private void BtnTestMimoAsr_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isTestingAsr)
            return;
        e.Handled = true;
        StopTestAsrRecording();
    }

    private void BtnTestMimoAsr_MouseLeave(object sender, MouseEventArgs e)
    {
        if (isTestingAsr && e.LeftButton == MouseButtonState.Released)
            StopTestAsrRecording();
    }

    private void tbMimoPushToTalkHotkey_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Tab or Key.Enter or Key.Escape)
            return;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            e.Handled = true;
            return;
        }

        var parts = new System.Collections.Generic.List<string>();
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
            parts.Add("Win");
        parts.Add(key.ToString());
        tbMimoPushToTalkHotkey.Text = string.Join("+", parts);
        tbMimoPushToTalkHotkey.CaretIndex = tbMimoPushToTalkHotkey.Text.Length;
        e.Handled = true;
    }

    private void StopTestAsrRecording()
    {
        if (!isTestingAsr || testAsrRecorder == null)
            return;
        isTestingAsr = false;
        if (BtnTestMimoAsr.IsMouseCaptured)
            BtnTestMimoAsr.ReleaseMouseCapture();

        string path;
        try
        {
            path = testAsrRecorder.Stop();
        }
        catch (Exception ex)
        {
            testAsrRecorder.Dispose();
            testAsrRecorder = null;
            tbMimoAsrStatus.Text = "录音失败: {0}".Translate(GetFriendlyMessage(ex));
            return;
        }
        testAsrRecorder.Dispose();
        testAsrRecorder = null;

        cachedAsrTestPath = path;
        BtnPlayMimoAsrCache.IsEnabled = true;
        BtnRecognizeMimoAsrCache.IsEnabled = true;
        tbMimoAsrStatus.Text = "录音已缓存, 可播放确认或识别。".Translate();
    }

    private async void BtnPlayMimoAsrCache_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(cachedAsrTestPath) || !File.Exists(cachedAsrTestPath))
        {
            tbMimoAsrStatus.Text = "没有可播放的录音缓存".Translate();
            BtnPlayMimoAsrCache.IsEnabled = false;
            BtnRecognizeMimoAsrCache.IsEnabled = false;
            return;
        }

        BtnPlayMimoAsrCache.IsEnabled = false;
        string oldText = BtnPlayMimoAsrCache.Content?.ToString() ?? "";
        BtnPlayMimoAsrCache.Content = "播放中".Translate();
        try
        {
            await PlayTestAudioAsync(cachedAsrTestPath);
            tbMimoAsrStatus.Text = "录音播放完成, 可继续识别测试。".Translate();
        }
        catch (Exception ex)
        {
            tbMimoAsrStatus.Text = "播放失败: {0}".Translate(GetFriendlyMessage(ex));
        }
        finally
        {
            BtnPlayMimoAsrCache.Content = oldText;
            BtnPlayMimoAsrCache.IsEnabled = File.Exists(cachedAsrTestPath ?? "");
        }
    }

    private async void BtnRecognizeMimoAsrCache_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(cachedAsrTestPath) || !File.Exists(cachedAsrTestPath))
        {
            tbMimoAsrStatus.Text = "没有可识别的录音缓存".Translate();
            BtnPlayMimoAsrCache.IsEnabled = false;
            BtnRecognizeMimoAsrCache.IsEnabled = false;
            return;
        }

        BtnTestMimoAsr.IsEnabled = false;
        BtnPlayMimoAsrCache.IsEnabled = false;
        BtnRecognizeMimoAsrCache.IsEnabled = false;
        tbMimoAsrStatus.Text = "识别中...".Translate();
        try
        {
            var config = ReadMiMoVoiceConfig();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds, 5, 600)));
            string recognized = await new MiMoAudioClient().RecognizeAsync(config, cachedAsrTestPath, cts.Token);
            tbMimoAsrStatus.Text = string.IsNullOrWhiteSpace(recognized)
                ? "未识别到文本".Translate()
                : recognized;
        }
        catch (Exception ex)
        {
            tbMimoAsrStatus.Text = "识别失败: {0}".Translate(GetFriendlyMessage(ex));
        }
        finally
        {
            BtnTestMimoAsr.IsEnabled = true;
            bool hasCache = File.Exists(cachedAsrTestPath ?? "");
            BtnPlayMimoAsrCache.IsEnabled = hasCache;
            BtnRecognizeMimoAsrCache.IsEnabled = hasCache;
        }
    }

    private void ListMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListMenu.SelectedIndex >= 0 && MainTab.SelectedIndex != ListMenu.SelectedIndex)
            MainTab.SelectedIndex = ListMenu.SelectedIndex;
    }

    private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MainTab.SelectedIndex >= 0 && ListMenu.SelectedIndex != MainTab.SelectedIndex)
            ListMenu.SelectedIndex = MainTab.SelectedIndex;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        ApplyCheatValues();
        if (!TrySaveLlmConfig(true))
            return;
        SaveActionLinkConfig();
        if (!TrySaveMiMoVoiceConfig(true))
            return;
        mw.Save();
        MessageBoxX.Show("保存成功".Translate());
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void WindowX_Closed(object sender, EventArgs e)
    {
        if (isTestingAsr)
            StopTestAsrRecording();
        ClearCachedAsrTestRecording();
        mw.winExtraSetting = null;
    }

    private static void SelectComboBoxByTag(ComboBox comboBox, string tag)
    {
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is ComboBoxItem item
                && string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }
        if (comboBox.Items.Count > 0)
            comboBox.SelectedIndex = 0;
    }

    private static string GetSelectedTag(ComboBox comboBox, string fallback)
    {
        return comboBox.SelectedItem is ComboBoxItem item && item.Tag != null
            ? item.Tag.ToString()
            : fallback;
    }

    private static string GetLlmApiKeyMissingMessage(LlmChatProvider provider)
    {
        return provider switch
        {
            LlmChatProvider.DeepSeek => "请先填写 DeepSeek API Key, 或设置环境变量 DS_API / DEEPSEEK_API_KEY 后重启应用。".Translate(),
            LlmChatProvider.MiMo => "请先填写 MiMo API Key, 或设置环境变量 MIMO_API / MIMO_API_KEY 后重启应用。".Translate(),
            _ => "请先填写 API Key".Translate()
        };
    }

    private void LoadAsrInputDevices(MiMoVoiceConfig config)
    {
        cbMimoAsrInputDevice.Items.Clear();
        cbMimoAsrInputDevice.Items.Add(new AsrInputDeviceItem(-1, GetDefaultAsrInputDeviceLabel()));
        int selectedIndex = 0;
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            string name;
            try
            {
                name = WaveInEvent.GetCapabilities(i).ProductName;
            }
            catch
            {
                name = "麦克风 {0}".Translate(i + 1);
            }
            var item = new AsrInputDeviceItem(i, name);
            cbMimoAsrInputDevice.Items.Add(item);
            if (i == config.AsrInputDeviceNumber
                || !string.IsNullOrWhiteSpace(config.AsrInputDeviceName)
                && string.Equals(config.AsrInputDeviceName, name, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = cbMimoAsrInputDevice.Items.Count - 1;
            }
        }
        cbMimoAsrInputDevice.SelectedIndex = selectedIndex;
    }

    private AsrInputDeviceItem GetSelectedAsrInputDevice()
    {
        return cbMimoAsrInputDevice.SelectedItem as AsrInputDeviceItem
            ?? new AsrInputDeviceItem(-1, GetDefaultAsrInputDeviceLabel());
    }

    private static string GetDefaultAsrInputDeviceLabel()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            string name = device?.FriendlyName;
            if (!string.IsNullOrWhiteSpace(name))
                return "系统默认 ({0})".Translate(name);
        }
        catch
        {
        }
        return "系统默认".Translate();
    }

    private static string GetPersistableLlmApiKey(LlmChatProvider provider, string apiKey, string savedApiKey)
    {
        if (provider == LlmChatProvider.DeepSeek
            && string.IsNullOrWhiteSpace(savedApiKey)
            && apiKey == LlmEnvironmentKeys.GetDeepSeekApiKey())
        {
            return "";
        }
        if (provider == LlmChatProvider.MiMo
            && string.IsNullOrWhiteSpace(savedApiKey)
            && apiKey == LlmEnvironmentKeys.GetMiMoApiKey())
        {
            return "";
        }
        return apiKey ?? "";
    }

    private static string GetPersistableMiMoApiKey(string apiKey, string savedApiKey)
    {
        if (string.IsNullOrWhiteSpace(savedApiKey)
            && apiKey == LlmEnvironmentKeys.GetMiMoApiKey())
        {
            return "";
        }
        return apiKey ?? "";
    }

    private async Task PlayTestAudioAsync(string path)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var player = new MediaPlayer
        {
            Volume = mw.Set.VoiceVolume
        };
        player.MediaEnded += (_, _) =>
        {
            player.Close();
            tcs.TrySetResult();
        };
        player.MediaFailed += (_, args) =>
        {
            player.Close();
            tcs.TrySetException(args.ErrorException ?? new InvalidOperationException("语音播放失败".Translate()));
        };
        player.Open(new Uri(path, UriKind.Absolute));
        player.Play();
        await tcs.Task;
    }

    private void ClearCachedAsrTestRecording()
    {
        if (!string.IsNullOrWhiteSpace(cachedAsrTestPath))
            TryDelete(cachedAsrTestPath);
        cachedAsrTestPath = "";
        if (BtnPlayMimoAsrCache != null)
            BtnPlayMimoAsrCache.IsEnabled = false;
        if (BtnRecognizeMimoAsrCache != null)
            BtnRecognizeMimoAsrCache.IsEnabled = false;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private sealed class AsrInputDeviceItem
    {
        public AsrInputDeviceItem(int deviceNumber, string name)
        {
            DeviceNumber = deviceNumber;
            Name = name ?? "";
        }

        public int DeviceNumber { get; }
        public string Name { get; }

        public override string ToString()
        {
            return DeviceNumber < 0 ? Name : $"{Name} ({DeviceNumber})";
        }
    }
}
