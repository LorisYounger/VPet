# VPet ABI 兼容性规则

> 这份文档是硬性约束, 不是建议. 修改 `VPet-Simulator.Core` 或
> `VPet-Simulator.Windows.Interface` 之前必须先读完.

## 为什么需要这份文档

VPet 的代码型 MOD 是以**编译好的 DLL** 形式分发的 (`CoreMOD.cs` 用
`Assembly.LoadFrom` 加载), 很多 MOD 作者已经不再活跃, 无法重新编译.

更糟的是 `CoreMOD.cs:404` 的 `catch` 会把加载异常**吞掉**, `ignoreError` 时连日志
都没有. 所以一次 ABI 破坏的表现不是"报错", 而是"用户装了 MOD 发现没生效", 几周后
才会有人报 issue, 而且无法回溯是哪次改动引入的.

因此: **Core 和 Interface 的公开 API 表面只许增, 不许改, 不许减.**

## 三条铁律

### R1 公开字段永远是字段, 永远在原来的声明类型上

.NET 没有 `MemberForwardedTo`. 一旦公开字段变成属性, 或者被挪到另一个程序集里的
基类上, 老 MOD 的 `ldfld` / `stfld` 指令就会在 JIT 期抛 `MissingFieldException`.

已实测确认会炸的改动:

| 改动 | 实际后果 |
|---|---|
| `public Point Locate;` → `public Point Locate { get; set; }` | `MissingFieldException: TouchArea.Locate` |
| `GameCore.Controller` 字段挪到另一程序集的 `GameCoreBase` | `MissingFieldException: GameCore.Controller` |
| `SayInfo.GraphName` 字段挪到另一程序集的 `SayInfoBase` | `MissingFieldException: SayInfo.GraphName` |

注意第二、三条: **"成员上移到基类"并不安全**, 即使 CLR 的字段解析理论上会沿继承链
向上查找 —— 跨程序集时它并不会. 这一点已用冻结的冒烟 MOD 实测复现.

### R2 基类与接口列表只许增不许改, 新增接口一律显式实现

给 `Main` / `TouchArea` / `WorkTimer` 追加接口是**安全**的, 因为显式接口实现在元数据里
的名字是 `VPet_Simulator.Core.IFoo.Member`, 与同名公开字段零冲突:

```csharp
// 正确: 字段原地不动, 接口视图走显式实现
public WorkingState State = WorkingState.Nomal;
PetState IPetHost.State { get => (PetState)State; set => State = (WorkingState)value; }
```

而**更换基类**是破坏性的. 尤其不要为了共享逻辑去抽公共基类 —— `Main` 的单继承已经
被 `ContentControlX` 占死, 硬抽基类的结果必然是被迫把字段改成属性 (这正是
`a470387e` 那次失败重构的根因).

### R3 不得在 `MainPlugin` 与 MOD 之间插入中间基类

`CoreMOD.cs:390` 的判据是:

```csharp
if (exportedType.BaseType == typeof(MainPlugin))
```

**只认直接派生一层**. 插入任何中间基类, 或者把 `MainPlugin` 改成接口, 都会让所有现有
MOD 被静默跳过.

## 禁止清单

以下改动看起来都像"改进", 但会静默改变行为且极难回溯. 当前项目的行为**就是**规格
说明书 —— 没有文档, 没有历史测试, MOD 生态依赖的是具体行为.

