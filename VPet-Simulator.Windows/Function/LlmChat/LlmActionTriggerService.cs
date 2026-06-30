using System;
using System.Collections.Generic;
using System.Windows.Threading;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Windows;

internal sealed class LlmActionTriggerService : IDisposable
{
    private readonly MainWindow mw;
    private string lastManualSequenceKey = "";
    private string suppressedKey = "";
    private string pendingAutomaticKey = "";
    private DateTime nextMoveChatterTime = DateTime.MinValue;
    private DateTime moveChatterScheduleBase = DateTime.MinValue;
    private DispatcherTimer moveChatterTimer;
    private bool coreMoveActive;
    private string coreMoveGraph = "";
    private double moveStartLeft;
    private double moveStartTop;
    private bool forcedMoveWindowMotion;
    private bool moveChatterQueued;
    private string keepAliveActionKey = "";
    private string keepAliveSource = "";
    private readonly HashSet<string> manualPendingCompletionKeys = new(StringComparer.OrdinalIgnoreCase);
    private bool disposed;

    public LlmActionTriggerService(MainWindow mw)
    {
        this.mw = mw;
        if (mw.Main == null)
            return;
        mw.Main.AutoDisplayIntercept = TryInterceptAutomaticAction;
        mw.Main.AutoDisplayKeepAlive = ShouldKeepAutoDisplay;
        mw.Main.ManualDisplayCompleteIntercept = TryInterceptManualInteractionComplete;
        mw.Main.GraphDisplayHandler += Main_GraphDisplayHandler;
        mw.Main.Event_MoveStart += Main_Event_MoveStart;
        mw.Main.Event_MoveEnd += Main_Event_MoveEnd;
        ScheduleNextMoveChatter(LoadConfig());
        moveChatterTimer = new DispatcherTimer(DispatcherPriority.Background, mw.Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        moveChatterTimer.Tick += MoveChatterTimer_Tick;
        moveChatterTimer.Start();
        Log("service_installed", DescribeConfig(LoadConfig()), $"nextMoveChatter={nextMoveChatterTime:HH:mm:ss}");
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        if (mw.Main != null)
        {
            if (ReferenceEquals(mw.Main.AutoDisplayIntercept?.Target, this))
                mw.Main.AutoDisplayIntercept = null;
            if (ReferenceEquals(mw.Main.AutoDisplayKeepAlive?.Target, this))
                mw.Main.AutoDisplayKeepAlive = null;
            if (ReferenceEquals(mw.Main.ManualDisplayCompleteIntercept?.Target, this))
                mw.Main.ManualDisplayCompleteIntercept = null;
            mw.Main.GraphDisplayHandler -= Main_GraphDisplayHandler;
            mw.Main.Event_MoveStart -= Main_Event_MoveStart;
            mw.Main.Event_MoveEnd -= Main_Event_MoveEnd;
        }
        moveChatterTimer?.Stop();
        if (moveChatterTimer != null)
            moveChatterTimer.Tick -= MoveChatterTimer_Tick;
        Log("service_disposed");
    }

    private void Main_Event_MoveStart(GraphHelper.Move move)
    {
        coreMoveActive = true;
        coreMoveGraph = move?.Graph ?? "";
        (moveStartLeft, moveStartTop) = GetWindowPosition();
        forcedMoveWindowMotion = TryEnableMoveWindowMotion();
        var config = LoadConfig();
        ScheduleNextMoveChatter(config, DateTime.Now);
        Log("move_event_start",
            $"graph={coreMoveGraph}",
            $"left={moveStartLeft:f1}",
            $"top={moveStartTop:f1}",
            $"smart={mw.Main.MoveTimerSmartMove}",
            $"force={mw.Main.ForceCurrentMoveWindowMotion}",
            DescribeState());
    }

    private void Main_Event_MoveEnd(GraphHelper.Move move)
    {
        var (endLeft, endTop) = GetWindowPosition();
        double deltaX = endLeft - moveStartLeft;
        double deltaY = endTop - moveStartTop;
        Log("move_event_end",
            $"graph={move?.Graph ?? coreMoveGraph}",
            $"left={endLeft:f1}",
            $"top={endTop:f1}",
            $"dx={deltaX:f1}",
            $"dy={deltaY:f1}",
            $"smart={mw.Main.MoveTimerSmartMove}",
            $"forced={forcedMoveWindowMotion}",
            DescribeState());
        if (Math.Abs(deltaX) < 0.5 && Math.Abs(deltaY) < 0.5)
        {
            Log("move_no_position_delta",
                $"graph={move?.Graph ?? coreMoveGraph}",
                $"smart={mw.Main.MoveTimerSmartMove}",
                $"forced={forcedMoveWindowMotion}",
                DescribeWindowDistances());
        }
        coreMoveActive = false;
        coreMoveGraph = "";
        forcedMoveWindowMotion = false;
        if (mw.TalkBox is LlmTalkBox llmTalkBox)
        {
            ScheduleNextMoveChatter(LoadConfig(), llmTalkBox.LastResponseCompletedAt > DateTime.MinValue
                ? llmTalkBox.LastResponseCompletedAt
                : DateTime.Now);
        }
    }

    private bool TryEnableMoveWindowMotion()
    {
        if (!IsLlmModeActive())
            return false;
        var config = LoadConfig();
        if (!config.Enabled || !config.MoveChatterEnabled)
            return false;
        mw.Main.ForceCurrentMoveWindowMotion = true;
        Log("move_force_motion_enabled", DescribeConfig(config));
        return true;
    }

    public bool IsActionEnabled(GraphInfo info, bool automatic)
    {
        if (!IsLlmModeActive())
            return false;
        var config = LoadConfig();
        if (!config.Enabled)
            return false;
        if (automatic && !config.AutoActionsEnabled)
            return false;
        if (!automatic && !config.ManualActionsEnabled)
            return false;
        if (!CanTrigger(info))
            return false;
        return config.IsActionEnabled(LlmActionDescriptor.BuildKey(info));
    }

    private bool IsLlmModeActive()
    {
        return string.Equals(mw.Set["CGPT"].GetString("type", ""), "API", StringComparison.OrdinalIgnoreCase)
            && mw.TalkBox is LlmTalkBox;
    }

    private bool TryInterceptAutomaticAction(GraphInfo info, Action playAction)
    {
        Log("auto_seen", Describe(info), DescribeState());
        if (info?.Type == GraphType.Move)
        {
            Log("auto_skip", Describe(info), "move_uses_chatter");
            return false;
        }
        if (IsCurrentlyMoving())
        {
            TryMoveChatter(null, "auto_blocked_by_move");
            Log("auto_skip", Describe(info), "currently_moving", DescribeState());
            return false;
        }
        if (!IsActionEnabled(info, true))
        {
            Log("auto_skip", Describe(info), GetDisabledReason(info, true));
            return false;
        }
        if (mw.TalkBox is not LlmTalkBox llmTalkBox)
        {
            Log("auto_skip", Describe(info), "talkbox_not_llm");
            return false;
        }
        var config = LoadConfig();
        if (config.SkipWhenBusy && llmTalkBox.IsResponding)
        {
            Log("auto_skip", Describe(info), "llm_busy");
            return false;
        }

        var context = new LlmActionTriggerContext
        {
            GraphInfo = info,
            IsAutomatic = true,
            PlayAction = () => PlaySuppressed(info, playAction)
        };
        pendingAutomaticKey = CanTrigger(info) ? LlmActionDescriptor.BuildKey(info) : "";
        if (!llmTalkBox.TryRespondToAction(context))
        {
            pendingAutomaticKey = "";
            Log("auto_skip", Describe(info), "try_respond_failed");
            return false;
        }
        TrackKeepAlive(info, llmTalkBox);
        Log("auto_trigger", Describe(info));
        return true;
    }

    private void Main_GraphDisplayHandler(GraphInfo info)
    {
        if (!IsLowSignalDisplay(info))
            Log("display_seen", Describe(info), DescribeState());
        TryMoveChatter(info, "display");
        if (TryRespondToObservedAutomaticAction(info))
            return;
        if (!IsActionEnabled(info, false))
        {
            if (!IsLowSignalDisplay(info))
                Log("manual_skip", Describe(info), GetDisabledReason(info, false));
            return;
        }
        if (info.Animat == AnimatType.C_End)
        {
            Log("manual_skip", Describe(info), "c_end");
            return;
        }
        if (IsManagedManualInteraction(info))
        {
            string managedKey = LlmActionDescriptor.BuildKey(info);
            if (!string.Equals(managedKey, lastManualSequenceKey, StringComparison.OrdinalIgnoreCase)
                || info.Animat == AnimatType.A_Start)
            {
                Log("manual_interaction_start", Describe(info), DescribeState());
            }
            lastManualSequenceKey = managedKey;
            Log("manual_skip", Describe(info), "managed_manual_interaction_wait_for_complete");
            return;
        }

        string key = LlmActionDescriptor.BuildKey(info);
        if (string.Equals(key, suppressedKey, StringComparison.OrdinalIgnoreCase))
        {
            lastManualSequenceKey = key;
            pendingAutomaticKey = "";
            Log("manual_skip", Describe(info), "suppressed_auto_play");
            return;
        }
        if (string.Equals(key, pendingAutomaticKey, StringComparison.OrdinalIgnoreCase))
        {
            lastManualSequenceKey = key;
            pendingAutomaticKey = "";
            Log("manual_skip", Describe(info), "pending_auto_play");
            return;
        }
        if (info.Animat == AnimatType.B_Loop && string.Equals(key, lastManualSequenceKey, StringComparison.OrdinalIgnoreCase))
        {
            Log("manual_skip", Describe(info), "same_sequence_b_loop");
            return;
        }
        lastManualSequenceKey = key;

        if (mw.TalkBox is not LlmTalkBox llmTalkBox)
        {
            Log("manual_skip", Describe(info), "talkbox_not_llm");
            return;
        }
        var config = LoadConfig();
        if (config.SkipWhenBusy && llmTalkBox.IsResponding)
        {
            Log("manual_skip", Describe(info), "llm_busy");
            return;
        }

        var context = new LlmActionTriggerContext
        {
            GraphInfo = info,
            IsAutomatic = false,
            PlayAction = null
        };
        bool started = llmTalkBox.TryRespondToAction(context);
        Log(started ? "manual_trigger" : "manual_skip", Describe(info), started ? "" : "try_respond_failed");
    }

    private bool TryInterceptManualInteractionComplete(GraphInfo info, string interactionType, Action playAction)
    {
        Log("manual_interaction_end", Describe(info), $"interaction={interactionType}", DescribeState());
        if (!IsManagedManualInteraction(info))
        {
            Log("manual_interaction_skip", Describe(info), $"interaction={interactionType}", "not_managed");
            return false;
        }
        string key = LlmActionDescriptor.BuildKey(info);
        if (manualPendingCompletionKeys.Contains(key))
        {
            Log("manual_interaction_wait_loop", Describe(info), $"interaction={interactionType}");
            return true;
        }
        return TryStartManualInteractionResponse(info, interactionType, "", playAction);
    }

    public bool TryRespondToFoodInteraction(Food food, string graphName, Action playAction)
    {
        string name = string.IsNullOrWhiteSpace(graphName) ? food?.GetGraph() ?? "eat" : graphName;
        var info = new GraphInfo(name, GraphType.Common, AnimatType.Single, mw.Core?.Save?.Mode ?? IGameSave.ModeType.Nomal);
        string detail = food == null
            ? ""
            : $"物品={food.TranslateName};类型={food.Type};饱腹={food.StrengthFood:f0};口渴={food.StrengthDrink:f0};心情={food.Feeling:f0}";
        Log("manual_interaction_end", Describe(info), "interaction=food", detail, DescribeState());
        return TryStartManualInteractionResponse(info, "food", detail, playAction);
    }

    public void NotifyFoodInteractionStart(Food food, string graphName)
    {
        string name = string.IsNullOrWhiteSpace(graphName) ? food?.GetGraph() ?? "eat" : graphName;
        var info = new GraphInfo(name, GraphType.Common, AnimatType.Single, mw.Core?.Save?.Mode ?? IGameSave.ModeType.Nomal);
        string detail = food == null ? "" : $"物品={food.TranslateName};类型={food.Type}";
        Log("manual_interaction_start", Describe(info), "interaction=food", detail, DescribeState());
    }

    private bool TryRespondToObservedAutomaticAction(GraphInfo info)
    {
        if (info == null || info.Animat == AnimatType.C_End)
            return false;
        string key = LlmActionDescriptor.BuildKey(info);
        if (string.Equals(key, suppressedKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, pendingAutomaticKey, StringComparison.OrdinalIgnoreCase))
            return false;
        if (info.Animat == AnimatType.B_Loop && string.Equals(key, lastManualSequenceKey, StringComparison.OrdinalIgnoreCase))
            return false;

        var config = LoadConfig();
        if (!config.Enabled || !config.AutoActionsEnabled || !config.ObservedAutoActionsEnabled)
            return false;
        if (!CanTrigger(info) || !IsObservedAutomaticType(info))
            return false;
        if (!config.IsActionEnabled(key))
            return false;
        if (mw.TalkBox is not LlmTalkBox llmTalkBox)
        {
            Log("observed_auto_skip", Describe(info), "talkbox_not_llm");
            return false;
        }
        if (config.SkipWhenBusy && llmTalkBox.IsResponding)
        {
            Log("observed_auto_skip", Describe(info), "llm_busy");
            return false;
        }

        lastManualSequenceKey = key;
        var context = new LlmActionTriggerContext
        {
            GraphInfo = info,
            IsAutomatic = true,
            PlayAction = null
        };
        bool started = llmTalkBox.TryRespondToAction(context);
        if (started)
            TrackKeepAlive(info, llmTalkBox);
        Log(started ? "observed_auto_trigger" : "observed_auto_skip", Describe(info),
            started ? "" : "try_respond_failed");
        return started;
    }

    private void TrackKeepAlive(GraphInfo info, LlmTalkBox llmTalkBox, string source = "auto")
    {
        if (info == null || llmTalkBox == null)
            return;
        keepAliveActionKey = LlmActionDescriptor.BuildKey(info);
        keepAliveSource = source;
        llmTalkBox.ResponseCompleted -= LlmTalkBox_KeepAliveResponseCompleted;
        llmTalkBox.ResponseCompleted += LlmTalkBox_KeepAliveResponseCompleted;
        Log(source == "manual" ? "manual_interaction_keep_alive_start" : "auto_keep_alive_start",
            Describe(info), $"key={keepAliveActionKey}");
    }

    private void LlmTalkBox_KeepAliveResponseCompleted()
    {
        if (mw.TalkBox is LlmTalkBox llmTalkBox)
            llmTalkBox.ResponseCompleted -= LlmTalkBox_KeepAliveResponseCompleted;
        Log(keepAliveSource == "manual" ? "manual_interaction_keep_alive_end" : "auto_keep_alive_end",
            $"key={keepAliveActionKey}");
        keepAliveActionKey = "";
        keepAliveSource = "";
    }

    private bool ShouldKeepAutoDisplay(GraphInfo info)
    {
        if (info == null || string.IsNullOrWhiteSpace(keepAliveActionKey))
            return false;
        if (!IsLlmModeActive())
            return false;
        if (mw.TalkBox is not LlmTalkBox llmTalkBox || !llmTalkBox.IsResponding)
            return false;
        if (!string.Equals(keepAliveActionKey, LlmActionDescriptor.BuildKey(info), StringComparison.OrdinalIgnoreCase))
            return false;

        Log("auto_keep_alive", Describe(info), DescribeState());
        return true;
    }

    private void MoveChatterTimer_Tick(object sender, EventArgs e)
    {
        TryMoveChatter(null, "timer");
    }

    private void TryMoveChatter(GraphInfo info, string source)
    {
        if (info != null && (info.Type != GraphType.Move || info.Animat == AnimatType.C_End))
            return;
        if (!IsCurrentlyMoving())
        {
            if (info?.Type == GraphType.Move)
                Log("move_skip", source, Describe(info), "move_not_active", DescribeState());
            return;
        }
        Log("move_seen", source, Describe(info), $"next={nextMoveChatterTime:HH:mm:ss}", DescribeState());
        if (!IsLlmModeActive())
        {
            Log("move_skip", source, Describe(info), "llm_mode_inactive");
            return;
        }
        var config = LoadConfig();
        if (!config.Enabled || !config.MoveChatterEnabled)
        {
            Log("move_skip", source, Describe(info), $"enabled={config.Enabled}", $"moveChatter={config.MoveChatterEnabled}");
            return;
        }
        if (mw.TalkBox is not LlmTalkBox llmTalkBox || llmTalkBox.IsResponding)
        {
            Log("move_skip", source, Describe(info), mw.TalkBox is LlmTalkBox ? "llm_busy" : "talkbox_not_llm");
            return;
        }
        if (llmTalkBox.LastResponseCompletedAt > moveChatterScheduleBase)
        {
            ScheduleNextMoveChatter(config, llmTalkBox.LastResponseCompletedAt);
            Log("move_reschedule_after_response", source, Describe(info), $"last={llmTalkBox.LastResponseCompletedAt:HH:mm:ss}", $"next={nextMoveChatterTime:HH:mm:ss}");
            return;
        }
        if (DateTime.Now < nextMoveChatterTime)
        {
            Log("move_skip", source, Describe(info), $"cooldown_until={nextMoveChatterTime:HH:mm:ss}");
            return;
        }

        ScheduleNextMoveChatter(config);
        Log("move_queue", source, Describe(info), $"next={nextMoveChatterTime:HH:mm:ss}");
        QueueMoveChatter();
    }

    private void QueueMoveChatter()
    {
        if (moveChatterQueued)
        {
            Log("move_queue_skip", "already_queued");
            return;
        }
        moveChatterQueued = true;
        mw.Dispatcher.BeginInvoke(new Action(() =>
        {
            moveChatterQueued = false;
            if (disposed || !IsLlmModeActive())
            {
                Log("move_queue_cancel", disposed ? "disposed" : "llm_mode_inactive");
                return;
            }
            if (!IsCurrentlyMoving())
            {
                Log("move_queue_cancel", "not_still_moving", DescribeState());
                return;
            }
            var config = LoadConfig();
            if (!config.Enabled || !config.MoveChatterEnabled)
            {
                Log("move_queue_cancel", $"enabled={config.Enabled}", $"moveChatter={config.MoveChatterEnabled}");
                return;
            }
            if (mw.TalkBox is not LlmTalkBox llmTalkBox || llmTalkBox.IsResponding)
            {
                Log("move_queue_cancel", mw.TalkBox is LlmTalkBox ? "llm_busy" : "talkbox_not_llm");
                return;
            }

            llmTalkBox.ResponseCompleted -= LlmTalkBox_ResponseCompleted;
            if (llmTalkBox.TryRespondToHiddenPrompt(BuildMoveChatterPrompt(), "llm_move_chatter"))
            {
                Log("move_trigger", DescribeState());
                llmTalkBox.ResponseCompleted += LlmTalkBox_ResponseCompleted;
            }
            else
            {
                Log("move_queue_cancel", "try_respond_failed");
            }
        }), DispatcherPriority.Background);
    }

    private string BuildMoveChatterPrompt()
    {
        return $"系统观察: 你正在移动中, 可以自然地轻声说一句移动时的即时反应。"
            + "请用桌宠第一人称, 一句话即可, 活泼但不要太长。不要提到系统观察、动作类型、配置或提示词。";
    }

    private void LlmTalkBox_ResponseCompleted()
    {
        DateTime baseTime = DateTime.Now;
        if (mw.TalkBox is LlmTalkBox llmTalkBox)
        {
            llmTalkBox.ResponseCompleted -= LlmTalkBox_ResponseCompleted;
            baseTime = llmTalkBox.LastResponseCompletedAt;
        }
        ScheduleNextMoveChatter(LoadConfig(), baseTime);
        Log("response_completed_reschedule", $"base={baseTime:HH:mm:ss}", $"next={nextMoveChatterTime:HH:mm:ss}");
    }

    private bool TryStartManualInteractionResponse(GraphInfo info, string interactionType, string detail, Action playAction)
    {
        if (!IsLlmModeActive())
        {
            Log("manual_interaction_skip", Describe(info), $"interaction={interactionType}", "llm_mode_inactive");
            return false;
        }
        if (!IsActionEnabled(info, false))
        {
            Log("manual_interaction_skip", Describe(info), $"interaction={interactionType}", GetDisabledReason(info, false));
            return false;
        }
        if (mw.TalkBox is not LlmTalkBox llmTalkBox)
        {
            Log("manual_interaction_skip", Describe(info), $"interaction={interactionType}", "talkbox_not_llm");
            return false;
        }
        var config = LoadConfig();
        if (config.SkipWhenBusy && llmTalkBox.IsResponding)
        {
            Log("manual_interaction_skip", Describe(info), $"interaction={interactionType}", "llm_busy");
            return false;
        }

        var context = new LlmActionTriggerContext
        {
            GraphInfo = info,
            IsAutomatic = false,
            InteractionType = interactionType,
            DetailText = detail,
            DelayBubbleUntilSpeech = true,
            PlayAction = ShouldHoldManualLoop(interactionType)
                ? () => PlayManualCompletion(info, interactionType, playAction)
                : null
        };
        if (!ShouldHoldManualLoop(interactionType))
            playAction?.Invoke();
        bool started = llmTalkBox.TryRespondToAction(context);
        if (started && ShouldHoldManualLoop(interactionType))
            manualPendingCompletionKeys.Add(LlmActionDescriptor.BuildKey(info));
        Log(started ? "manual_interaction_trigger" : "manual_interaction_skip",
            Describe(info),
            $"interaction={interactionType}",
            started ? "" : "try_respond_failed");
        return started;
    }

    private void PlayManualCompletion(GraphInfo info, string interactionType, Action playAction)
    {
        manualPendingCompletionKeys.Remove(LlmActionDescriptor.BuildKey(info));
        Log("manual_interaction_play_action", Describe(info), $"interaction={interactionType}", DescribeState());
        PlaySuppressed(info, playAction);
    }

    private static bool ShouldHoldManualLoop(string interactionType)
    {
        return string.Equals(interactionType, "touch_head", StringComparison.OrdinalIgnoreCase)
            || string.Equals(interactionType, "touch_body", StringComparison.OrdinalIgnoreCase);
    }

    private void ScheduleNextMoveChatter(LlmActionLinkConfig config, DateTime? baseTime = null)
    {
        int min = Math.Max(5, config.MoveChatterMinSeconds);
        int max = Math.Max(min, config.MoveChatterMaxSeconds);
        DateTime startTime = baseTime ?? DateTime.Now;
        moveChatterScheduleBase = startTime;
        nextMoveChatterTime = startTime.AddSeconds(Random.Shared.Next(min, max + 1));
        Log("move_schedule", $"base={startTime:HH:mm:ss}", $"min={min}", $"max={max}", $"next={nextMoveChatterTime:HH:mm:ss}");
    }

    private LlmActionLinkConfig LoadConfig()
    {
        var config = LlmActionLinkConfig.Load(mw.Set);
        if (!config.Initialized)
        {
            config.InitializeDefaultActions(LlmActionDescriptor.FromGraphCore(mw.Core?.Graph));
            config.Save(mw.Set);
            Log("config_initialized", DescribeConfig(config), $"enabledActions={config.EnabledActionKeys.Count}");
        }
        else
        {
            int beforeCount = config.EnabledActionKeys.Count;
            config.InitializeDefaultActions(LlmActionDescriptor.FromGraphCore(mw.Core?.Graph));
            if (config.EnabledActionKeys.Count != beforeCount)
            {
                config.Save(mw.Set);
                Log("config_default_migrated", DescribeConfig(config), $"before={beforeCount}", $"after={config.EnabledActionKeys.Count}");
            }
        }
        return config;
    }

    private void PlaySuppressed(GraphInfo info, Action playAction)
    {
        string key = LlmActionDescriptor.BuildKey(info);
        suppressedKey = key;
        try
        {
            if (mw.Dispatcher.CheckAccess())
                playAction?.Invoke();
            else
                mw.Dispatcher.Invoke(playAction ?? (() => { }), DispatcherPriority.Normal);
        }
        finally
        {
            mw.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.Equals(suppressedKey, key, StringComparison.OrdinalIgnoreCase))
                    suppressedKey = "";
            }), DispatcherPriority.Background);
        }
    }

    private static bool CanTrigger(GraphInfo info)
    {
        if (info == null || string.IsNullOrWhiteSpace(info.Name))
            return false;
        if (info.Type == GraphType.Move)
            return false;
        if (info.Animat == AnimatType.C_End)
            return false;
        if (LlmActionDescriptor.IsNamedCommonTrigger(info.Type, info.Name))
            return true;
        return !LlmActionDescriptor.IsReservedType(info.Type);
    }

    private static bool IsObservedAutomaticType(GraphInfo info)
    {
        if (info == null)
            return false;
        if (info.Type == GraphType.Common && LlmActionDescriptor.IsCommonMusicTrigger(info.Name))
            return true;
        return info.Type is GraphType.Idel
            or GraphType.Sleep
            or GraphType.StateONE
            or GraphType.StateTWO
            or GraphType.Switch_Up
            or GraphType.Switch_Down
            or GraphType.Switch_Thirsty
            or GraphType.Switch_Hunger
            or GraphType.SideHide_Left_Main
            or GraphType.SideHide_Left_Rise
            or GraphType.SideHide_Right_Main
            or GraphType.SideHide_Right_Rise
            or GraphType.Work;
    }

    private static bool IsManagedManualInteraction(GraphInfo info)
    {
        if (info == null)
            return false;
        if (LlmActionDescriptor.IsCommonFoodTrigger(info.Name))
            return true;
        return info.Type is GraphType.Touch_Head
            or GraphType.Touch_Body
            or GraphType.Raised_Static
            or GraphType.Raised_Dynamic;
    }

    private bool IsCurrentlyMoving()
    {
        return coreMoveActive || mw.Main?.DisplayType.Type == GraphType.Move;
    }

    private static bool IsLowSignalDisplay(GraphInfo info)
    {
        return info == null
            || info.Type is GraphType.Default or GraphType.Say or GraphType.StartUP or GraphType.Shutdown
            || info.Animat == AnimatType.C_End;
    }

    private void Log(string eventName, params string[] details)
    {
        LlmActionDebugLog.Write(mw, eventName, details);
    }

    private string GetDisabledReason(GraphInfo info, bool automatic)
    {
        if (!IsLlmModeActive())
            return "llm_mode_inactive";
        var config = LoadConfig();
        if (!config.Enabled)
            return "config_disabled";
        if (automatic && !config.AutoActionsEnabled)
            return "auto_disabled";
        if (!automatic && !config.ManualActionsEnabled)
            return "manual_disabled";
        if (!CanTrigger(info))
            return "cannot_trigger";
        string key = LlmActionDescriptor.BuildKey(info);
        if (!config.IsActionEnabled(key))
            return "action_not_checked:" + key;
        return "unknown";
    }

    private static string DescribeConfig(LlmActionLinkConfig config)
    {
        return $"enabled={config.Enabled};auto={config.AutoActionsEnabled};observedAuto={config.ObservedAutoActionsEnabled};manual={config.ManualActionsEnabled};move={config.MoveChatterEnabled};skipBusy={config.SkipWhenBusy};version={config.DefaultVersion};actions={config.EnabledActionKeys.Count}";
    }

    private string DescribeState()
    {
        var display = mw.Main?.DisplayType;
        string displayText = display == null ? "display=null" : $"display={display.Type}/{display.Name}/{display.Animat}";
        return $"{displayText};moveEvent={coreMoveActive}/{coreMoveGraph};cgpt={mw.Set["CGPT"].GetString("type", "")};talkbox={mw.TalkBox?.GetType().Name ?? "null"}";
    }

    private (double Left, double Top) GetWindowPosition()
    {
        if (mw.Dispatcher.CheckAccess())
            return (mw.Left, mw.Top);
        return mw.Dispatcher.Invoke(() => (mw.Left, mw.Top));
    }

    private string DescribeWindowDistances()
    {
        var controller = mw.Core?.Controller;
        if (controller == null)
            return "controller=null";
        return $"distLeft={controller.GetWindowsDistanceLeft():f1};distRight={controller.GetWindowsDistanceRight():f1};distUp={controller.GetWindowsDistanceUp():f1};distDown={controller.GetWindowsDistanceDown():f1}";
    }

    private static string Describe(GraphInfo info)
    {
        if (info == null)
            return "info=null";
        return $"type={info.Type};name={info.Name};animat={info.Animat};mode={info.ModeType};key={LlmActionDescriptor.BuildKey(info)}";
    }
}
