using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;

namespace VPet_Simulator.Windows;

internal class MiMoAsrRecorder : IDisposable
{
    private readonly int deviceNumber;
    private WaveInEvent waveIn;
    private WaveFileWriter writer;
    private Stopwatch stopwatch;
    private string path;

    public bool IsRecording => waveIn != null;

    public MiMoAsrRecorder(int deviceNumber = -1)
    {
        this.deviceNumber = deviceNumber;
    }

    public void Start()
    {
        if (IsRecording)
            return;

        string dir = MiMoAudioClient.GetTempDirectory();
        path = Path.Combine(dir, $"mimo_asr_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav");
        waveIn = new WaveInEvent
        {
            DeviceNumber = deviceNumber >= 0 && deviceNumber < WaveInEvent.DeviceCount ? deviceNumber : -1,
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 60
        };
        writer = new WaveFileWriter(path, waveIn.WaveFormat);
        stopwatch = Stopwatch.StartNew();
        waveIn.DataAvailable += (_, e) => writer?.Write(e.Buffer, 0, e.BytesRecorded);
        waveIn.RecordingStopped += (_, _) =>
        {
            writer?.Dispose();
            writer = null;
            waveIn?.Dispose();
            waveIn = null;
        };
        waveIn.StartRecording();
    }

    public string Stop()
    {
        if (!IsRecording)
            return path;

        waveIn.StopRecording();
        stopwatch?.Stop();
        writer?.Dispose();
        writer = null;
        waveIn?.Dispose();
        waveIn = null;

        if (stopwatch != null && stopwatch.ElapsedMilliseconds < 350)
            throw new InvalidOperationException("录音太短, 请按住说话");
        return path;
    }

    public void Dispose()
    {
        try
        {
            Stop();
        }
        catch
        {
        }
    }
}
