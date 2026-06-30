using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Windows;
using System.Windows.Controls;

namespace VPet_Simulator.Windows;

public partial class winLlmChatSetting : WindowX
{
    private readonly MainWindow mw;
    private bool allowProviderChange;

    public winLlmChatSetting(MainWindow mw)
    {
        this.mw = mw;
        InitializeComponent();
        LoadConfig(LlmChatConfig.Load(mw.Set));
    }

    private void LoadConfig(LlmChatConfig config)
    {
        allowProviderChange = false;
        var provider = config.Provider == LlmChatProvider.DeepSeek ? LlmChatProvider.DeepSeek : LlmChatProvider.Ollama;
        bool providerChanged = config.Provider != provider;
        for (int i = 0; i < cbProvider.Items.Count; i++)
        {
            if (cbProvider.Items[i] is ComboBoxItem item && item.Tag?.ToString() == provider.ToString())
            {
                cbProvider.SelectedIndex = i;
                break;
            }
        }
        tbBaseUrl.Text = providerChanged || string.IsNullOrWhiteSpace(config.BaseUrl) ? LlmChatConfig.DefaultBaseUrl(provider) : config.BaseUrl;
        tbModel.Text = providerChanged || string.IsNullOrWhiteSpace(config.Model) ? LlmChatConfig.DefaultModel(provider) : config.Model;
        pbApiKey.Password = provider == LlmChatProvider.Ollama ? "" : config.ApiKey;
        tbSystemPrompt.Text = config.SystemPrompt;
        numTemperature.Value = config.Temperature;
        numMaxTokens.Value = config.MaxTokens;
        numHistoryTurns.Value = config.HistoryTurns;
        numTimeoutSeconds.Value = config.TimeoutSeconds;
        allowProviderChange = true;
        UpdateApiKeyEnabled(provider);
    }

    private LlmChatConfig ReadConfig()
    {
        var provider = GetSelectedProvider();
        var oldConfig = LlmChatConfig.Load(mw.Set);
        return new LlmChatConfig
        {
            Provider = provider,
            BaseUrl = tbBaseUrl.Text,
            Model = tbModel.Text,
            ApiKey = provider == LlmChatProvider.Ollama ? "" : pbApiKey.Password,
            SystemPrompt = tbSystemPrompt.Text,
            Temperature = numTemperature.Value ?? 0.8,
            MaxTokens = (int)(numMaxTokens.Value ?? 1024),
            HistoryTurns = (int)(numHistoryTurns.Value ?? 8),
            TimeoutSeconds = (int)(numTimeoutSeconds.Value ?? 60),
            OllamaPreloadBeforeSend = oldConfig.OllamaPreloadBeforeSend,
            OllamaKeepAliveMinutes = oldConfig.OllamaKeepAliveMinutes,
            OllamaContextLength = oldConfig.OllamaContextLength,
            OllamaThink = oldConfig.OllamaThink
        };
    }

    private LlmChatProvider GetSelectedProvider()
    {
        if (cbProvider.SelectedItem is ComboBoxItem item
            && Enum.TryParse(item.Tag?.ToString(), true, out LlmChatProvider provider)
            && provider == LlmChatProvider.DeepSeek)
        {
            return LlmChatProvider.DeepSeek;
        }
        return LlmChatProvider.Ollama;
    }

    private void cbProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!allowProviderChange)
            return;

        var provider = GetSelectedProvider();
        tbBaseUrl.Text = LlmChatConfig.DefaultBaseUrl(provider);
        tbModel.Text = LlmChatConfig.DefaultModel(provider);
        UpdateApiKeyEnabled(provider);
    }

    private void UpdateApiKeyEnabled(LlmChatProvider provider)
    {
        pbApiKey.IsEnabled = provider != LlmChatProvider.Ollama;
        if (provider == LlmChatProvider.Ollama)
            pbApiKey.Password = "";
    }

    private void BtnDefault_Click(object sender, RoutedEventArgs e)
    {
        var provider = GetSelectedProvider();
        LoadConfig(new LlmChatConfig
        {
            Provider = provider,
            BaseUrl = LlmChatConfig.DefaultBaseUrl(provider),
            Model = LlmChatConfig.DefaultModel(provider)
        });
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var config = ReadConfig();
        if (string.IsNullOrWhiteSpace(config.Model))
        {
            MessageBoxX.Show("请先填写模型名称".Translate());
            return;
        }
        if (config.Provider != LlmChatProvider.Ollama && string.IsNullOrWhiteSpace(config.ApiKey))
        {
            MessageBoxX.Show("请先填写 API Key".Translate());
            return;
        }
        config.Save(mw.Set);
        Close();
    }
}
