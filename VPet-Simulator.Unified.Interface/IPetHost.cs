using LinePutScript;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Unified.Interface;

/// <summary>
/// 语音播放器
/// </summary>
/// 宿主自己不带音频后端: Avalonia 没有内置音频, 而引一个原生音频库是个不该由
/// 迁移工作单方面替项目做的决定. 所以定成契约, 由跨平台语音 MOD 来实现.
///
/// Remaining 是关键的一项: 消息栏的口型同步靠"还剩多少秒"决定气泡什么时候收起,
/// 只有一个"播完了"的事件是不够的.
public interface IVoicePlayer : IDisposable
{
    /// <summary>正在播吗</summary>
    bool IsPlaying { get; }
    /// <summary>还剩多久播完; 不知道时返回 Zero</summary>
    TimeSpan Remaining { get; }
    /// <summary>音量 0~1</summary>
    double Volume { get; set; }
    /// <summary>播放</summary>
    void Play(string path);
    /// <summary>停</summary>
    void Stop();
    /// <summary>播完了</summary>
    event Action? Completed;
}

/// <summary>
/// 物品注册表
/// </summary>
public interface IItemRegistry
{
    /// <summary>背包里现有的东西(实时视图)</summary>
    IReadOnlyList<IItemInfo> All { get; }

    /// <summary>往背包里加一件, 同名同类型的会自动合并数量</summary>
    void Add(UnifiedItem item);

    /// <summary>
    /// 注册一种自定义物品的创建方式
    /// </summary>
    /// <param name="itemType">物品类型名</param>
    /// <param name="creator">从存档行造出物品; 返回 null 表示这行不归你管</param>
    /// 建议在插件构造函数里注册 —— 那时存档还没读, 正好赶得上.
    /// 但**晚注册也不会丢东西**: 存档里认不出主人的物品行会被挂起, 等到有人注册了
    /// 对应的类型再补上. Windows 版原本有个坑, 文档说在 LoadPlugin 里注册, 而存档
    /// 反序列化其实跑在 LoadPlugin 之前, 那些物品会被降级成普通物品; 这里不复制
    /// 那个行为.
    void RegisterCreator(string itemType, Func<ILine, UnifiedItem?> creator);

    /// <summary>
    /// 注册"用掉某类物品时干什么"
    /// </summary>
    /// <returns>处理器返回 true 表示已处理, 后面的就不再试了</returns>
    /// 先注册的先执行, 与 Windows 版 Item.UseAction 的顺序语义一致.
    void RegisterUseAction(string itemType, Func<IItemInfo, bool> action);

    /// <summary>喂给桌宠(结算数值, 不扣钱)</summary>
    void Take(IFoodInfo food);
    /// <summary>通知宿主"用掉了某样东西", 供统计和成就用</summary>
    void TakeHandle(IFoodInfo food, int count, string from);

    /// <summary>有东西被喂给桌宠时触发</summary>
    event Action<IFoodInfo>? TakeItem;
    /// <summary>上次喂东西的时间</summary>
    DateTime LastTakeItemTime { get; }
}

/// <summary>
/// 桌宠本体能做的事
/// </summary>
/// 刻意不暴露任何控件: MOD 拿到控件就会在错误的线程上碰它. 需要界面的 MOD 通过
/// IPetHost.GetService 去取宿主专属的扩展接口, 那时它自己知道在跟哪个宿主打交道.
public interface IPetView
{
    /// <summary>当前在干什么</summary>
    PetWorkingState State { get; set; }
    /// <summary>当前的工作, 没有则 null</summary>
    IWorkInfo? NowWork { get; }
    /// <summary>上次和桌宠互动的时间</summary>
    DateTime LastInteractionTime { get; set; }

    /// <summary>按名字播动画</summary>
    void Display(string name, PetAnimatType animat, Action? endAction = null);
    /// <summary>按类型播动画</summary>
    void Display(PetGraphType type, PetAnimatType animat, Action? endAction = null);
    /// <summary>回到默认动画</summary>
    void DisplayToNomal();
    /// <summary>播一段带图片的动画(例如喂食)</summary>
    /// <param name="graphName">动画名</param>
    /// <param name="imagePath">要塞进去的图片路径</param>
    void DisplayFoodAnimation(string graphName, string imagePath);

    /// <summary>说话</summary>
    void Say(string text, string? graphName = null, bool force = false, string? desc = null);
    /// <summary>说话, 随机挑个表情</summary>
    void SayRnd(string text, bool force = false, string? desc = null);
    /// <summary>在桌宠身上飘一行小字</summary>
    void LabelDisplayShow(string text, int time = 2000);

