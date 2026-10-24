using System;
using System.IO;
using System.Runtime.InteropServices;
#if VPET_AVALONIA
namespace VPet_Simulator.MutiPlatform;
#else
namespace VPet_Simulator.Windows;
#endif
static partial class Win32
{
public static class AuthenticodeHelper
        {
            // WINTRUST_ACTION_GENERIC_VERIFY_V2
            private static readonly Guid WinTrustActionGenericVerifyV2 =
                new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

            private const uint WTD_UI_NONE = 2;
            private const uint WTD_REVOKE_NONE = 0;
            private const uint WTD_CHOICE_FILE = 1;
            private const uint WTD_STATEACTION_IGNORE = 0;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct WINTRUST_FILE_INFO
            {
                public uint cbStruct;

                [MarshalAs(UnmanagedType.LPWStr)]
                public string pcwszFilePath;

                public IntPtr hFile;
                public IntPtr pgKnownSubject;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct WINTRUST_DATA
            {
                public uint cbStruct;
                public IntPtr pPolicyCallbackData;
                public IntPtr pSIPClientData;
                public uint dwUIChoice;
                public uint fdwRevocationChecks;
                public uint dwUnionChoice;
                public IntPtr pFile;
                public uint dwStateAction;
                public IntPtr hWVTStateData;
                public IntPtr pwszURLReference;
                public uint dwProvFlags;
                public uint dwUIContext;
            }

            [DllImport(
                "wintrust.dll",
                ExactSpelling = true,
                CharSet = CharSet.Unicode)]
            private static extern int WinVerifyTrust(
                IntPtr hwnd,
                ref Guid pgActionID,
                ref WINTRUST_DATA pWVTData);

            /// <summary>
            /// 使用 Windows Authenticode 策略验证文件签名。
            /// 返回 true 表示 Windows 信任验证成功。
            /// </summary>
            public static bool Verify(string filePath)
            {
                filePath = Path.GetFullPath(filePath);

                var fileInfo = new WINTRUST_FILE_INFO
                {
                    cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                    pcwszFilePath = filePath,
                    hFile = IntPtr.Zero,
                    pgKnownSubject = IntPtr.Zero
                };

                IntPtr fileInfoPtr =
                    Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());

                bool structureCreated = false;

                try
                {
                    Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);
                    structureCreated = true;

                    var trustData = new WINTRUST_DATA
                    {
                        cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),

                        // 完全不弹 Windows UI
                        dwUIChoice = WTD_UI_NONE,

                        // 暂不额外强制吊销检查
                        fdwRevocationChecks = WTD_REVOKE_NONE,

                        // 验证文件
                        dwUnionChoice = WTD_CHOICE_FILE,
                        pFile = fileInfoPtr,

                        // 不保存 WinTrust state
                        dwStateAction = WTD_STATEACTION_IGNORE,

                        hWVTStateData = IntPtr.Zero,
                        pwszURLReference = IntPtr.Zero,

                        // 使用系统默认 Authenticode 策略
                        dwProvFlags = 0,

                        dwUIContext = 0
                    };

                    Guid action = WinTrustActionGenericVerifyV2;

                    // INVALID_HANDLE_VALUE + WTD_UI_NONE：
                    // 不允许验证过程中与用户交互。
                    int result = WinVerifyTrust(
                        new IntPtr(-1),
                        ref action,
                        ref trustData);

                    // WinVerifyTrust 只有 0 才代表成功。
                    return result == 0;
                }
                finally
                {
                    if (structureCreated)
                        Marshal.DestroyStructure<WINTRUST_FILE_INFO>(fileInfoPtr);

                    Marshal.FreeHGlobal(fileInfoPtr);
                }
            }
        }
}
