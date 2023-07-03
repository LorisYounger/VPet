using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows
{
    static partial class Win32
    {
        /// <summary>
        /// 扩展的窗口风格
        /// 这是 long 类型的，如果想要使用 int 类型请使用 <see cref="WindowExStyles"/> 类
        /// </summary>
        /// 代码：[Extended Window Styles (Windows)](https://msdn.microsoft.com/en-us/library/windows/desktop/ff700543(v=vs.85).aspx )
        /// code from [Extended Window Styles (Windows)](https://msdn.microsoft.com/en-us/library/windows/desktop/ff700543(v=vs.85).aspx )
        [Flags]
        public enum ExtendedWindowStyles : long
        {
            /// <summary>
            /// The window accepts drag-drop files
            /// </summary>
            WS_EX_ACCEPTFILES = 0x00000010L,

            /// <summary>
            /// Forces a top-level window onto the taskbar when the window is visible
            /// </summary>
            WS_EX_APPWINDOW = 0x00040000L,

            /// <summary>
            /// The window has a border with a sunken edge.
            /// </summary>
            WS_EX_CLIENTEDGE = 0x00000200L,

            /// <summary>
            /// Paints all descendants of a window in bottom-to-top painting order using double-buffering. For more information, see Remarks. This cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC.Windows 2000:  This style is not supported.
            /// </summary>
            WS_EX_COMPOSITED = 0x02000000L,

            /// <summary>
            /// The title bar of the window includes a question mark. When the user clicks the question mark, the cursor changes to a question mark with a pointer. If the user then clicks a child window, the child receives a WM_HELP message. The child window should pass the message to the parent window procedure, which should call the WinHelp function using the HELP_WM_HELP command. The Help application displays a pop-up window that typically contains help for the child window.WS_EX_CONTEXTHELP cannot be used with the WS_MAXIMIZEBOX or WS_MINIMIZEBOX styles.
            /// </summary>
            WS_EX_CONTEXTHELP = 0x00000400L,

            /// <summary>
            /// The window itself contains child windows that should take part in dialog box navigation. If this style is specified, the dialog manager recurses into children of this window when performing navigation operations such as handling the TAB key, an arrow key, or a keyboard mnemonic.
            /// </summary>
            WS_EX_CONTROLPARENT = 0x00010000L,

            /// <summary>
            /// The window has a double border; the window can, optionally, be created with a title bar by specifying the WS_CAPTION style in the dwStyle parameter.
            /// </summary>
            WS_EX_DLGMODALFRAME = 0x00000001L,

            /// <summary>
            /// The window is a layered window. This style cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC.Windows 8:  The WS_EX_LAYERED style is supported for top-level windows and child windows. Previous Windows versions support WS_EX_LAYERED only for top-level windows.
            /// </summary>
            WS_EX_LAYERED = 0x00080000,

            /// <summary>
            /// If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the horizontal origin of the window is on the right edge. Increasing horizontal values advance to the left.
            /// </summary>
            WS_EX_LAYOUTRTL = 0x00400000L,

            /// <summary>
            /// The window has generic left-aligned properties. This is the default.
            /// </summary>
            WS_EX_LEFT = 0x00000000L,

            /// <summary>
            /// If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the vertical scroll bar (if present) is to the left of the client area. For other languages, the style is ignored.
            /// </summary>
            WS_EX_LEFTSCROLLBAR = 0x00004000L,

            /// <summary>
            /// The window text is displayed using left-to-right reading-order properties. This is the default.
            /// </summary>
            WS_EX_LTRREADING = 0x00000000L,

            /// <summary>
            /// The window is a MDI child window.
            /// </summary>
            WS_EX_MDICHILD = 0x00000040L,

            /// <summary>
            /// A top-level window created with this style does not become the foreground window when the user clicks it. The system does not bring this window to the foreground when the user minimizes or closes the foreground window.To activate the window, use the SetActiveWindow or SetForegroundWindow function.The window does not appear on the taskbar by default. To force the window to appear on the taskbar, use the WS_EX_APPWINDOW style.
            /// </summary>
            WS_EX_NOACTIVATE = 0x08000000L,

            /// <summary>
            /// The window does not pass its window layout to its child windows.
            /// </summary>
            WS_EX_NOINHERITLAYOUT = 0x00100000L,

            /// <summary>
            /// The child window created with this style does not send the WM_PARENTNOTIFY message to its parent window when it is created or destroyed.
            /// </summary>
            WS_EX_NOPARENTNOTIFY = 0x00000004L,

            /// <summary>
            /// The window does not render to a redirection surface. This is for windows that do not have visible content or that use mechanisms other than surfaces to provide their visual.
            /// </summary>
            WS_EX_NOREDIRECTIONBITMAP = 0x00200000L,

            /// <summary>
            /// The window is an overlapped window.
            /// </summary>
            WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE),

            /// <summary>
            /// The window is palette window, which is a modeless dialog box that presents an array of commands.
            /// </summary>
            WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST),

            /// <summary>
            /// The window has generic "right-aligned" properties. This depends on the window class. This style has an effect only if the shell language is Hebrew, Arabic, or another language that supports reading-order alignment; otherwise, the style is ignored.Using the WS_EX_RIGHT style for static or edit controls has the same effect as using the SS_RIGHT or ES_RIGHT style, respectively. Using this style with button controls has the same effect as using BS_RIGHT and BS_RIGHTBUTTON styles.
            /// </summary>
            WS_EX_RIGHT = 0x00001000L,

            /// <summary>
            /// The vertical scroll bar (if present) is to the right of the client area. This is the default.
            /// </summary>
            WS_EX_RIGHTSCROLLBAR = 0x00000000L,

            /// <summary>
            /// If the shell language is Hebrew, Arabic, or another language that supports reading-order alignment, the window text is displayed using right-to-left reading-order properties. For other languages, the style is ignored.
            /// </summary>
            WS_EX_RTLREADING = 0x00002000L,

            /// <summary>
            /// The window has a three-dimensional border style intended to be used for items that do not accept user input.
            /// </summary>
            WS_EX_STATICEDGE = 0x00020000L,

            /// <summary>
            /// The window is intended to be used as a floating toolbar. A tool window has a title bar that is shorter than a normal title bar, and the window title is drawn using a smaller font. A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT+TAB. If a tool window has a system menu, its icon is not displayed on the title bar. However, you can display the system menu by right-clicking or by typing ALT+SPACE.
            /// </summary>
            WS_EX_TOOLWINDOW = 0x00000080L,

            /// <summary>
            /// The window should be placed above all non-topmost windows and should stay above them, even when the window is deactivated. To add or remove this style, use the SetWindowPos function.
            /// </summary>
            WS_EX_TOPMOST = 0x00000008L,

            /// <summary>
            /// The window should not be painted until siblings beneath the window (that were created by the same thread) have been painted. The window appears transparent because the bits of underlying sibling windows have already been painted.To achieve transparency without these restrictions, use the SetWindowRgn function.
            /// </summary>
            WS_EX_TRANSPARENT = 0x00000020L,

            /// <summary>
            /// The window has a border with a raised edge
            /// </summary>
            WS_EX_WINDOWEDGE = 0x00000100L
        }

        public static partial class User32
        {
            /// <summary>
            /// 获得指定窗口的信息
            /// </summary>
            /// <param name="hWnd">指定窗口的句柄</param>
            /// <param name="nIndex">需要获得的信息的类型 请使用<see cref="GetWindowLongFields"/></param>
            /// <returns></returns>
            // This static method is required because Win32 does not support
            // GetWindowLongPtr directly
            public static IntPtr GetWindowLongPtr(IntPtr hWnd, GetWindowLongFields nIndex) => GetWindowLongPtr(hWnd, (int)nIndex);

            /// <summary>
            /// 获得指定窗口的信息
            /// </summary>
            /// <param name="hWnd">指定窗口的句柄</param>
            /// <param name="nIndex">需要获得的信息的类型 请使用<see cref="GetWindowLongFields"/></param>
            /// <returns></returns>
            // This static method is required because Win32 does not support
            // GetWindowLongPtr directly
            public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
            {
                return IntPtr.Size > 4
#pragma warning disable CS0618 // 类型或成员已过时
                    ? GetWindowLongPtr_x64(hWnd, nIndex)
                    : new IntPtr(GetWindowLong(hWnd, nIndex));
#pragma warning restore CS0618 // 类型或成员已过时
            }

            /// <summary>
            /// 获得指定窗口的信息
            /// </summary>
            /// <param name="hWnd">指定窗口的句柄</param>
            /// <param name="nIndex">需要获得的信息的类型 请使用<see cref="GetWindowLongFields"/></param>
            /// <returns></returns>
            [Obsolete("请使用 GetWindowLongPtr 解决 x86 和 x64 需要使用不同方法")]
            [DllImport(LibraryName, CharSet = Properties.BuildCharSet)]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            /// <summary>
            /// 获得指定窗口的信息
            /// </summary>
            /// <param name="hWnd">指定窗口的句柄</param>
            /// <param name="nIndex">需要获得的信息的类型 请使用<see cref="GetWindowLongFields"/></param>
            /// <returns></returns>
            [Obsolete("请使用 GetWindowLongPtr 解决 x86 和 x64 需要使用不同方法")]
            [DllImport(LibraryName, CharSet = Properties.BuildCharSet, EntryPoint = "GetWindowLongPtr")]
            public static extern IntPtr GetWindowLongPtr_x64(IntPtr hWnd, int nIndex);

            /// <summary>
            /// 改变指定窗口的属性
            /// </summary>
            /// <param name="hWnd">窗口句柄</param>
            /// <param name="nIndex">
            /// 指定将设定的大于等于0的偏移值。有效值的范围从0到额外类的存储空间的字节数减4：例如若指定了12或多于12个字节的额外窗口存储空间，则应设索引位8来访问第三个4字节，同样设置0访问第一个4字节，4访问第二个4字节。要设置其他任何值，可以指定下面值之一
            /// 从 GetWindowLongFields 可以找到所有的值
            /// </param>
            /// <param name="dwNewLong">指定的替换值</param>
            /// <returns></returns>
            public static IntPtr SetWindowLongPtr(IntPtr hWnd, GetWindowLongFields nIndex, IntPtr dwNewLong) => SetWindowLongPtr(hWnd, (int)nIndex, dwNewLong);

            /// <summary>
            /// 改变指定窗口的属性
            /// </summary>
            /// <param name="hWnd">窗口句柄</param>
            /// <param name="nIndex">指定将设定的大于等于0的偏移值。有效值的范围从0到额外类的存储空间的字节数减4：例如若指定了12或多于12个字节的额外窗口存储空间，则应设索引位8来访问第三个4字节，同样设置0访问第一个4字节，4访问第二个4字节。要设置其他任何值，可以指定下面值之一
            /// 从 GetWindowLongFields 可以找到所有的值
            /// <para>
            /// GetWindowLongFields.GWL_EXSTYLE             -20    设定一个新的扩展风格。 </para>
            /// <para>GWL_HINSTANCE     -6	   设置一个新的应用程序实例句柄。</para>
            /// <para>GWL_ID            -12    设置一个新的窗口标识符。</para>
            /// <para>GWL_STYLE         -16    设定一个新的窗口风格。</para>
            /// <para>GWL_USERDATA      -21    设置与窗口有关的32位值。每个窗口均有一个由创建该窗口的应用程序使用的32位值。</para>
            /// <para>GWL_WNDPROC       -4    为窗口设定一个新的处理函数。</para>
            /// <para>GWL_HWNDPARENT    -8    改变子窗口的父窗口,应使用SetParent函数</para>
            /// </param>
            /// <param name="dwNewLong">指定的替换值</param>
            /// <returns></returns>
            // This static method is required because Win32 does not support
            // GetWindowLongPtr directly
            public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
            {

                return IntPtr.Size > 4
#pragma warning disable CS0618 // 类型或成员已过时
                    ? SetWindowLongPtr_x64(hWnd, nIndex, dwNewLong)
                    : new IntPtr(SetWindowLong(hWnd, nIndex, dwNewLong.ToInt32()));
#pragma warning restore CS0618 // 类型或成员已过时
            }

            /// <summary>
            /// 改变指定窗口的属性
            /// </summary>
            /// <param name="hWnd">窗口句柄</param>
            /// <param name="nIndex">指定将设定的大于等于0的偏移值。有效值的范围从0到额外类的存储空间的字节数减4：例如若指定了12或多于12个字节的额外窗口存储空间，则应设索引位8来访问第三个4字节，同样设置0访问第一个4字节，4访问第二个4字节。要设置其他任何值，可以指定下面值之一
            /// 从 GetWindowLongFields 可以找到所有的值
            /// <para>
            /// GetWindowLongFields.GWL_EXSTYLE             -20    设定一个新的扩展风格。 </para>
            /// <para>GWL_HINSTANCE     -6	   设置一个新的应用程序实例句柄。</para>
            /// <para>GWL_ID            -12    设置一个新的窗口标识符。</para>
            /// <para>GWL_STYLE         -16    设定一个新的窗口风格。</para>
            /// <para>GWL_USERDATA      -21    设置与窗口有关的32位值。每个窗口均有一个由创建该窗口的应用程序使用的32位值。</para>
            /// <para>GWL_WNDPROC       -4    为窗口设定一个新的处理函数。</para>
            /// <para>GWL_HWNDPARENT    -8    改变子窗口的父窗口,应使用SetParent函数</para>
            /// </param>
            /// <param name="dwNewLong">指定的替换值</param>
            /// <returns></returns>
            [Obsolete("请使用 SetWindowLongPtr 解决 x86 和 x64 需要使用不同方法")]
            [DllImport(LibraryName, CharSet = Properties.BuildCharSet)]
            public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            /// <summary>
            /// 改变指定窗口的属性
            /// </summary>
            /// <param name="hWnd">窗口句柄</param>
            /// <param name="nIndex">指定将设定的大于等于0的偏移值。有效值的范围从0到额外类的存储空间的字节数减4：例如若指定了12或多于12个字节的额外窗口存储空间，则应设索引位8来访问第三个4字节，同样设置0访问第一个4字节，4访问第二个4字节。要设置其他任何值，可以指定下面值之一
            /// 从 GetWindowLongFields 可以找到所有的值
            /// <para>
            /// GetWindowLongFields.GWL_EXSTYLE             -20    设定一个新的扩展风格。 </para>
            /// <para>GWL_HINSTANCE     -6	   设置一个新的应用程序实例句柄。</para>
            /// <para>GWL_ID            -12    设置一个新的窗口标识符。</para>
            /// <para>GWL_STYLE         -16    设定一个新的窗口风格。</para>
            /// <para>GWL_USERDATA      -21    设置与窗口有关的32位值。每个窗口均有一个由创建该窗口的应用程序使用的32位值。</para>
            /// <para>GWL_WNDPROC       -4    为窗口设定一个新的处理函数。</para>
            /// <para>GWL_HWNDPARENT    -8    改变子窗口的父窗口,应使用SetParent函数</para>
            /// </param>
            /// <param name="dwNewLong">指定的替换值</param>
            /// <returns></returns>
            [DllImport(LibraryName, CharSet = Properties.BuildCharSet, EntryPoint = "SetWindowLongPtr")]
            [Obsolete("请使用 SetWindowLongPtr 解决 x86 和 x64 需要使用不同方法")]
            public static extern IntPtr SetWindowLongPtr_x64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
            public const string LibraryName = "user32";
        }

        internal static class Properties
        {
#if !ANSI
            public const CharSet BuildCharSet = CharSet.Unicode;
#else
            public const CharSet BuildCharSet = CharSet.Ansi;
#endif
        }

        /// <summary>
        /// 用于在 <see cref="Win32.GetWindowLong"/> 的 int index 传入
        /// </summary>
        /// 代码：[GetWindowLong function (Windows)](https://msdn.microsoft.com/en-us/library/windows/desktop/ms633584(v=vs.85).aspx )
        public enum GetWindowLongFields
        {
            /// <summary>
            /// 设定一个新的扩展风格
            /// Retrieves the extended window styles
            /// </summary>
            GWL_EXSTYLE = -20,

            /// <summary>
            /// 设置一个新的应用程序实例句柄
            /// Retrieves a handle to the application instance
            /// </summary>
            GWL_HINSTANCE = -6,

            /// <summary>
            /// 改变子窗口的父窗口
            /// Retrieves a handle to the parent window, if any
            /// </summary>
            GWL_HWNDPARENT = -8,

            /// <summary>
            ///  设置一个新的窗口标识符
            /// Retrieves the identifier of the window
            /// </summary>
            GWL_ID = -12,

            /// <summary>
            /// 设定一个新的窗口风格
            /// Retrieves the window styles
            /// </summary>
            GWL_STYLE = -16,

            /// <summary>
            /// 设置与窗口有关的32位值。每个窗口均有一个由创建该窗口的应用程序使用的32位值
            /// Retrieves the user data associated with the window. This data is intended for use by the application that created the window. Its value is initially zero
            /// </summary>
            GWL_USERDATA = -21,

            /// <summary>
            /// 为窗口设定一个新的处理函数
            /// Retrieves the address of the window procedure, or a handle representing the address of the window procedure. You must use the CallWindowProc function to call the window procedure
            /// </summary>
            GWL_WNDPROC = -4,
        }
    }

    static partial class Win32
    {
        public static class Dwmapi
        {
            public const string LibraryName = "Dwmapi.dll";

            [DllImport(LibraryName, ExactSpelling = true, PreserveSig = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DwmIsCompositionEnabled();
        }
    }

    static partial class Win32
    {
        public enum WM
        {
            NULL = 0x0000,
            CREATE = 0x0001,
            DESTROY = 0x0002,
            MOVE = 0x0003,
            SIZE = 0x0005,
            ACTIVATE = 0x0006,
            SETFOCUS = 0x0007,
            KILLFOCUS = 0x0008,
            ENABLE = 0x000A,
            SETREDRAW = 0x000B,
            SETTEXT = 0x000C,
            GETTEXT = 0x000D,
            GETTEXTLENGTH = 0x000E,
            PAINT = 0x000F,
            CLOSE = 0x0010,
            QUERYENDSESSION = 0x0011,
            QUERYOPEN = 0x0013,
            ENDSESSION = 0x0016,
            QUIT = 0x0012,
            ERASEBKGND = 0x0014,
            SYSCOLORCHANGE = 0x0015,
            SHOWWINDOW = 0x0018,
            WININICHANGE = 0x001A,
            SETTINGCHANGE = WININICHANGE,
            DEVMODECHANGE = 0x001B,
            ACTIVATEAPP = 0x001C,
            FONTCHANGE = 0x001D,
            TIMECHANGE = 0x001E,
            CANCELMODE = 0x001F,
            SETCURSOR = 0x0020,
            MOUSEACTIVATE = 0x0021,
            CHILDACTIVATE = 0x0022,
            QUEUESYNC = 0x0023,
            GETMINMAXINFO = 0x0024,
            PAINTICON = 0x0026,
            ICONERASEBKGND = 0x0027,
            NEXTDLGCTL = 0x0028,
            SPOOLERSTATUS = 0x002A,
            DRAWITEM = 0x002B,
            MEASUREITEM = 0x002C,
            DELETEITEM = 0x002D,
            VKEYTOITEM = 0x002E,
            CHARTOITEM = 0x002F,
            SETFONT = 0x0030,
            GETFONT = 0x0031,
            SETHOTKEY = 0x0032,
            GETHOTKEY = 0x0033,
            QUERYDRAGICON = 0x0037,
            COMPAREITEM = 0x0039,
            GETOBJECT = 0x003D,
            COMPACTING = 0x0041,
            COMMNOTIFY = 0x0044 /* no longer suported */,
            WINDOWPOSCHANGING = 0x0046,
            WINDOWPOSCHANGED = 0x0047,
            POWER = 0x0048,
            COPYDATA = 0x004A,
            CANCELJOURNAL = 0x004B,
            NOTIFY = 0x004E,
            INPUTLANGCHANGEREQUEST = 0x0050,
            INPUTLANGCHANGE = 0x0051,
            TCARD = 0x0052,
            HELP = 0x0053,
            USERCHANGED = 0x0054,
            NOTIFYFORMAT = 0x0055,
            CONTEXTMENU = 0x007B,
            STYLECHANGING = 0x007C,
            STYLECHANGED = 0x007D,
            DISPLAYCHANGE = 0x007E,
            GETICON = 0x007F,
            SETICON = 0x0080,
            NCCREATE = 0x0081,
            NCDESTROY = 0x0082,
            NCCALCSIZE = 0x0083,
            NCHITTEST = 0x0084,
            NCPAINT = 0x0085,
            NCACTIVATE = 0x0086,
            GETDLGCODE = 0x0087,
            SYNCPAINT = 0x0088,
            NCMOUSEMOVE = 0x00A0,
            NCLBUTTONDOWN = 0x00A1,
            NCLBUTTONUP = 0x00A2,
            NCLBUTTONDBLCLK = 0x00A3,
            NCRBUTTONDOWN = 0x00A4,
            NCRBUTTONUP = 0x00A5,
            NCRBUTTONDBLCLK = 0x00A6,
            NCMBUTTONDOWN = 0x00A7,
            NCMBUTTONUP = 0x00A8,
            NCMBUTTONDBLCLK = 0x00A9,
            NCXBUTTONDOWN = 0x00AB,
            NCXBUTTONUP = 0x00AC,
            NCXBUTTONDBLCLK = 0x00AD,
            INPUT_DEVICE_CHANGE = 0x00FE,
            INPUT = 0x00FF,
            KEYFIRST = 0x0100,
            KEYDOWN = 0x0100,
            KEYUP = 0x0101,
            CHAR = 0x0102,
            DEADCHAR = 0x0103,
            SYSKEYDOWN = 0x0104,
            SYSKEYUP = 0x0105,
            SYSCHAR = 0x0106,
            SYSDEADCHAR = 0x0107,
            UNICHAR = 0x0109,
            KEYLAST = 0x0109,
            IME_STARTCOMPOSITION = 0x010D,
            IME_ENDCOMPOSITION = 0x010E,
            IME_COMPOSITION = 0x010F,
            IME_KEYLAST = 0x010F,
            INITDIALOG = 0x0110,
            COMMAND = 0x0111,
            SYSCOMMAND = 0x0112,
            TIMER = 0x0113,
            HSCROLL = 0x0114,
            VSCROLL = 0x0115,
            INITMENU = 0x0116,
            INITMENUPOPUP = 0x0117,
            GESTURE = 0x0119,
            GESTURENOTIFY = 0x011A,
            MENUSELECT = 0x011F,
            MENUCHAR = 0x0120,
            ENTERIDLE = 0x0121,
            MENURBUTTONUP = 0x0122,
            MENUDRAG = 0x0123,
            MENUGETOBJECT = 0x0124,
            UNINITMENUPOPUP = 0x0125,
            MENUCOMMAND = 0x0126,
            CHANGEUISTATE = 0x0127,
            UPDATEUISTATE = 0x0128,
            QUERYUISTATE = 0x0129,
            CTLCOLORMSGBOX = 0x0132,
            CTLCOLOREDIT = 0x0133,
            CTLCOLORLISTBOX = 0x0134,
            CTLCOLORBTN = 0x0135,
            CTLCOLORDLG = 0x0136,
            CTLCOLORSCROLLBAR = 0x0137,
            CTLCOLORSTATIC = 0x0138,
            MOUSEFIRST = 0x0200,
            MOUSEMOVE = 0x0200,
            LBUTTONDOWN = 0x0201,
            LBUTTONUP = 0x0202,
            LBUTTONDBLCLK = 0x0203,
            RBUTTONDOWN = 0x0204,
            RBUTTONUP = 0x0205,
            RBUTTONDBLCLK = 0x0206,
            MBUTTONDOWN = 0x0207,
            MBUTTONUP = 0x0208,
            MBUTTONDBLCLK = 0x0209,
            MOUSEWHEEL = 0x020A,
            XBUTTONDOWN = 0x020B,
            XBUTTONUP = 0x020C,
            XBUTTONDBLCLK = 0x020D,
            MOUSEHWHEEL = 0x020E,
            MOUSELAST = 0x020E,
            PARENTNOTIFY = 0x0210,
            ENTERMENULOOP = 0x0211,
            EXITMENULOOP = 0x0212,
            NEXTMENU = 0x0213,
            SIZING = 0x0214,
            CAPTURECHANGED = 0x0215,
            MOVING = 0x0216,
            POWERBROADCAST = 0x0218,
            DEVICECHANGE = 0x0219,
            MDICREATE = 0x0220,
            MDIDESTROY = 0x0221,
            MDIACTIVATE = 0x0222,
            MDIRESTORE = 0x0223,
            MDINEXT = 0x0224,
            MDIMAXIMIZE = 0x0225,
            MDITILE = 0x0226,
            MDICASCADE = 0x0227,
            MDIICONARRANGE = 0x0228,
            MDIGETACTIVE = 0x0229,
            MDISETMENU = 0x0230,
            ENTERSIZEMOVE = 0x0231,
            EXITSIZEMOVE = 0x0232,
            DROPFILES = 0x0233,
            MDIREFRESHMENU = 0x0234,
            POINTERDEVICECHANGE = 0x238,
            POINTERDEVICEINRANGE = 0x239,
            POINTERDEVICEOUTOFRANGE = 0x23A,
            TOUCH = 0x0240,
            NCPOINTERUPDATE = 0x0241,
            NCPOINTERDOWN = 0x0242,
            NCPOINTERUP = 0x0243,
            POINTERUPDATE = 0x0245,
            POINTERDOWN = 0x0246,
            POINTERUP = 0x0247,
            POINTERENTER = 0x0249,
            POINTERLEAVE = 0x024A,
            POINTERACTIVATE = 0x024B,
            POINTERCAPTURECHANGED = 0x024C,
            TOUCHHITTESTING = 0x024D,
            POINTERWHEEL = 0x024E,
            POINTERHWHEEL = 0x024F,
            IME_SETCONTEXT = 0x0281,
            IME_NOTIFY = 0x0282,
            IME_CONTROL = 0x0283,
            IME_COMPOSITIONFULL = 0x0284,
            IME_SELECT = 0x0285,
            IME_CHAR = 0x0286,
            IME_REQUEST = 0x0288,
            IME_KEYDOWN = 0x0290,
            IME_KEYUP = 0x0291,
            MOUSEHOVER = 0x02A1,
            MOUSELEAVE = 0x02A3,
            NCMOUSEHOVER = 0x02A0,
            NCMOUSELEAVE = 0x02A2,
            WTSSESSION_CHANGE = 0x02B1,
            TABLET_FIRST = 0x02c0,
            TABLET_LAST = 0x02df,
            DPICHANGED = 0x02E0,
            CUT = 0x0300,
            COPY = 0x0301,
            PASTE = 0x0302,
            CLEAR = 0x0303,
            UNDO = 0x0304,
            RENDERFORMAT = 0x0305,
            RENDERALLFORMATS = 0x0306,
            DESTROYCLIPBOARD = 0x0307,
            DRAWCLIPBOARD = 0x0308,
            PAINTCLIPBOARD = 0x0309,
            VSCROLLCLIPBOARD = 0x030A,
            SIZECLIPBOARD = 0x030B,
            ASKCBFORMATNAME = 0x030C,
            CHANGECBCHAIN = 0x030D,
            HSCROLLCLIPBOARD = 0x030E,
            QUERYNEWPALETTE = 0x030F,
            PALETTEISCHANGING = 0x0310,
            PALETTECHANGED = 0x0311,
            HOTKEY = 0x0312,
            PRINT = 0x0317,
            PRINTCLIENT = 0x0318,
            APPCOMMAND = 0x0319,
            THEMECHANGED = 0x031A,
            CLIPBOARDUPDATE = 0x031D,
            DWMCOMPOSITIONCHANGED = 0x031E,
            DWMNCRENDERINGCHANGED = 0x031F,
            DWMCOLORIZATIONCOLORCHANGED = 0x0320,
            DWMWINDOWMAXIMIZEDCHANGE = 0x0321,
            DWMSENDICONICTHUMBNAIL = 0x0323,
            DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326,
            GETTITLEBARINFOEX = 0x033F,
            HANDHELDFIRST = 0x0358,
            HANDHELDLAST = 0x035F,
            AFXFIRST = 0x0360,
            AFXLAST = 0x037F,
            PENWINFIRST = 0x0380,
            PENWINLAST = 0x038F,
            APP = 0x8000,
            USER = 0x0400
        }
    }

    class PerformanceDesktopTransparentWindow
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct STYLESTRUCT
        {
            public int styleOld;
            public int styleNew;
        }
    }
}