| # | 禁止 | 位置 | 理由 |
|---|---|---|---|
| N1 | 统一 `FindGraph` / `FindGraphs` 的状态回退边界 | `GraphCore.cs` | 四处刻意的不对称: `FindGraph` 在 `mode == Ill` 时提前返回 null; 向上回退边界一个是 `i >= 1` 一个是 `i >= 0`; 兜底时一个排除 Ill 一个返回全量. 社区 MOD 动画包已适配当前行为, 改了会让某些动画静默不播 |
| N2 | 删掉 `MainDisplay.cs` 双缓冲切换后的 `GC.Collect()` | `MainDisplay.cs` | 它可能在掩盖真实的 `BitmapSource` 泄漏. 跨平台侧**新写**的渲染层可以不加, Windows 版一个字不许动 |
| N3 | "修复" `MoveWindows` 的 `ZoomRatio` 约定 | `MWController.cs` 乘 / `MainLogic.cs` 除 | `IController` 是公开接口, MOD 可以自己实现. 跨平台实现必须遵守同样的约定 |
| N4 | 统一各项目的 TargetFramework | 全仓 | 收益为零, 风险非零. 现状靠 roll-forward 正常工作 |
| N5 | 把类型从 Core 搬到另一个程序集 (哪怕加 `TypeForwardedTo`) | — | 类型转发救不了字段→属性、救不了成员上移到跨程序集基类、救不了默认接口成员变显式实现. 跨平台共享请用**共享源码编译**, 见下 |
| N6 | 字段改属性 / `virtual` 改 `abstract` / 收紧可访问性 / 加 `sealed` / 改参数名 | 任何 public / protected 成员 | 字段→属性是"编译得过、运行时炸", 最阴险的一类 |
| N7 | 在 `MainPlugin` 和 MOD 之间插中间基类, 或把 `MainPlugin` 改成接口 | `CoreMOD.cs` | 见 R3 |
| N8 | 用 `Random.Shared` 替换 `Function.Rnd` | `Function.cs` | 它是 public static **字段**, MOD 可以读取甚至替换它来做确定性测试 |
| N9 | 移除 `MainWindow.xaml.cs` 里 hook `WM_STYLECHANGING` 的那段 | `MainWindow.xaml.cs` | 注释里逐字解释了为什么必须这样 (WPF 在 `AllowsTransparency=false` 时会摘掉 `WS_EX_LAYERED`). 它很脏, 但它是全项目最有价值的一段脏代码 |
| N10 | 给 `MainDisplay.cs` 里 `(IGraph)PetGridTag` 的强转加 null 检查 (Windows 侧) | `MainDisplay.cs` | 会把崩溃变成静默空白. 当前靠 `Load_4_Start` 预塞动画保证非 null; 不变式一旦被破坏, 你要的是崩溃 (有堆栈可定位), 不是静默降级 |
| N11 | 给现有 Windows 侧项目引入 Avalonia 引用 | — | 跨平台代码必须在全新的、Windows 版不引用的项目里 |
| N12 | 顺手格式化 / 改 using / 加 nullable 注解 / 删 BOM | — | 会让 diff 噪音淹没真正的语义改动, 使 review 失效 |

## 跨平台共享代码的正确姿势: 共享源码, 不拆程序集

`VPet_Simulator.Core.Base/` 是共享源码的存放地. 关键点是
**`VPet-Simulator.Core` 不引用 `Base.dll`, 而是 glob 编译同一批源码文件**:

```xml
<!-- VPet-Simulator.Core.csproj -->
<ItemGroup>
  <!-- 只 glob 源码子目录, 绝不能写 **\*.cs 全量 —— 否则会把 Base 的 obj/ 下
       生成的 AssemblyInfo.cs 也编进来, 产生重复特性错误 -->
  <Compile Include="..\VPet_Simulator.Core.Base\Graph\**\*.cs" LinkBase="Shared\Graph" />
  <Compile Include="..\VPet_Simulator.Core.Base\Handle\**\*.cs" LinkBase="Shared\Handle" />
  ...
</ItemGroup>
```

这样:

- `VPet-Simulator.Core.dll` 里这些类型的 ABI **逐字节不变**
- Windows 部署目录里**不增加新 DLL**, 也就不用动 `CoreMOD.cs` 的 `LoadedDLL` 白名单
- 跨平台侧 (`VPet_Simulator.Core.MutiPlatform`) 正常 `ProjectReference` 到 `Base.dll`

唯一的代价是 `VPet_Simulator.Core.GraphInfo` 在 `Core.dll` 和 `Base.dll` 里是两个不同的
CLR 类型. 因为代码型 MOD 不需要跨平台, 两侧永不同时加载、永不交换对象, 这个代价为零.

共享源码必须能在两个项目的编译设置下同时通过: `Base` 关掉 `ImplicitUsings`
(隐式 using 会让共享文件在两边行为不一致).

### partial 拆分的三条规矩

把一个类拆成"共享半 + Windows 半"时, 下面三条踩了就是破坏:

