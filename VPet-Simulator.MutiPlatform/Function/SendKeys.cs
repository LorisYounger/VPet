using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VPet_Simulator.MutiPlatform
{
    /// <summary>
    /// 自定按钮里的按键模拟
    /// </summary>
    /// 跨平台: Windows 版直接用 WinForms 的 SendKeys.SendWait, 这边没有 WinForms, 用 user32 的 SendInput 照着 SendKeys 的写法实现一遍:
    /// 普通字符逐个按 Unicode 发, {ENTER} {ESC} {BS} {F1}… 这类花括号名换成虚拟键, ^ + % 分别是 Ctrl / Shift / Alt, 加在下一个键或 (…) 一组上.
    /// Linux/macOS 没有对应实现, 抛异常, 由调用方按 Windows 版同一条路报"快捷键运行失败"
    static class SendKeys
    {
        //INPUT 里是个联合体, 大小要按最大的 MOUSEINPUT 算, 否则 SendInput 报参数错误
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_UNICODE = 0x0004;

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        static readonly Dictionary<string, ushort> Names = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ENTER"] = 0x0D, ["TAB"] = 0x09, ["ESC"] = 0x1B, ["ESCAPE"] = 0x1B, ["BS"] = 0x08, ["BACKSPACE"] = 0x08, ["BKSP"] = 0x08,
            ["DEL"] = 0x2E, ["DELETE"] = 0x2E, ["INS"] = 0x2D, ["INSERT"] = 0x2D, ["HOME"] = 0x24, ["END"] = 0x23,
            ["PGUP"] = 0x21, ["PGDN"] = 0x22, ["UP"] = 0x26, ["DOWN"] = 0x28, ["LEFT"] = 0x25, ["RIGHT"] = 0x27,
            ["PRTSC"] = 0x2C, ["SCROLLLOCK"] = 0x91, ["CAPSLOCK"] = 0x14, ["NUMLOCK"] = 0x90, ["BREAK"] = 0x03, ["HELP"] = 0x2F,
            ["SPACE"] = 0x20, ["F1"] = 0x70, ["F2"] = 0x71, ["F3"] = 0x72, ["F4"] = 0x73, ["F5"] = 0x74, ["F6"] = 0x75, ["F7"] = 0x76,
            ["F8"] = 0x77, ["F9"] = 0x78, ["F10"] = 0x79, ["F11"] = 0x7A, ["F12"] = 0x7B, ["F13"] = 0x7C, ["F14"] = 0x7D, ["F15"] = 0x7E, ["F16"] = 0x7F,
        };

        const ushort VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12;

        /// <summary>
        /// 按 SendKeys 的写法模拟按键
        /// </summary>
        public static void SendWait(string keys)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("SendKeys");
            var inputs = new List<INPUT>();
            var held = new List<ushort>();
            int i = 0;
            while (i < keys.Length)
            {
                char c = keys[i];
                switch (c)
                {
                    case '^': held.Add(VK_CONTROL); i++; continue;
                    case '+': held.Add(VK_SHIFT); i++; continue;
                    case '%': held.Add(VK_MENU); i++; continue;
                    case '(':
                    {
                        //一组键共用前面的修饰键
                        int end = keys.IndexOf(')', i + 1);
                        if (end < 0) end = keys.Length;
                        var mods = held.ToArray(); held.Clear();
                        foreach (var vk in mods) inputs.Add(Key(vk, false));
                        int j = i + 1;
                        while (j < end)
                            j = AppendOne(keys, j, inputs);
                        for (int k = mods.Length - 1; k >= 0; k--) inputs.Add(Key(mods[k], true));
                        i = end + 1;
                        continue;
                    }
                    default:
                    {
                        var mods = held.ToArray(); held.Clear();
                        foreach (var vk in mods) inputs.Add(Key(vk, false));
                        i = AppendOne(keys, i, inputs);
                        for (int k = mods.Length - 1; k >= 0; k--) inputs.Add(Key(mods[k], true));
                        continue;
                    }
                }
            }
            if (inputs.Count == 0)
                return;
            var array = inputs.ToArray();
            if (SendInput((uint)array.Length, array, Marshal.SizeOf<INPUT>()) != array.Length)
                throw new InvalidOperationException("SendInput: " + Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// 读一个键 (普通字符或 {名字}), 把按下/抬起放进 inputs, 返回下一个位置
        /// </summary>
        static int AppendOne(string keys, int i, List<INPUT> inputs)
        {
            if (keys[i] == '{')
            {
                int end = keys.IndexOf('}', i + 1);
                if (end < 0) end = keys.Length;
                var name = keys.Substring(i + 1, end - i - 1);
                //{}} {{} 这种是转义的花括号本身
                if (name.Length == 0 && end + 1 < keys.Length && keys[end + 1] == '}')
                {
                    name = "}"; end++;
                }
                if (Names.TryGetValue(name, out var vk))
                {
                    inputs.Add(Key(vk, false)); inputs.Add(Key(vk, true));
                }
                else if (name.Length == 1)
                {
                    inputs.Add(Unicode(name[0], false)); inputs.Add(Unicode(name[0], true));
                }
                else
                    throw new ArgumentException("SendKeys: " + name);
                return end + 1;
            }
            inputs.Add(Unicode(keys[i], false)); inputs.Add(Unicode(keys[i], true));
            return i + 1;
        }

        static INPUT Key(ushort vk, bool up) => new INPUT { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = up ? KEYEVENTF_KEYUP : 0 } } };
        static INPUT Unicode(char c, bool up) => new INPUT { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE | (up ? KEYEVENTF_KEYUP : 0) } } };
    }
}
