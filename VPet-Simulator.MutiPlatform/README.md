# VPet 跨平台宿主 (Avalonia) 发布说明

`VPet-Simulator.MutiPlatform` 是 Windows 版 `VPet-Simulator.Windows` 的逐文件移植: 同样的文件、类名、
控件名、菜单、文案; 只有真正的平台差异那几行不同, 每处都在原位留了 `//跨平台:` 注释.
移植原则与目录对照见仓库根目录的 [ABI-Compatibility.md](../ABI-Compatibility.md).

## 运行

```bash
dotnet run --project VPet-Simulator.MutiPlatform
```

- 第一次启动前把 `mod/0000_core` (Windows 版仓库里的 `VPet-Simulator.Windows/mod/0000_core`) 放到 MOD 目录下.
- 所有平台统一使用 `VPet-Simulator.MutiPlatform` 运行文件所在目录: 本地 MOD 在 `mod/`,
  存档在 `Saves/`, 备份在 `Saves_BKP/`, 设置为根目录的 `Setting.lps` (多开为 `Setting-名字.lps`),
  缓存在 `cache/`. 路径与启动时的工作目录无关.
- 旧版本保存在用户目录的数据, 需要手动复制到运行文件所在目录才能继续使用.
- 多开: `--prefix <存档名>`; 加入访客表: `+connect_lobby <房间号>` (与 Windows 版相同).

## 已知差异与手工修法

**语言被写成了英文.** 早先的跨平台版本把进程区域钉成了 InvariantCulture, 语言匹配不到就把
`language#en:|` 写进了 `Setting.lps` 并持久化. 现在的版本与 Windows 版一样只换数字格式、保留区域名,
但 Windows 版的语义是 "`language` 不是 `null` 就是用户的选择", 程序分不清这一行是当年 bug 写的还是
用户选的, 所以**不会**自动改回. 手工修法二选一:

- 设置面板 → 系统 → 语言, 选回自己的语言;
- 或删掉 `Setting.lps` 里的 `language#en:|` 那一行, 下次启动会按系统语言重新匹配.

**鼠标穿透 (托盘菜单 / 设置里的 "鼠标穿透") 在 Linux/macOS 上是空操作.** 菜单项和状态照旧, 但 Windows
上靠的是 Win32 的 `WS_EX_TRANSPARENT`, 其他平台本期没有对应实现 (`MainWindow.SetTransparentHitThrough`).

**Linux 默认使用 X11/XWayland。** 即使登录的是 GNOME Wayland 会话，默认也通过 XWayland 运行。
主桌宠和联机访客的移动均成对提交 X/Y 坐标，避免 X11 异步更新位置时横向位移被下一次写入覆盖。

**跨工作区显示：** Linux 的“设置 → 图形 → 在所有工作区显示桌宠和访客”默认开启，
对主桌宠、已打开及随后加入的联机访客发送 EWMH `_NET_WM_DESKTOP` 请求。
关闭时请求回到当前工作区；设置随存档保存。实际效果由窗口管理器决定，置顶是独立设置。

**原生 Wayland 实验模式：** 按 VPet 的 LinePutScript 启动参数格式，传入 `wayland` 或 `wayland:|`：

```bash
./VPet-Simulator.MutiPlatform wayland
./VPet-Simulator.MutiPlatform 'wayland:|'
```

Steam 启动选项填写 `wayland` 即可。该开关只在 Linux 生效，删除参数恢复 X11/XWayland。
它显式选择 `Avalonia.Wayland`，没有 Wayland 服务时会启动失败，不会静默切回 X11。
合成器需要支持 Avalonia 要求的协议版本（包括 `xdg_wm_base >= 3`）；老版本 WSLg/Weston 可能不满足。
软件内部重启会保留原生 Wayland 模式。

- 主桌宠和访客：长按可提起区域，出现提起动作后移动鼠标，由合成器拖动窗口。
  系统接管时会结束应用内的提起状态；点击和其他长按交互仍保留。
- 自动走动、贴边、程序定位、位置恢复以及程序置顶不可用。不会把无效全局坐标保存为退出位置。
- 跨工作区开关不可用。在 GNOME 窗口菜单中选择“始终位于可见工作区”；自动设置需要桌面扩展配合。
- 这不是对 #582 聊天框问题的独立修复；原生 Wayland 的透明窗口、弹出菜单和输入仍需实机验收。

**设置 → 关于 里的鸣谢仍列着 Panuon.WPF.UI.** 页面文案与 Windows 版逐字相同 (文案对齐门禁要求如此),
跨平台版实际没有引用 Panuon —— 它的样式由 `Core.MutiPlatform/Display/basestyle.axaml` 等样式表提供.
要不要在跨平台版把这一条改掉由所有者决定.

**Steam.** 引用项目内 `Lib/Steamworks/2.5.2/net6.0` 中的官方 Facepunch.Steamworks DLL.
Windows x64 使用 `Facepunch.Steamworks.Win64.dll` 和 `steam_api64.dll`, 不再支持 Windows 32 位;
Linux x64 和 macOS (x64 / arm64) 使用 `Facepunch.Steamworks.Posix.dll`, 分别携带 `libsteam_api.so`
和 `libsteam_api.dylib`. 原生库来自同版本发布包的 `Release/Unity/redistributable_bin`, macOS 库包含 x64 / arm64.
构建和发布时自动复制对应库; 未指定 RID 时按当前系统选择, 跨平台发布请明确指定目标:

```bash
dotnet publish VPet-Simulator.MutiPlatform -c Release -r win-x64 --self-contained false
dotnet publish VPet-Simulator.MutiPlatform -c Release -r linux-x64 --self-contained false
dotnet publish VPet-Simulator.MutiPlatform -c Release -r osx-x64 --self-contained false
dotnet publish VPet-Simulator.MutiPlatform -c Release -r osx-arm64 --self-contained false
```

成就、云存档、创意工坊、访客表 (联机) 都是真实现, 没有 Steam 客户端时按 Windows 版同样的路径降级
(`IsSteamUser = false`). Linux/macOS 上的原生库加载还没有实机验证过.

**旧式 WPF MOD (`MainPlugin`) 不在这里跑.** 跨平台 MOD 走统一契约 `VPet-Simulator.Unified` (命名空间 `VPet_Simulator.Unified.Interface`),
同一个 dll 两个宿主都认; Windows 版的旧 MOD 不受影响.

运行日志在运行文件所在目录的 `vpet.log`.