1. **基类子句只写在 Windows 半.** `Item : NotifyPropertyChangedBase` 里那个基类是
   Panuon 的类型, 共享半没有也不该有它. 两个 partial 里都写基类会编译失败, 写在
   共享半则等于给跨平台那份也套上 WPF 依赖.

2. **嵌套类型必须保持嵌套.** `ScheduleTask.Package` 是 MOD 能直接写出来的全名,
   把它挪成顶层 `Package` 等于改名字, 已编译的 MOD 当场 `TypeLoadException`.
   正确做法是共享半也写成 `partial class ScheduleTask { partial class Package }`.

3. **`[Line]` 特性随成员一起搬, 一个字都不改.** 它决定存档里的子项名. 迁移期间
   真踩过一次: 统一契约里的 `UnifiedItem` 用了 `[Line(ignoreCase: true)]`, 写出来
   是 `Name`/`ItemType`, 而 Windows 版 `Item` 写死的是小写 `name`/`itemtype` ——
   存档拷过去认不出物品类型, 而且不报错, 只是东西没了.

拆不动的就别硬拆. `ScheduleTask` 本体通篇拿着 `IMainWindow`, `PackageFull` 有个
`WorkType` 属性而 `GraphHelper.Work` 在两个 Core 里是两个类型 —— 这些留在 Windows
半, 等跨平台界面真要用时连宿主抽象一起处理, 现在硬拆只会拆出一个没人用的壳.

## 跨平台侧的线程约定

Avalonia 的控件在**构造时**就记下了创建它的那个 Dispatcher, 之后挂进可视树、参与
排版时会校验. 也就是说"在后台线程 new 一个控件, 再交给 UI 线程用"是不行的 ——
报错点在下一次排版, 堆栈里全是 Avalonia 内部帧, 跟真正出问题的那行 `new` 隔着
十万八千里, 极难定位.

这个坑在迁移期间真炸过一次: 消息栏正文下面那行小字(数值增减的 `+++`)原先是在
`PetMain.Say` 里 new 好再传给 `MessageBar.Show` 的, 而 `Say` 整个跑在 `Task.Run`
里, 结果是"桌宠一说带数值的话就整个进程退出".

所以跨平台侧的约定是:

- 控件一律在 UI 线程创建. 需要从后台线程构造界面元素时, 传**数据**(字符串/路径)
  进去, 让 UI 线程那一侧去 new
- 共享的、懒加载的控件(例如 `FoodAnimation.FoodGrid`)不要写成静态字段初始化器 ——
  静态构造会在第一次碰到该类型时触发, 而那往往是后台的动画扫描线程. 写成惰性属性
  并加 `Dispatcher.UIThread.VerifyAccess()`, 把约定变成当场报错
- 计时器回调(`System.Timers.Timer`)全部跑在线程池上, 与 Windows 版一致. 回调里凡是
  碰界面的地方都要走 `RunOnUi`

注意 Windows 版没有这个问题: WPF 的 `Dispatcher.Invoke` 只关心当前线程是不是 UI
线程, 不关心对象是谁创建的. 照抄 WPF 的写法在这里会踩雷.

## 统一 MOD 契约

两套 MOD 体系是**并列**关系, 不是替代关系:

| | 写法 | 能跑在 | 保证 |
|---|---|---|---|
| 旧 | 继承 `MainPlugin` | 只有 Windows | 上面三条铁律, 老 dll 不重编译也能跑 |
| 新 | 继承 `UnifiedPlugin` | Windows + 跨平台 | 同一个 dll 两边都加载 |

`VPet-Simulator.Unified.Interface` 是这套新契约, 只引用 BCL 和 `LinePutScript`,
不含任何 WPF / Avalonia 类型 —— 照着它写出来的 MOD 编一次, 两个宿主都认.

### 旧 MOD 的兼容范围

保证的是**公开接口**和**原生命周期**:

- `VPet-Simulator.Core` 与 `VPet-Simulator.Windows.Interface` 的公开 API 表面
- `MainPlugin` 的六个虚方法被调用的时机和顺序
- `Item.Creators` / `Item.UseAction` / `IMainWindow` 上那些公开成员的语义