    /// <summary>摸头时触发</summary>
    event Action? TouchHead;
    /// <summary>摸身体时触发</summary>
    event Action? TouchBody;
    /// <summary>说话时触发</summary>
    event Action<string>? OnSay;
    /// <summary>开始工作时触发</summary>
    event Action<IWorkInfo>? WorkStart;
    /// <summary>结束工作时触发 (工作, 收益, 用时分钟)</summary>
    event Action<IWorkInfo, double, double>? WorkEnd;
    /// <summary>每个逻辑周期触发一次; 在计时器线程上</summary>
    event Action? Tick;

    /// <summary>播放语音</summary>
    void PlayVoice(string path);
    /// <summary>语音音量</summary>
    double VoiceVolume { get; set; }
    /// <summary>正在放语音吗</summary>
    bool PlayingVoice { get; }
}

/// <summary>
/// 宿主门面: MOD 能碰到的一切都在这里
/// </summary>
/// Windows 版和跨平台版各自实现一份. 两边的实现都不暴露自己的界面类型, 所以按这份
/// 契约写的 MOD 编译一次两边都能跑.
///
/// == 线程约定 ==
///
///   插件构造函数    后台线程, 没有 UI 线程, 存档和动画都还没就绪 —— 只做注册
///   LoadPlugin      UI 线程, 存档已读好
///   LoadDIY         UI 线程, 可能被反复调用(每次重建自定菜单)
///   GameLoaded      UI 线程, 动画就绪、桌宠已经跑起来
///   Save            调用方线程(自动保存计时器或 UI)
///   EndGame         UI 线程
///   Tick / WorkEnd  计时器线程
///
/// 门面上所有方法都可以从任意线程调用, 内部会自己切到 UI 线程. 但**不要自己 new
/// 界面控件再交给宿主** —— Avalonia 的控件在构造时就绑定了创建它的线程.
public interface IPetHost
{
    // ---- 宿主 ----

    /// <summary>"Windows" 或 "MutiPlatform"</summary>
    string HostName { get; }
    /// <summary>宿主版本号</summary>
    int HostVersion { get; }
    /// <summary>当前是不是在 UI 线程上</summary>
    bool IsUIThread { get; }
    /// <summary>切到 UI 线程执行</summary>
    void RunOnUI(Action action);
    /// <summary>切到 UI 线程取值</summary>
    T RunOnUI<T>(Func<T> func);
    /// <summary>翻译</summary>
    string Translate(string text);
    /// <summary>翻译并格式化</summary>
    string Translate(string text, params object[] args);
    /// <summary>写一行日志</summary>
    void Log(string message);
    /// <summary>给玩家看一条提示</summary>
    void ShowMessage(string text, string title = "");
    /// <summary>弹个输入框</summary>
    void ShowInputBox(string title, string text, string defaultText, Action<string> end, bool allowMultiLine = false);

    /// <summary>
    /// 取宿主专属的扩展接口
    /// </summary>
    /// 逃生舱: 用它就意味着这个 MOD 只能在某一个宿主上跑了. Windows 侧能取到
    /// IMainWindow / Main, 跨平台侧能取到 PetMain / Window.
    T? GetService<T>() where T : class;

    // ---- 插件与 MOD ----

    /// <summary>本插件所属的 MOD</summary>
    IPluginInfo Info { get; }
    /// <summary>所有扫描到的 MOD</summary>
    IEnumerable<IPluginInfo> Mods { get; }
    /// <summary>已启用的 MOD</summary>
    IEnumerable<IPluginInfo> OnMods { get; }
    /// <summary>MOD 根目录</summary>
    IReadOnlyList<string> ModPaths { get; }
    /// <summary>可写的数据目录</summary>
    string DataDirectory { get; }
    /// <summary>本 MOD 专用的可写目录, 不存在会自动建</summary>
    string GetModStorage(string modName);
    /// <summary>多开前缀</summary>
    string PrefixSave { get; }

    // ---- 设置与存档 ----

