using LinePutScript.Localization.WPF;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows;

internal class MiMoTtsPlaybackQueue : LlmSpeechOutputGate, IDisposable
{
    private readonly MainWindow mw;
    private readonly MiMoVoiceConfig config;
    private readonly string logContext;
    private readonly MiMoAudioClient client = new();
    private readonly ConcurrentQueue<TtsSegment> queue = new();
    private readonly List<MediaPlayer> activePlayers = new();
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource cts = new();
    private readonly StringBuilder buffer = new();
    private int sentenceCount;
    private int segmentIndex;
    private bool completed;
    private bool workerStarted;

    public MiMoTtsPlaybackQueue(MainWindow mw, MiMoVoiceConfig config, string logContext = "")
    {
        this.mw = mw;
        this.config = config;
        this.logContext = string.IsNullOrWhiteSpace(logContext) ? "unknown" : logContext;
    }

    public event Action<string> ErrorOccurred;
    public event Action<string> SegmentReady;

    public void AddText(string delta)
    {
        if (completed || string.IsNullOrEmpty(delta))
            return;

        if (!config.TtsSegmentEnabled)
        {
            buffer.Append(delta);
            return;
        }

        foreach (char c in delta)
        {
            buffer.Append(c);
            if (IsSentenceEnd(c))
            {
                sentenceCount++;
                if (sentenceCount >= config.TtsMaxSentences)
                    FlushBuffer();
            }
            else if (ShouldSoftCut(c))
            {
                FlushBuffer();
            }
        }
    }

    public void Complete()
    {
        if (completed)
            return;
        completed = true;
        FlushBuffer();
        EnsureWorker();
    }

    public override Task WaitPlaybackCompleteAsync(CancellationToken cancellationToken)
    {
        Complete();
        return completion.Task.WaitAsync(cancellationToken);
    }

    public void Dispose()
    {
        cts.Cancel();
    }

    private bool ShouldSoftCut(char c)
    {
        if (buffer.Length < config.TtsSoftMaxChars)
            return false;
        if (IsSentenceEnd(c))
            return true;
        return config.TtsCutAtCommaAfterLimit && IsComma(c);
    }

    private void FlushBuffer()
    {
        string displayText = buffer.ToString();
        string speechText = MiMoAudioClient.CleanTextForSpeech(displayText);
        buffer.Clear();
        sentenceCount = 0;
        if (string.IsNullOrWhiteSpace(speechText))
            return;

        int id = Interlocked.Increment(ref segmentIndex);
        queue.Enqueue(new TtsSegment(id, displayText, speechText));
        LlmActionDebugLog.Write(mw, "tts_segment_queued",
            $"source={logContext}",
            $"index={id}",
            $"displayChars={displayText.Length}",
            $"speechChars={speechText.Length}",
            $"speechText={TrimForLog(speechText)}");
        EnsureWorker();
    }

