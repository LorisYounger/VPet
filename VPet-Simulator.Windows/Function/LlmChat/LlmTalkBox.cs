using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Windows;

internal class LlmTalkBox : TalkBox
{
    private readonly MainWindow mw;
    private readonly List<LlmChatMessage> history = new();
    private readonly Dictionary<LlmChatProvider, ILlmChatClient> clients = new();
    private readonly object historyLock = new();
    private readonly MiMoAudioClient voiceClient = new();
    private readonly PushToTalkHotkeyService pushToTalkHotkeyService;
    private LlmFloatingTalkWindow bubble;
    private MiMoAsrRecorder asrRecorder;
    private bool isResponding;
    private bool llmSpeaking;
    private bool isRecognizingVoice;
    private string lastAssistantReply = "";
    public bool IsResponding => isResponding;
    public DateTime LastResponseCompletedAt { get; private set; } = DateTime.MinValue;
    public event Action ResponseCompleted;

    public LlmTalkBox(MainWindow mw) : base(new BuiltInMainPlugin(mw))
    {
        this.mw = mw;
        MainGrid.Visibility = System.Windows.Visibility.Collapsed;
        Visibility = System.Windows.Visibility.Collapsed;
        pushToTalkHotkeyService = new PushToTalkHotkeyService(mw);
        pushToTalkHotkeyService.Pressed += () => mw.Dispatcher.Invoke(StartVoiceInput);
        pushToTalkHotkeyService.Released += () => mw.Dispatcher.Invoke(StopVoiceInput);
        RefreshVoiceConfig();
    }

    public override string APIName => "大模型 API 聊天";

    public override void Setting()
    {
        mw.Dispatcher.Invoke(() => mw.ShowExtraSetting(1));
    }

    public void ShowInput()
    {
        mw.Dispatcher.Invoke(() =>
        {
            var inputBubble = GetBubble();
            inputBubble.FocusInput();
            ScheduleBubbleAutoClose(inputBubble);
        });
    }

    public void CloseBubble()
    {
        mw.Dispatcher.Invoke(() => bubble?.Close());
    }

    public void DisposeServices()
    {
        pushToTalkHotkeyService.Dispose();
        asrRecorder?.Dispose();
        asrRecorder = null;
    }

    public void RefreshVoiceConfig()
    {
        var voiceConfig = MiMoVoiceConfig.Load(mw.Set);
        if (voiceConfig.AsrEnabled)
            pushToTalkHotkeyService.Register(voiceConfig.PushToTalkHotkey);
        else
            pushToTalkHotkeyService.Unregister();

        mw.Dispatcher.Invoke(() =>
        {
            if (bubble != null && !bubble.IsClosed)
                bubble.SetVoiceInputEnabled(voiceConfig.AsrEnabled);
        });
    }

    public override void Responded(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        StartResponse(text, null);
    }

    public bool TryRespondToAction(LlmActionTriggerContext context)
    {
        if (context?.GraphInfo == null)
            return false;
        if (isResponding)
        {
            return false;
        }
        StartResponse(context.BuildPrompt(mw), context, showThinking: false, outputOnly: true);
        return true;
    }

    public bool TryRespondToHiddenPrompt(string prompt, string logType = "llm_hidden")
    {
        if (string.IsNullOrWhiteSpace(prompt) || isResponding)
            return false;
        StartResponse(prompt, null, logType, showThinking: false, suppressSpeakAnimation: true, outputOnly: true);
        return true;
    }

    public bool TryShowPetSay(SayInfo sayInfo)
    {
        if (isResponding)
            return false;
        if (sayInfo is not SayInfoWithOutStream outStream)
            return false;
        if (outStream.MsgContent != null || outStream.Force)
            return false;
        if (string.IsNullOrWhiteSpace(outStream.Text))
            return false;

        var voiceConfig = MiMoVoiceConfig.Load(mw.Set);
        if (voiceConfig.TtsEnabled)
        {
            StartPetSayTts(outStream.Text);
            return true;
        }

        mw.Dispatcher.Invoke(() =>
        {
            var sayBubble = GetBubble();
            sayBubble.ShowPlainText(outStream.Text);
            ScheduleBubbleAutoClose(sayBubble);
        });
        return true;
    }