    /// <summary>
    /// 读写一行自定义设置 (随 Setting.lps 保存)
    /// </summary>
    /// 这是 MOD 存设置的正规入口, 与 Windows 版 ISetting 的行索引器同源
    ILine Setting(string lineName);
    /// <summary>
    /// 读写存档里的自定义数据 (随存档保存)
    /// </summary>
    /// 行名请加自己的前缀, 免得和别的 MOD 撞车
    ILPS SaveData { get; }
    /// <summary>桌宠的数值</summary>
    IPetSave Save { get; }
    /// <summary>统计数据</summary>
    IPetStatistics Statistics { get; }
    /// <summary>防作弊检查是否还有效</summary>
    bool HashCheck { get; }
    /// <summary>关掉防作弊检查; 会改数值的 MOD 请在改之前调它</summary>
    void HashCheckOff();
    /// <summary>立刻存档</summary>
    void SaveGame();
    /// <summary>跨 MOD 共享的一袋子东西</summary>
    IDictionary<string, object> DynamicResources { get; }

    // ---- 数据表 ----

    /// <summary>所有食物(实时视图)</summary>
    IReadOnlyList<IFoodInfo> Foods { get; }
    /// <summary>所有照片(实时视图)</summary>
    IReadOnlyList<IPhotoInfo> Photos { get; }
    /// <summary>某一类说话文本</summary>
    IReadOnlyList<ITextInfo> Texts(PetTextKind kind);
    /// <summary>加一条说话文本</summary>
    void AddText(PetTextKind kind, ILine line);
    /// <summary>按当前时间和状态挑一条点击文本</summary>
    ITextInfo? GetClickText();
    /// <summary>找图片, 找不到时退回上级</summary>
    string? FindImagePath(string name, string? superior = null);
    /// <summary>找 MOD 提供的文件</summary>
    string? FindFilePath(string name);

    // ---- 物品与桌宠 ----

    /// <summary>物品注册表</summary>
    IItemRegistry Items { get; }
    /// <summary>桌宠本体</summary>
    IPetView Pet { get; }

    // ---- 界面挂点 ----

    /// <summary>往工具栏某个一级菜单下加一个按钮</summary>
    void AddMenuButton(PetMenuType menu, string displayName, Action click);
    /// <summary>往工具栏某个一级菜单下的分组里加一个按钮</summary>
    void AddMenuButton(PetMenuType menu, string groupName, string displayName, Action click);
    /// <summary>登记一个要在退出时释放的东西(窗口、计时器等)</summary>
    void RegisterClosable(IDisposable closable);
    /// <summary>提供语音播放器</summary>
    void RegisterVoicePlayer(IVoicePlayer player);

    // ---- 杂项 ----

    void SetZoomLevel(double level);
    void Close();
    void Restart();
    /// <summary>新的一天开始时触发</summary>
    event Action? NewDay;
}

/// <summary>
/// 统一 MOD 的插件主体
/// </summary>
/// 请直接继承这个类 —— 两个宿主的加载器都只认"直接派生一层", 中间插任何基类都会
/// 让这个 MOD 被静默跳过.
///
/// 与 Windows 版的 MainPlugin 是并列关系: 旧的 MainPlugin 仍然可用(只能在 Windows
/// 上跑), 按这份契约写的插件则两个平台都能加载.
public abstract class UnifiedPlugin
{
    /// <summary>
    /// 插件名称, 必须与 MOD 名称一致
    /// </summary>
    public abstract string PluginName { get; }

    /// <summary>
    /// 宿主
    /// </summary>
    public IPetHost Host { get; }

    /// <summary>
    /// 构造
    /// </summary>
    /// 此时在后台线程上, 没有 UI 线程, 存档和动画都还没就绪.
    /// 只做注册(物品创建器、使用处理器), 不要读游戏数据.
    protected UnifiedPlugin(IPetHost host)
    {
        Host = host;
    }

    /// <summary>
    /// 初始化, 存档已读好
    /// </summary>
    /// 例: 挂事件、加菜单按钮、创建自己的界面
    public virtual void LoadPlugin() { }

    /// <summary>
    /// 游戏加载完毕
    /// </summary>
    /// 例: 修改已经加载好的内容
    public virtual void GameLoaded() { }

    /// <summary>
    /// 重建自定菜单
    /// </summary>
    /// 会被反复调用, 每次调用前宿主都会清空自定菜单
    public virtual void LoadDIY() { }

    /// <summary>
    /// 保存
    /// </summary>
    /// 请写进 Host.SaveData 或 Host.Setting
    public virtual void Save() { }

    /// <summary>
    /// 打开本插件的设置界面
    /// </summary>
    /// 覆盖了这个方法, 宿主才会给这个 MOD 显示"设置"入口
    public virtual void Setting() { }

    /// <summary>
    /// 退出
    /// </summary>
    public virtual void EndGame() { }
}
