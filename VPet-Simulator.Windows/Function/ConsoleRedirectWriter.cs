using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// 将 Console 输出重定向到 ActivityLogs 的 TextWriter
    /// </summary>
    public class ConsoleRedirectWriter : TextWriter
    {
        private readonly ObservableCollection<ActivityLog> _activityLogs;
        private readonly StringBuilder _currentLine = new StringBuilder();

        public ConsoleRedirectWriter(ObservableCollection<ActivityLog> activityLogs)
        {
            _activityLogs = activityLogs ?? throw new ArgumentNullException(nameof(activityLogs));
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (value == '\n')
            {
                FlushCurrentLine();
            }
            else if (value != '\r')
            {
                _currentLine.Append(value);
            }
        }

        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            foreach (char c in value)
            {
                Write(c);
            }
        }

        public override void WriteLine(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _currentLine.Append(value);
            }
            FlushCurrentLine();
        }

        public override void WriteLine()
        {
            FlushCurrentLine();
        }

        private void FlushCurrentLine()
        {
            if (_currentLine.Length > 0)
            {
                string logContent = _currentLine.ToString();
                _activityLogs.Add(new ActivityLog("console", true, logContent));
                _currentLine.Clear();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                FlushCurrentLine();
            }
            base.Dispose(disposing);
        }
    }
}