    private void StartPetSayTts(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || isResponding)
            return;

        isResponding = true;
        DateTime responseStartTime = DateTime.Now;
        lastAssistantReply = text;
        LlmFloatingTalkWindow currentBubble = null;
        var voiceConfig = MiMoVoiceConfig.Load(mw.Set);
        LlmActionDebugLog.Write(mw, "pet_say_tts_start", $"chars={text.Length}", $"text={TrimForLog(text)}");
        mw.Dispatcher.Invoke(() =>
        {
            currentBubble = GetBubble();
            currentBubble.ShowOutputOnly();
            currentBubble.BeginResponse(false);
            currentBubble.SetInputEnabled(false);
        });

        var ttsQueue = new MiMoTtsPlaybackQueue(mw, voiceConfig, "pet_say");
        bool speechStarted = false;
        ttsQueue.SegmentReady += displayText =>
        {
            if (!speechStarted)
            {
                speechStarted = true;
                StartSpeechOrAction();
            }
            if (currentBubble == null || currentBubble.IsClosed)
                return;
            if (string.IsNullOrWhiteSpace(currentBubble.OutputText))
                currentBubble.ReplaceText(displayText);
            else
                currentBubble.AppendText(displayText);
        };
        ttsQueue.ErrorOccurred += message =>
        {
            if (!speechStarted)
            {
                speechStarted = true;
                StartSpeechOrAction();
            }
            if (currentBubble != null && !currentBubble.IsClosed)
                currentBubble.ReplaceText("语音合成失败: {0}".Translate(message));
        };