不保证的是宿主内部实现: 用反射去读 `MainWindow` 的私有字段、去改 WPF 可视树内部
结构的 MOD, 不在兼容范围内. 这类 MOD 本来就在跟着每个版本坏, 为它们冻结内部实现
会把整个迁移卡死.

### 两个宿主的接入方式

Windows 侧: `UnifiedPluginHost` 实现 `IPetHost`, 每个插件实例、每个窗口一个.
它和 `MainPlugin` 那条老路**并列**存在 —— `CoreMOD` 的加载循环里, 旧分支一字未动,
新分支是 `else if` 加在后面的; 六个生命周期调用点也都是在旧循环之后另起一个循环.
这样做的代价是多写几行, 换来的是"旧路径的行为不可能被新代码改变".

加载判据两边统一为 `exportedType.BaseType == typeof(UnifiedPlugin)` —— **只认直接
派生一层**. 中间插任何基类都会让这个 MOD 被静默跳过. 这一条与旧的 `MainPlugin`
判据完全一致, 是有意保持的对称.

### 契约的两条设计约束

**实时投影, 不是快照.** `IFoodInfo` / `IItemInfo` 这些接口包的是宿主里的真对象,
不是拿 `ILine` 抄出来的副本. 宿主在 MOD 加载之后还会改这些对象(价格钳制、收藏
状态、吃腻度), 抄一份出来的话 MOD 读到的是旧值, 改的也传不回去. 包装器按对象缓存
(`ConditionalWeakTable`), 同一个食物每次拿到的都是同一个包装器, 事件参数才能比较
相等.

**存档相关的一切都要每次重新取.** `GameSavesData` 在读档时会被整个替换掉, 构造期
缓存住的引用会指向老对象. 契约里 `IPetSave` / `IPetStatistics` 的实现都是持有一个
`Func<>` 每次现取.

### 顺便修掉的一个时序坑

Windows 版 `Item.Creators` 的文档说在 `LoadPlugin` 里注册, 而存档反序列化实际跑在
`LoadPlugin` **之前** —— 那时还没有创建器, MOD 的自定义物品会被静默降级成普通
`Item`: 数据不丢, 但类型不对, 使用处理器也就找不到它了.

统一契约不复制这个行为. 存档里认不出主人的物品行先挂起, 谁注册了对应类型就当场把
它们补出来, 所以在构造函数里注册(推荐)和在 `LoadPlugin` 里注册都能拿到东西. 旧的
`MainPlugin` 那条路保持原样, 免得改变现有 MOD 的行为.

## 怎么验证没有破坏

本仓库没有测试工程, 也不打算加. 验证是靠仓库外的一次性工具做的, 迁移期间用到了
五种手段, 记录在这里以便需要时重建:

**1. 公开 API 表面比对**

用 `System.Reflection.MetadataLoadContext` 只读载入基线 dll 和当前 dll, 把所有
public / protected 成员导出成归一化文本再做差集. 关键设计:

- 字段和属性用不同前缀记录, 所以"公开字段改成属性"会表现为一条 `FIELD` 消失
- 基类和接口分行记录, 所以"给类型追加接口"是安全的新增而不会误报
- 解析依赖时必须让 `Microsoft.WindowsDesktop.App` 排在 `Microsoft.NETCore.App`
  前面: 后者下面的 `WindowsBase.dll` 只是 16KB 的外观程序集, 不含
  `System.Windows.Point` 的定义, 用错了会让 `TouchArea` / `Main` / `GraphCore`
  这些类型静默解析失败, 它们内部的破坏就查不出来了
- 任何类型解析失败都必须让整个比对失败, 不能只记一行错误了事 —— 否则两侧
  恰好失败在同一个类型上时, 破坏会被完全掩盖

**2. 冻结的冒烟 MOD**

一个对着基线 dll 编译、之后**永不重新编译**的 MOD dll, 精确模拟社区里那些作者
已失联、无法重编译的插件. 把它 `Assembly.LoadFrom` 到当前构建上, 再用
`RuntimeHelpers.PrepareMethod` 强制 JIT 每个探针方法 —— JIT 会解析方法体里所有
字段/方法/类型令牌, 破坏会立刻以 `MissingFieldException` /
`MissingMethodException` / `TypeLoadException` 抛出. 这样连 `WorkTimer` 这种
"必须先有 Main 实例才能构造"的类型也能覆盖到, 不需要真的把游戏跑起来.