    private void EnsureWorker()
    {
        if (workerStarted)
            return;
        workerStarted = true;
        _ = Task.Run(ProcessQueueAsync);
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                if (queue.TryDequeue(out TtsSegment segment))
                {
                    LlmActionDebugLog.Write(mw, "tts_segment_synthesize_start",
                        $"source={logContext}",
                        $"index={segment.Index}",
                        $"voice={config.TtsVoice}",
                        $"model={config.TtsModel}",
                        $"speechText={TrimForLog(segment.SpeechText)}");
                    string path = await client.SynthesizeToWavFileAsync(config, segment.SpeechText, cts.Token);
                    LlmActionDebugLog.Write(mw, "tts_segment_ready",
                        $"source={logContext}",
                        $"index={segment.Index}",
                        $"bytes={GetFileLength(path)}");
                    mw.Dispatcher.Invoke(() => SegmentReady?.Invoke(segment.DisplayText));
                    await PlayFileAsync(path, segment, cts.Token);
                    TryDelete(path);
                    continue;
                }

                if (completed)
                    break;

                await Task.Delay(80, cts.Token);
            }
            completion.TrySetResult();
        }
        catch (OperationCanceledException)
        {
            completion.TrySetResult();
        }
        catch (Exception ex)
        {
            mw.Dispatcher.Invoke(() => mw.ActivityLogs.Add(new ActivityLog("mimo_tts_error", true, ex.ToString())));
            LlmActionDebugLog.Write(mw, "tts_error", $"source={logContext}", GetFriendlyMessage(ex));
            mw.Dispatcher.Invoke(() => ErrorOccurred?.Invoke(GetFriendlyMessage(ex)));
            completion.TrySetResult();
        }
    }

    private Task PlayFileAsync(string path, TtsSegment segment, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenSource timeoutCts = null;
        mw.Dispatcher.Invoke(() =>
        {
            var player = new MediaPlayer
            {
                Volume = mw.Set.VoiceVolume
            };
            activePlayers.Add(player);
            bool finished = false;
            void FinishPlayback(string eventName, Exception error = null)
            {
                if (finished)
                    return;
                finished = true;
                timeoutCts?.Cancel();
                activePlayers.Remove(player);
                player.Close();
                if (error == null)
                {
                    LlmActionDebugLog.Write(mw, eventName,
                        $"source={logContext}",
                        $"index={segment.Index}");
                    tcs.TrySetResult();
                }
                else
                {
                    LlmActionDebugLog.Write(mw, eventName,
                        $"source={logContext}",
                        $"index={segment.Index}",
                        GetFriendlyMessage(error));
                    tcs.TrySetException(error);
                }
            }
            void StartPlaybackTimeout(TimeSpan timeout, string eventName)
            {
                timeoutCts?.Cancel();
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(timeout, timeoutCts.Token);
                        mw.Dispatcher.Invoke(() => FinishPlayback(eventName));
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });
            }
            player.MediaOpened += (_, _) =>
            {
                TimeSpan timeout = GetPlaybackTimeout(player, path);
                StartPlaybackTimeout(timeout, "tts_segment_play_timeout");
                LlmActionDebugLog.Write(mw, "tts_segment_play_opened",
                    $"source={logContext}",
                    $"index={segment.Index}",
                    $"timeoutMs={timeout.TotalMilliseconds:f0}");
            };
            player.MediaEnded += (_, _) =>
            {
                FinishPlayback("tts_segment_play_end");
            };
            player.MediaFailed += (_, e) =>
            {
                FinishPlayback("tts_segment_play_failed",
                    e.ErrorException ?? new InvalidOperationException("语音播放失败".Translate()));
            };
            player.Open(new Uri(path, UriKind.Absolute));
            LlmActionDebugLog.Write(mw, "tts_segment_play_start",
                $"source={logContext}",
                $"index={segment.Index}");
            StartPlaybackTimeout(GetOpenPlaybackTimeout(path), "tts_segment_play_open_timeout");
            player.Play();
        });

        cancellationToken.Register(() =>
        {
            timeoutCts?.Cancel();
            mw.Dispatcher.BeginInvoke(() =>
            {
                LlmActionDebugLog.Write(mw, "tts_segment_play_cancelled",
                    $"source={logContext}",
                    $"index={segment.Index}");
            });
            tcs.TrySetCanceled(cancellationToken);
        });
        return tcs.Task;
    }

    private static TimeSpan GetOpenPlaybackTimeout(string path)
    {
        TimeSpan duration = EstimateWavDuration(path);
        if (duration <= TimeSpan.Zero)
            duration = TimeSpan.FromSeconds(30);
        return duration + TimeSpan.FromSeconds(10);
    }

    private static TimeSpan GetPlaybackTimeout(MediaPlayer player, string path)
    {
        TimeSpan duration = TimeSpan.Zero;
        try
        {
            if (player.NaturalDuration.HasTimeSpan)
                duration = player.NaturalDuration.TimeSpan;
        }
        catch
        {
        }
        if (duration <= TimeSpan.Zero)
            duration = EstimateWavDuration(path);
        if (duration <= TimeSpan.Zero)
            duration = TimeSpan.FromSeconds(30);
        return duration + TimeSpan.FromSeconds(5);
    }

    private static TimeSpan EstimateWavDuration(string path)
    {
        try
        {
            using var reader = new WaveFileReader(path);
            return reader.TotalTime;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private static bool IsSentenceEnd(char c)
    {
        return c is '。' or '！' or '？' or '.' or '!' or '?' or '\n';
    }

    private static bool IsComma(char c)
    {
        return c is '，' or ',' or '、' or ';' or '；';
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private static string GetFriendlyMessage(Exception ex)
    {
        string message = ex.InnerException?.Message ?? ex.Message;
        if (string.IsNullOrWhiteSpace(message))
            return "未知错误";
        return message.Length > 180 ? message[..180] : message;
    }

    private static long GetFileLength(string path)
    {
        try
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }
        catch
        {
            return 0;
        }
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

    private readonly record struct TtsSegment(int Index, string DisplayText, string SpeechText);

    public static IEnumerable<string> SegmentForPreview(MiMoVoiceConfig config, string text)
    {
        var segments = new List<string>();
        var sb = new StringBuilder();
        int sentences = 0;
        foreach (char c in text ?? "")
        {
            sb.Append(c);
            if (IsSentenceEnd(c))
                sentences++;
            bool shouldCut = sentences >= config.TtsMaxSentences
                || sb.Length >= config.TtsSoftMaxChars && (IsSentenceEnd(c) || config.TtsCutAtCommaAfterLimit && IsComma(c));
            if (!shouldCut)
                continue;

            string segment = MiMoAudioClient.CleanTextForSpeech(sb.ToString());
            if (!string.IsNullOrWhiteSpace(segment))
                segments.Add(segment);
            sb.Clear();
            sentences = 0;
        }
        string tail = MiMoAudioClient.CleanTextForSpeech(sb.ToString());
        if (!string.IsNullOrWhiteSpace(tail))
            segments.Add(tail);
        return segments;
    }
}
