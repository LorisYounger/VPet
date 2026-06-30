using System;
using System.IO;
using System.Text;

namespace VPet_Simulator.Windows;

internal static class LlmActionDebugLog
{
    private static readonly object LockObject = new();
    private static string logPath;

    public static void Write(MainWindow mw, string eventName, params string[] details)
    {
        try
        {
            if (!LlmActionLinkConfig.Load(mw.Set).DebugLogEnabled)
                return;
            string path = GetPath(mw);
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{eventName}";
            if (details != null && details.Length > 0)
                line += "\t" + string.Join("\t", details);
            lock (LockObject)
            {
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
        }
    }

    private static string GetPath(MainWindow mw)
    {
        if (!string.IsNullOrWhiteSpace(logPath))
            return logPath;

        logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "llm-action-link-debug.log");
        return logPath;
    }
}