这是唯一能真正验收"没有破坏 MOD"的手段, 比任何静态分析都可靠.

**3. 动画决策黄金文件**

把 `GraphCore.FindGraph` / `FindGraphs` 在各种"库里有哪些状态的动画"场景下的
决策结果, 以及 `GraphInfo` 从路径推断类型/状态/动作的结果, 固化成文本再比对.
随机源要用固定种子, 否则"随机取一个"那一步不可复现.

它保护的正是 N1 那四处不对称: 重构时"顺手统一"不会报错、不会崩溃, 只会让某些
MOD 的动画悄悄不播.

**4. 后端黄金测试**

"一份后端两边用"的抽取(存档哈希、存档命名与轮换、MOD 元数据、资源索引)必须是纯粹的
搬家, 不能顺手改行为. 做法是同时加载 `VPet-Simulator.Windows.Interface` 和
`VPet-Simulator.Unified.Services`, 喂同一份输入让两边各算一遍, 逐字节比对.

这里抓到过两个真问题: 备份文件名原本用 `string.GetHashCode()`, 而 .NET Core 的字符串
哈希每个进程都不一样, 每次启动都会堆出新的备份文件 —— 改成用 `LPS_D.GetHashCode()`
(它是确定性的); 以及 `info.lps` 里 `authorid` / `itemid` 是**行**不是**子项**, 用
`FindSub` 读出来恒为 0.

**5. 统一契约门禁**

一个真的统一 MOD(`VPet.Plugin.UnifiedDemo`)加上一个最小宿主, 把六个生命周期跑完整,
核对可观察到的效果: 物品创建器、使用处理器、菜单按钮、存档与设置读写、事件的挂与摘、
调用顺序.

它同时是可移植性的硬检查 —— 用 `PEReader` 读演示 MOD 的引用表, 除了契约、
`LinePutScript` 和 BCL 之外一个都不许有. 这道门禁本身刻意跑在 `net8.0` 而不是
`net8.0-windows` 上: 它跑得起来, 就说明契约和照着写的 MOD 都不依赖 Windows.

**门禁抓到过的真问题**

留一份清单在这儿, 是因为它们都有同一个特点: **不报错**。

| 问题 | 表现 |
|---|---|
| 统一契约的 `UnifiedItem` 写出 `ItemType` 而 Windows 写 `itemtype` | 存档拷到另一平台, 物品认不出类型, 东西直接没了 |
| 联机包把三个字段包成一行的子项, 而 Windows 是三个顶层行 | 能连上, 但对面收到的全是默认值 |
| Steam 云存档用十进制解析十六进制文件名 | "删最旧的"实际删任意一个 |
| 跨平台没钉 InvariantCulture | 德语环境下 `12.5` 写成 `12,5`, 存档哈希当场失效 |
| 备份文件名用 `string.GetHashCode()` | .NET Core 的字符串哈希每进程不同, 每次启动堆一份新备份 |
| `info.lps` 里 `authorid` 是行不是子项 | `FindSub` 读出来恒为 0 |

共同的教训: **自洽的往返测试证明不了互通**。联机那一条原来是有门禁的, 只验了
跨平台↔跨平台的往返 —— 那当然自洽。改成拿 Windows 版的真类型去拆跨平台造的包
才抓出来。凡是声称"两边一致"的地方, 门禁必须真的把两边都拉进来比。

**渲染探针**

`--probe <名字> [输出.png]` 把某个界面渲染成图然后退出, 不用把桌宠启动起来。
没有 Linux/macOS 机器可以实机验证, 这是唯一能看到"界面画出来是什么样"的办法。

它画的必须是**真的**控件(例如商店的格子走 `Windows.ShopCell`), 照着样子另写一份
的话探针就只是在检查探针自己。迁移期间它当场抓到过: 脱离可视树的控件模板不展开
(画出来一片空白)、输入框深色底浅色字、TextBlock 和 CheckBox 前景色太浅、开关旁边
的 On/Off 是英文、卡片不等高导致一排按钮参差不齐。
