using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace VPet_Simulator.Windows;

internal class PushToTalkHotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int Id = 0x5750;
    private readonly Window owner;
    private readonly DispatcherTimer releaseTimer;
    private HwndSource source;
    private bool registered;
    private uint registeredModifiers;
    private uint registeredKey;

    public PushToTalkHotkeyService(Window owner)
    {
        this.owner = owner;
        owner.SourceInitialized += Owner_SourceInitialized;
        owner.Closed += (_, _) => Dispose();
        releaseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        releaseTimer.Tick += (_, _) => CheckHotkeyReleased();
        EnsureHook();
    }

    public event Action Pressed;
    public event Action Released;

    public void Register(string hotkey)
    {
        EnsureHook();
        if (source == null)
            return;
        Unregister();
        if (!TryParseHotkey(hotkey, out uint modifiers, out uint key))
            return;
        registeredModifiers = modifiers;
        registeredKey = key;
        registered = RegisterHotKey(source.Handle, Id, modifiers, key);
    }

    public void Unregister()
    {
        if (registered && source != null)
            UnregisterHotKey(source.Handle, Id);
        registered = false;
        releaseTimer.Stop();
        registeredModifiers = 0;
        registeredKey = 0;
    }

    public void Dispose()
    {
        Unregister();
        releaseTimer.Stop();
        if (source != null)
        {
            source.RemoveHook(WndProc);
            source = null;
        }
    }

    private void Owner_SourceInitialized(object sender, EventArgs e)
    {
        EnsureHook();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == Id)
        {
            Pressed?.Invoke();
            releaseTimer.Start();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void EnsureHook()
    {
        if (source != null)
            return;
        IntPtr handle = new WindowInteropHelper(owner).Handle;
        if (handle == IntPtr.Zero)
            return;
        source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
    }

    private void CheckHotkeyReleased()
    {
        if (registeredKey == 0 || IsHotkeyDown())
            return;

        releaseTimer.Stop();
        Released?.Invoke();
    }

    private bool IsHotkeyDown()
    {
        if (!IsKeyDown((int)registeredKey))
            return false;
        if ((registeredModifiers & 0x0001) != 0 && !IsKeyDown(0x12))
            return false;
        if ((registeredModifiers & 0x0002) != 0 && !IsKeyDown(0x11))
            return false;
        if ((registeredModifiers & 0x0004) != 0 && !IsKeyDown(0x10))
            return false;
        if ((registeredModifiers & 0x0008) != 0 && !IsKeyDown(0x5B) && !IsKeyDown(0x5C))
            return false;
        return true;
    }

    private static bool IsKeyDown(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    private static bool TryParseHotkey(string text, out uint modifiers, out uint key)
    {
        modifiers = 0;
        key = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (string rawPart in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string part = rawPart.Trim();
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                modifiers |= 0x0002;
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                modifiers |= 0x0001;
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                modifiers |= 0x0004;
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase))
                modifiers |= 0x0008;
            else if (Enum.TryParse(part, true, out Key parsedKey))
                key = (uint)KeyInterop.VirtualKeyFromKey(parsedKey);
        }
        return key != 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