        Task.Run(async () =>
        {
            try
            {
                ttsQueue.AddText(text);
                ttsQueue.Complete();
                using var closeCts = new CancellationTokenSource();
                await ttsQueue.WaitPlaybackCompleteAsync(closeCts.Token);
                if (currentBubble != null && !currentBubble.IsClosed)
                    await currentBubble.WaitForOutputCompleteAsync(closeCts.Token);
            }
            finally
            {
                mw.Dispatcher.Invoke(StopSpeakAnimation);
                LastResponseCompletedAt = DateTime.Now;
                isResponding = false;
                LlmActionDebugLog.Write(mw, "pet_say_tts_end",
                    $"elapsedMs={(DateTime.Now - responseStartTime).TotalMilliseconds:f0}",
                    $"chars={text.Length}");
                mw.Dispatcher.Invoke(() => ResponseCompleted?.Invoke());
                mw.Dispatcher.Invoke(() =>
                {
                    if (currentBubble != null && !currentBubble.IsClosed)
                    {
                        currentBubble.SetInputEnabled(true);
                        currentBubble.SetReplayEnabled(!string.IsNullOrWhiteSpace(lastAssistantReply)
                            && MiMoVoiceConfig.Load(mw.Set).TtsEnabled);
                        ScheduleBubbleAutoClose(currentBubble);
                    }
                });
            }
        });
    }

    private void StartResponse(string text, LlmActionTriggerContext actionContext, string directLogType = "hostsay",
        bool? showThinking = null, bool suppressSpeakAnimation = false, bool outputOnly = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        if (isResponding)
        {
            if (actionContext == null)
            {
                mw.Dispatcher.Invoke(() =>
                {
                    var busyBubble = GetBubble();
                    busyBubble.ReplaceText("我还在想上一句话哦, 等我说完再聊吧~".Translate());
                    busyBubble.FocusInput();
                });
            }
            return;
        }

        isResponding = true;
        DateTime responseStartTime = DateTime.Now;
        LlmActionDebugLog.Write(mw, "llm_response_start",
            actionContext == null ? $"type={directLogType}" : "type=action",
            actionContext == null ? "" : $"action={actionContext.ActionKey}",
            actionContext == null ? "" : $"interaction={actionContext.InteractionType}",
            $"outputOnly={outputOnly}",
            $"suppressSpeak={suppressSpeakAnimation}");
        LlmFloatingTalkWindow currentBubble = null;
        bool delayBubble = actionContext?.DelayBubbleUntilSpeech == true;
        if (!delayBubble)
        {
            mw.Dispatcher.Invoke(() =>
            {
                currentBubble = GetBubble();
                if (outputOnly)
                    currentBubble.ShowOutputOnly();
                else
                    currentBubble.SetInputPanelVisible(true);
                currentBubble.BeginResponse(showThinking ?? actionContext?.SuppressThinking != true);
                currentBubble.SetInputEnabled(false);
            });
        }
        mw.Dispatcher.Invoke(() => mw.ActivityLogs.Add(actionContext == null
            ? new ActivityLog(directLogType, text)
            : new ActivityLog("llm_action", actionContext.GraphInfo.ToString())));
        if (!outputOnly && !suppressSpeakAnimation && (actionContext == null || actionContext.IsAutomatic && !actionContext.SuppressThinking))
            mw.Dispatcher.Invoke(DisplayThink);

        LlmSpeechOutputGate speechGate = LlmSpeechOutputGate.Immediate;
        MiMoTtsPlaybackQueue ttsQueue = null;

        LlmFloatingTalkWindow EnsureResponseBubble(bool showResponseThinking)
        {
            if (currentBubble != null && !currentBubble.IsClosed)
                return currentBubble;
            currentBubble = GetBubble();
            if (outputOnly)
                currentBubble.ShowOutputOnly();
            else
                currentBubble.SetInputPanelVisible(true);
            currentBubble.BeginResponse(showResponseThinking);
            currentBubble.SetInputEnabled(false);
            return currentBubble;
        }

        Task.Run(async () =>
        {
            string result = "";
            bool speechStarted = false;
            try
            {
                LlmChatConfig config = LlmChatConfig.Load(mw.Set);
                MiMoVoiceConfig voiceConfig = MiMoVoiceConfig.Load(mw.Set);
                bool showingThinking = true;
                bool hasContent = false;
                if (voiceConfig.TtsEnabled)
                {
                    ttsQueue = new MiMoTtsPlaybackQueue(mw, voiceConfig, BuildTtsLogContext(actionContext, directLogType));
                    ttsQueue.SegmentReady += displayText =>
                    {
                        if (!speechStarted)
                        {
                            speechStarted = true;
                            StartSpeechOrAction(actionContext, suppressSpeakAnimation);
                        }
                        currentBubble = EnsureResponseBubble(false);
                        if (currentBubble == null || currentBubble.IsClosed)
                            return;
                        if (showingThinking)
                        {
                            showingThinking = false;
                            hasContent = true;
                            currentBubble.ReplaceText(displayText);
                        }
                        else
                        {
                            hasContent = true;
                            currentBubble.AppendText(displayText);
                        }
                    };
                    ttsQueue.ErrorOccurred += message =>
                    {
                        currentBubble = EnsureResponseBubble(false);
                        if (currentBubble != null && !currentBubble.IsClosed)
                        {
                            if (!speechStarted)
                            {
                                speechStarted = true;
                                StartSpeechOrAction(actionContext, suppressSpeakAnimation);
                            }
                            currentBubble.ReplaceText("语音合成失败: {0}".Translate(message));
                        }
                    };
                    speechGate = ttsQueue;
                }
                ILlmChatClient client = GetClient(config.Provider);
                IReadOnlyList<LlmChatMessage> copiedHistory;
                lock (historyLock)
                {
                    int take = Math.Max(0, config.HistoryTurns) * 2;
                    copiedHistory = take == 0
                        ? Array.Empty<LlmChatMessage>()
                        : history.TakeLast(take).ToArray();
                }
                LlmActionDebugLog.Write(mw, "llm_request",
                    $"provider={config.Provider}",
                    $"model={config.Model}",
                    $"tts={voiceConfig.TtsEnabled}",
                    $"history={copiedHistory.Count}");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, config.TimeoutSeconds + 5)));
                result = await client.StreamChatAsync(
                    config,
                    copiedHistory,
                    text,
                    config.GetSystemPrompt(mw),
                    cts.Token,
                    (delta, isThinking) => mw.Dispatcher.Invoke(() =>
                    {
                        if (isThinking)
                        {
                            if (currentBubble != null && !currentBubble.IsClosed && !showingThinking && !hasContent)
                            {
                                showingThinking = true;
                                currentBubble.SetThinking();
                            }
                            return;
                        }

                        if (ttsQueue != null)
                        {
                            ttsQueue.AddText(delta);
                            return;
                        }

                        if (!speechStarted)
                        {
                            speechStarted = true;
                            StartSpeechOrAction(actionContext, suppressSpeakAnimation);
                        }
                        currentBubble = EnsureResponseBubble(false);
                        if (currentBubble == null || currentBubble.IsClosed)
                            return;

                        if (showingThinking)
                        {
                            showingThinking = false;
                            hasContent = true;
                            currentBubble.ReplaceText(delta);
                        }
                        else
                        {
                            hasContent = true;
                            currentBubble.AppendText(delta);
                        }
                    }));

                LlmActionDebugLog.Write(mw, "llm_response_stream_done", $"chars={result?.Length ?? 0}");

                if (string.IsNullOrWhiteSpace(result))
                {
                    result = "模型没有返回内容, 可以检查模型名称和接口地址哦~".Translate();
                    if (ttsQueue != null)
                    {
                        ttsQueue.AddText(result);
                    }
                    else
                    {
                        mw.Dispatcher.Invoke(() =>
                        {
                            currentBubble = EnsureResponseBubble(false);
                            if (currentBubble == null || currentBubble.IsClosed)
                            {
                                if (actionContext?.PlayAction != null && !speechStarted)
                                {
                                    speechStarted = true;
                                    StartSpeechOrAction(actionContext, suppressSpeakAnimation);
                                }
                                return;
                            }
                            if (!speechStarted)
                            {
                                speechStarted = true;
                                StartSpeechOrAction(actionContext, suppressSpeakAnimation);
                            }
                            currentBubble.AppendText(result);
                        });
                    }
                }

                if (ShouldStoreInChatHistory(actionContext, directLogType))
                {
                    lock (historyLock)
                    {
                        history.Add(new LlmChatMessage("user", text));
                        history.Add(new LlmChatMessage("assistant", result));
                        int maxCount = Math.Max(0, config.HistoryTurns) * 2;
                        if (maxCount > 0 && history.Count > maxCount)
                        {
                            history.RemoveRange(0, history.Count - maxCount);
                        }
                    }
                }
                lastAssistantReply = result;
            }
            catch (Exception ex)
            {
                result = "大模型对话失败: {0}".Translate(GetFriendlyMessage(ex));
                mw.Dispatcher.Invoke(() =>
                {
                    currentBubble = EnsureResponseBubble(false);
                    if (currentBubble == null || currentBubble.IsClosed)
                    {
                        if (actionContext?.PlayAction != null && !speechStarted)
                        {
                            speechStarted = true;
                            StartSpeechOrAction(actionContext, suppressSpeakAnimation);
                        }
                        return;
                    }
                    if (!speechStarted)
                    {
                        speechStarted = true;
                        StartSpeechOrAction(actionContext, suppressSpeakAnimation);
                    }
                    currentBubble.ReplaceText(result);
                });
                mw.Dispatcher.Invoke(() => mw.ActivityLogs.Add(new ActivityLog("llm_error", true, ex.ToString())));
                LlmActionDebugLog.Write(mw, "llm_response_error", GetFriendlyMessage(ex));
            }
            finally
            {
                ttsQueue?.Complete();
                using var closeCts = new CancellationTokenSource();
                await speechGate.WaitPlaybackCompleteAsync(closeCts.Token);
                if (currentBubble != null && !currentBubble.IsClosed)
                    await currentBubble.WaitForOutputCompleteAsync(closeCts.Token);
                mw.Dispatcher.Invoke(StopSpeakAnimation);
                LastResponseCompletedAt = DateTime.Now;
                isResponding = false;
                LlmActionDebugLog.Write(mw, "llm_response_end",
                    $"elapsedMs={(DateTime.Now - responseStartTime).TotalMilliseconds:f0}",
                    $"lastReplyChars={lastAssistantReply?.Length ?? 0}");
                mw.Dispatcher.Invoke(() => ResponseCompleted?.Invoke());
                mw.Dispatcher.Invoke(() =>
                {
                    if (currentBubble != null && !currentBubble.IsClosed)
                    {
                        currentBubble.SetInputEnabled(true);
                        currentBubble.SetReplayEnabled(!string.IsNullOrWhiteSpace(lastAssistantReply)
                            && MiMoVoiceConfig.Load(mw.Set).TtsEnabled);
                        ScheduleBubbleAutoClose(currentBubble);
                    }
                });
                mw.Dispatcher.Invoke(() =>
                {
                    if (bubble != null && !bubble.IsClosed && !ReferenceEquals(bubble, currentBubble))
                    {
                        bubble.SetInputEnabled(true);
                        ScheduleBubbleAutoClose(bubble);
                    }
                });
            }
        });
    }

    private bool ShouldStoreInChatHistory(LlmActionTriggerContext actionContext, string directLogType)
    {
        if (actionContext != null)
            return false;
        return string.Equals(directLogType, "hostsay", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildTtsLogContext(LlmActionTriggerContext actionContext, string directLogType)
    {
        if (actionContext?.GraphInfo != null)
            return "action:" + actionContext.ActionKey;
        return string.IsNullOrWhiteSpace(directLogType) ? "llm" : directLogType;
    }

    private LlmFloatingTalkWindow GetBubble()
    {
        if (bubble == null || bubble.IsClosed)
        {
            var createdBubble = new LlmFloatingTalkWindow(mw);
            createdBubble.InputSubmitted += Responded;
            createdBubble.VoiceInputStarted += StartVoiceInput;
            createdBubble.VoiceInputStopped += StopVoiceInput;
            createdBubble.ReplayRequested += ReplayLastReply;
            createdBubble.Closed += (_, _) =>
            {
                if (ReferenceEquals(bubble, createdBubble))
                    bubble = null;
            };
            bubble = createdBubble;
            bubble.SetVoiceInputEnabled(MiMoVoiceConfig.Load(mw.Set).AsrEnabled);
        }
        if (!bubble.IsVisible)
            bubble.Show();
        bubble.SetInputEnabled(!isResponding);
        return bubble;
    }

    private void ReplayLastReply()
    {
        string text = lastAssistantReply;
        if (string.IsNullOrWhiteSpace(text))
            return;

        var voiceConfig = MiMoVoiceConfig.Load(mw.Set);
        if (!voiceConfig.TtsEnabled)
            return;

        LlmFloatingTalkWindow currentBubble = GetBubble();
        currentBubble.SetReplayEnabled(false);
        var replayQueue = new MiMoTtsPlaybackQueue(mw, voiceConfig, "replay");
        replayQueue.ErrorOccurred += message =>
        {
            if (currentBubble != null && !currentBubble.IsClosed)
                currentBubble.AppendText(Environment.NewLine + "语音重播失败: {0}".Translate(message));
        };
        Task.Run(async () =>
        {
            try
            {
                mw.Dispatcher.Invoke(() => StartSpeakAnimation());
                replayQueue.AddText(text);
                replayQueue.Complete();
                using var cts = new CancellationTokenSource();
                await replayQueue.WaitPlaybackCompleteAsync(cts.Token);
            }
            finally
            {
                mw.Dispatcher.Invoke(() => StopSpeakAnimation());
                mw.Dispatcher.Invoke(() =>
                {
                    if (currentBubble != null && !currentBubble.IsClosed)
                        currentBubble.SetReplayEnabled(true);
                });
            }
        });
    }

    private void ScheduleBubbleAutoClose(LlmFloatingTalkWindow targetBubble)
    {
        if (targetBubble == null || targetBubble.IsClosed)
            return;
        var config = LlmChatConfig.Load(mw.Set);
        if (!config.BubbleAutoCloseEnabled)
            return;
        int seconds = Math.Clamp(config.BubbleAutoCloseSeconds, 3, 120);
        _ = targetBubble.WaitThenCloseAsync(seconds * 1000, CancellationToken.None);
    }

    private void StartVoiceInput()
    {
        if (isRecognizingVoice || asrRecorder != null)
            return;
        var voiceConfig = MiMoVoiceConfig.Load(mw.Set);
        if (!voiceConfig.AsrEnabled)
            return;
        if (string.IsNullOrWhiteSpace(voiceConfig.ApiKey))
        {
            ShowVoiceStatus("请先在额外附加设置里填写 MiMo API Key".Translate(), false);
            return;
        }

        try
        {
            asrRecorder = new MiMoAsrRecorder(voiceConfig.AsrInputDeviceNumber);
            asrRecorder.Start();
            ShowVoiceStatus("录音中".Translate(), false);
        }
        catch (Exception ex)
        {
            asrRecorder?.Dispose();
            asrRecorder = null;
            ShowVoiceError("语音录制失败: {0}".Translate(GetFriendlyMessage(ex)));
        }
    }

    private void StopVoiceInput()
    {
        if (asrRecorder == null || isRecognizingVoice)
            return;

        string wavPath;
        try
        {
            wavPath = asrRecorder.Stop();
        }
        catch (Exception ex)
        {
            asrRecorder.Dispose();
            asrRecorder = null;
            ShowVoiceError("语音录制失败: {0}".Translate(GetFriendlyMessage(ex)));
            return;
        }
        asrRecorder.Dispose();
        asrRecorder = null;
        RecognizeVoiceAsync(wavPath);
    }

    private void RecognizeVoiceAsync(string wavPath)
    {
        isRecognizingVoice = true;
        ShowVoiceStatus("识别中".Translate(), true);
        Task.Run(async () =>
        {
            string recognized = "";
            try
            {
                var voiceConfig = MiMoVoiceConfig.Load(mw.Set);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(voiceConfig.TimeoutSeconds, 5, 600)));
                recognized = await voiceClient.RecognizeAsync(voiceConfig, wavPath, cts.Token);
                mw.Dispatcher.Invoke(() =>
                {
                    if (voiceConfig.AsrSubmitMode == MiMoAsrSubmitMode.AutoSend)
                    {
                        GetBubble().SetInputText("");
                        Responded(recognized);
                    }
                    else
                    {
                        GetBubble().SetInputText(recognized);
                    }
                });
            }
            catch (Exception ex)
            {
                mw.Dispatcher.Invoke(() => ShowVoiceError("语音识别失败: {0}".Translate(GetFriendlyMessage(ex))));
                mw.Dispatcher.Invoke(() => mw.ActivityLogs.Add(new ActivityLog("mimo_asr_error", true, ex.ToString())));
            }
            finally
            {
                TryDelete(wavPath);
                isRecognizingVoice = false;
                mw.Dispatcher.Invoke(() =>
                {
                    if (bubble != null && !bubble.IsClosed)
                    {
                        bubble.SetVoiceInputState("按住说".Translate(), false);
                        bubble.SetInputEnabled(!isResponding);
                    }
                });
            }
        });
    }

    private void ShowVoiceStatus(string text, bool busy)
    {
        var voiceBubble = GetBubble();
        voiceBubble.SetVoiceInputState(text, busy);
        voiceBubble.FocusInput();
    }

    private void ShowVoiceError(string text)
    {
        var voiceBubble = GetBubble();
        voiceBubble.ReplaceText(text);
        voiceBubble.SetVoiceInputState("按住说".Translate(), false);
        voiceBubble.SetInputEnabled(!isResponding);
        voiceBubble.FocusInput();
    }

    private void StartSpeakAnimation(LlmActionTriggerContext actionContext = null)
    {
        if (actionContext?.PlayAction != null)
        {
            llmSpeaking = true;
            LlmActionDebugLog.Write(mw, "llm_action_play_start", $"action={actionContext.ActionKey}",
                $"display={mw.Main.DisplayType.Type}/{mw.Main.DisplayType.Name}/{mw.Main.DisplayType.Animat}");
            void PlayAction()
            {
                actionContext.PlayAction.Invoke();
                LlmActionDebugLog.Write(mw, "llm_action_play_invoked", $"action={actionContext.ActionKey}",
                    $"display={mw.Main.DisplayType.Type}/{mw.Main.DisplayType.Name}/{mw.Main.DisplayType.Animat}");
            }

            if (mw.Main.DisplayType.Name == "think")
                mw.Main.Display("think", AnimatType.C_End, PlayAction);
            else
                PlayAction();
            return;
        }

        var sayName = mw.Core.Graph.FindName(GraphType.Say);
        if (string.IsNullOrWhiteSpace(sayName))
            return;

        void StartSay()
        {
            llmSpeaking = true;
            mw.Main.Display(sayName, AnimatType.A_Start, () => ContinueSpeakAnimation(sayName));
        }

        if (mw.Main.DisplayType.Name == "think")
            mw.Main.Display("think", AnimatType.C_End, StartSay);
        else if (mw.Main.DisplayType.Type == GraphType.Default || mw.Main.DisplayType.Type == GraphType.Say)
            StartSay();
    }

    private void StartSpeechOrAction(LlmActionTriggerContext actionContext = null, bool suppressSpeakAnimation = false)
    {
        if (suppressSpeakAnimation)
        {
            LlmActionDebugLog.Write(mw, "llm_speech_start_suppressed", actionContext == null ? "" : $"action={actionContext.ActionKey}");
            return;
        }
        if (actionContext?.SuppressSpeakAnimation == true)
        {
            LlmActionDebugLog.Write(mw, "llm_speech_start_suppressed", actionContext == null ? "" : $"action={actionContext.ActionKey}");
            return;
        }
        StartSpeakAnimation(actionContext);
    }

    private void StopSpeakAnimation()
    {
        llmSpeaking = false;
        if (mw.Main.DisplayType.Type == GraphType.Say && mw.Main.DisplayType.Animat != AnimatType.C_End)
            mw.Main.DisplayCEndtoNomal(mw.Main.DisplayType.Name);
    }

    private void ContinueSpeakAnimation(string sayName)
    {
        if (llmSpeaking)
            mw.Main.Display(sayName, AnimatType.B_Loop, () => ContinueSpeakAnimation(sayName));
        else if (mw.Main.DisplayType.Type == GraphType.Say && mw.Main.DisplayType.Animat != AnimatType.C_End)
            mw.Main.DisplayCEndtoNomal(sayName);
    }

    private static string GetFriendlyMessage(Exception ex)
    {
        string message = ex.InnerException?.Message ?? ex.Message;
        if (string.IsNullOrWhiteSpace(message))
            return "未知错误";
        return message.Length > 180 ? message[..180] : message;
    }

    private static string TrimForLog(string text)
    {
        string value = (text ?? "")
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ")
            .Trim();
        return value.Length > 120 ? value[..120] + "..." : value;
    }

    private ILlmChatClient GetClient(LlmChatProvider provider)
    {
        if (!clients.TryGetValue(provider, out ILlmChatClient client))
        {
            client = LlmChatClientFactory.Create(provider);
            clients[provider] = client;
        }
        return client;
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

    private class BuiltInMainPlugin : MainPlugin
    {
        public BuiltInMainPlugin(MainWindow mw) : base(mw)
        {
        }

        public override string PluginName => "BuiltInLlmChat";
    }
}
