# 项目/动画/架构分析

用于辅助制作和开发动画或代码插件MOD

### 核心组件结构
```
VPet-Simulator.Core (核心库)
├── Handle (接口与控件)
│   ├── IController (窗体控制器)
│   ├── Function (通用功能)
│   ├── GameCore (游戏核心)
│   ├── GameSave (存档功能)
│   ├── IFood (物品和食物接口)
│   └── PetLoader (动画加载器)
└── Graph (图形渲染)
    ├── PNGAnimation (PNG动画系统)
    └── GraphInfo (动画信息管理)

VPet-Simulator.Windows (桌面应用)
├── Function (功能性代码)
│   ├── CoreMOD (MOD管理)
│   └── MWController (窗体控制器)
├── WinDesign (窗口和UI设计)
└── MainWindows (主窗体)

VPet-Simulator.Tool (MOD制作工具)
```

## 系统介绍

### 动画系统

1. **动画格式设计**
   - 支持PNG序列帧动画
   - 实现ABC三段式动画系统 (Start-Loop-End)
   - 动画类型分类 (必需17种基础类型)

2. **动画加载优化**
   - 大图合成技术 (将多帧合并为单张图片)
   - 缓存机制
   - 内存管理和延迟加载

### 交互系统
1. **基础交互**
   - 鼠标悬停检测
   - 点击事件处理
   - 拖拽提起效果

2. **状态管理**
   - 4种基础状态 (Normal, Happy, PoorCondition, Ill)
   - 状态切换动画
   - 属性值系统 (饥饿、口渴等)

### 扩展系统
1. **MOD支持**
   - 动画文件热加载
   - 配置文件解析
   - 插件接口设计

2. **高级功能**
   - Steam Workshop集成 (可选)
   - 多语言支持
   - 主题系统

## 动画图片格式判断流程

### 1. 核心判断原理
动画格式通过 **文件路径解析** 和 **配置文件信息** 两种方式进行判断：

### 2. 路径解析流程

#### 第一步：路径标准化
```csharp
// 获取相对于startup路径的文件路径
string pn = Sub.Split(path.FullName.ToLower(), info[(gstr)"startuppath"].ToLower()).Last();
// 将路径分隔符转换为下划线，并分割成数组
var path_name = pn.Replace('\\', '_').Split('_').ToList();
path_name.RemoveAll(string.IsNullOrWhiteSpace); // 移除空白元素
```

#### 第二步：状态类型判断 (ModeType)
优先级：**配置文件 > 路径关键词**

```csharp
// 1. 尝试从配置文件读取
if (!Enum.TryParse(info[(gstr)"mode"], true, out IGameSave.ModeType modetype))
{
    // 2. 从路径中查找关键词
    if (path_name.Remove("happy"))          modetype = IGameSave.ModeType.Happy;
    else if (path_name.Remove("nomal"))     modetype = IGameSave.ModeType.Nomal;
    else if (path_name.Remove("poorcondition")) modetype = IGameSave.ModeType.PoorCondition;
    else if (path_name.Remove("ill"))       modetype = IGameSave.ModeType.Ill;
    else modetype = IGameSave.ModeType.Nomal; // 默认值
}
```

#### 第三步：动画类型判断 (GraphType)
优先级：**配置文件 > 路径关键词匹配**

```csharp
// 1. 尝试从配置文件读取
if (!Enum.TryParse(info[(gstr)"graph"], true, out GraphType graphtype))
{
    // 2. 遍历预定义的关键词数组进行匹配
    for (int i = 0; i < GraphTypeValue.Length; i++)
    {
        if (path_name.Contains(GraphTypeValue[i][0]))
        {
            // 进行完整的多词匹配验证
            // 匹配成功后从路径中移除这些关键词
        }
    }
}
```

#### 第四步：动作类型判断 (AnimatType)
优先级：**配置文件 > 路径关键词**

```csharp
if (!Enum.TryParse(info[(gstr)"animat"], true, out AnimatType animatType))
{
    if (path_name.Remove("a") || path_name.Remove("start"))      animatType = AnimatType.A_Start;
    else if (path_name.Remove("b") || path_name.Remove("loop"))  animatType = AnimatType.B_Loop;
    else if (path_name.Remove("c") || path_name.Remove("end"))   animatType = AnimatType.C_End;
    else if (path_name.Remove("single"))                        animatType = AnimatType.Single;
    else animatType = AnimatType.Single; // 默认值
}
```

#### 第五步：动画名称确定
优先级：**配置文件 > 路径剩余部分 > 动画类型名**

```csharp
Name = info.Info; // 首先尝试从配置文件获取
if (string.IsNullOrWhiteSpace(Name))
{
    // 移除数字和特殊标记
    while (path_name.Count > 0 && (double.TryParse(path_name.Last(), out _) || path_name.Last().StartsWith("~")))
    {
        path_name.RemoveAt(path_name.Count - 1);
    }
    // 使用路径最后一个有效部分作为名称
    if (path_name.Count > 0) Name = path_name.Last();
}
// 如果仍然为空，使用动画类型名作为默认名称
if (string.IsNullOrWhiteSpace(Name)) Name = graphtype.ToString().ToLower();
```

### 3. 路径命名规范示例

路径命名规范：

`{startup_path}_{状态}_{动画类型}_{动画名称}_{动作}_{帧时间}.png`
其中 `_` 和 `/` 可以互相替换
其中 `{状态}` `{动画类型}` `{动画名称}`顺序不重要

例如：

```
- happy/touch_head/pet_a_125.png     (开心状态-摸头动画-pet名称-开始动作-125ms)
- nomal/default/breath_b_200.png     (普通状态-默认动画-呼吸名称-循环动作-200ms)
- ill/sleep/tired_c_150.png          (生病状态-睡觉动画-疲惫名称-结束动作-150ms)
```

### 4. 关键特点

1. **层级优先级**：配置文件 > 路径关键词 > 默认值
2. **关键词移除**：匹配到的关键词会从路径中移除，避免重复解析
3. **智能过滤**：自动过滤数字和特殊标记（以~开头）
4. **容错机制**：每个属性都有默认值，确保解析不会失败
5. **大小写不敏感**：所有路径都转换为小写进行匹配

## 动作类型 (AnimatType)

### 1. 四种动作类型
```csharp
public enum AnimatType
{
    Single,    // 动画只有一个动作
    A_Start,   // 开始动作
    B_Loop,    // 循环动作
    C_End,     // 结束动作
}
```

### 2. 动作类型详细说明

| 动作类型    | 英文名称 | 中文含义 | 用途说明                         |
| ----------- | -------- | -------- | -------------------------------- |
| **Single**  | Single   | 单一动作 | 完整的独立动画，不需要分段       |
| **A_Start** | A_Start  | 开始动作 | 动画序列的开始部分               |
| **B_Loop**  | B_Loop   | 循环动作 | 动画序列的循环部分（可重复播放） |
| **C_End**   | C_End    | 结束动作 | 动画序列的结束部分               |

## 动画ABC系统的意思

### 1. 设计理念
动画ABC系统是一个 **三段式动画架构**，将复杂动画分解为：
- **A段（Start）**：入场/准备阶段
- **B段（Loop）**：主体/持续阶段  
- **C段（End）**：退场/结束阶段

### 2. 路径识别规则
```csharp
// 从文件路径中识别动作类型的优先级
if (path_name.Remove("a") || path_name.Remove("start"))      // A_Start
else if (path_name.Remove("b") || path_name.Remove("loop"))  // B_Loop
else if (path_name.Remove("c") || path_name.Remove("end"))   // C_End
else if (path_name.Remove("single"))                        // Single
else // 默认为Single
```

### 3. 支持ABC系统的动画类型
根据注释，以下动画类型支持完整的ABC三段式：

**必须有的动画（标*）：**

- `Raised_Static` - 被提起静态 (开始&循环&结束)
- `Sleep` - 睡觉 (开始&循环&结束)
- `Say` - 说话 (开始&循环&结束)
- `Work` - 工作 (开始&循环&结束)

**可选的动画：**
- `Touch_Head` - 摸头 (开始&循环&结束)
- `Touch_Body` - 摸身体 (开始&循环&结束)
- `Idel` - 空闲 (开始&循环&结束)
- `StateONE` - 待机模式1 (开始&循环&结束)
- `StateTWO` - 待机模式2 (开始&循环&结束)

### 4. 实际应用示例

以摸头动画为例：
```
touch_head_a_100.png  // 摸头开始动画，100ms一帧
touch_head_b_150.png  // 摸头循环动画，150ms一帧  
touch_head_c_120.png  // 摸头结束动画，120ms一帧
```

播放流程：
1. **A段播放一次** → 手接近头部
2. **B段循环播放** → 持续摸头动作
3. **C段播放一次** → 手离开头部

### 5. 文件命名规范

支持的关键词识别：
- **A段**：`a` 或 `start`
- **B段**：`b` 或 `loop` 
- **C段**：`c` 或 `end`
- **单段**：`single`

这种设计使得动画更加自然流畅，特别适合需要持续交互的场景，用户可以控制B段的循环次数，而A、C段确保动画的完整性。

## 动画类型 (GraphType) 完整总结

### 1. 基础分类

| 类型       | 英文名称 | 中文含义 | 是否必须 | ABC支持/Single支持 | 说明                                        |
| ---------- | -------- | -------- | -------- | ------------------ | ------------------------------------------- |
| **Common** | Common   | 通用动画 | 否       | -                  | 用于被其他动画调用或mod等用途，不被默认启用 |

### 2. 核心必须动画（标*）

| 类型               | 英文名称       | 中文含义   | ABC支持/Single支持 | 用途说明                     |
| ------------------ | -------------- | ---------- | ------------------ | ---------------------------- |
| **Raised_Dynamic** | Raised_Dynamic | 被提起动态 | ❌✅                 | 被用户拖拽时的动态效果       |
| **Raised_Static**  | Raised_Static  | 被提起静态 | ✅❌                 | 被用户提起时的静态动画序列   |
| **Default**        | Default        | 呼吸/默认  | ❌✅                 | 桌宠的基础呼吸动画，idle状态 |
| **Sleep**          | Sleep          | 睡觉       | ✅❌                 | 桌宠睡眠时的动画序列         |
| **Say**            | Say            | 说话       | ✅❌                 | 桌宠说话时的动画序列         |
| **StartUP**        | StartUP        | 开机       | ❌✅                 | 程序启动时的动画             |
| **Work**           | Work           | 工作       | ✅❌                 | 桌宠工作时的动画序列         |

### 3. 交互类动画

| 类型           | 英文名称   | 中文含义 | ABC支持/Single支持 | 用途说明               |
| -------------- | ---------- | -------- | ------------------ | ---------------------- |
| **Touch_Head** | Touch_Head | 摸头     | ✅❌                 | 用户摸桌宠头部时的反应 |
| **Touch_Body** | Touch_Body | 摸身体   | ✅❌                 | 用户摸桌宠身体时的反应 |

### 4. 行为类动画

| 类型     | 英文名称 | 中文含义 | ABC支持/Single支持 | 用途说明                         |
| -------- | -------- | -------- | ------------------ | -------------------------------- |
| **Move** | Move     | 移动     | ✅❌                 | 所有会动的东西都归类为MOVE       |
| **Idel** | Idel     | 空闲     | ✅✅                 | 包括下蹲、无聊等通用空闲随机动画 |

### 5. 状态类动画

状态类动画等同于**Idel**, 不过支持 从状态1切换到状态2的功能

| 类型         | 英文名称 | 中文含义  | ABC支持/Single支持 | 用途说明           |
| ------------ | -------- | --------- | ------------------ | ------------------ |
| **StateONE** | StateONE | 待机模式1 | ✅❌                 | 第一种待机状态动画 |
| **StateTWO** | StateTWO | 待机模式2 | ✅❌                 | 第二种待机状态动画 |

### 6. 系统类动画

| 类型         | 英文名称 | 中文含义 | ABC支持/Single支持 | 用途说明         |
| ------------ | -------- | -------- | ------------------ | ---------------- |
| **Shutdown** | Shutdown | 关机     | ❌✅                 | 程序关闭时的动画 |

### 7. 状态切换类动画

| 类型               | 英文名称       | 中文含义     | ABC支持/Single支持 | 用途说明             |
| ------------------ | -------------- | ------------ | ------------------ | -------------------- |
| **Switch_Up**      | Switch_Up      | 向上切换状态 | ❌✅                 | 状态提升时的过渡动画 |
| **Switch_Down**    | Switch_Down    | 向下切换状态 | ❌✅                 | 状态下降时的过渡动画 |
| **Switch_Thirsty** | Switch_Thirsty | 口渴         | ❌✅                 | 切换到口渴状态的动画 |
| **Switch_Hunger**  | Switch_Hunger  | 饥饿         | ❌✅                 | 切换到饥饿状态的动画 |

## 关键特点总结

### 1. **必须动画（7个）**
```
Raised_Dynamic, Raised_Static, Default, Sleep, Say, StartUP, Work
```
这些是桌宠系统运行的基础动画，必须提供。

### 2. **ABC三段式支持（8个）**
```
Raised_Static, Touch_Head, Touch_Body, Idel, Sleep, Say, StateONE, StateTWO, Work
```
这些动画支持完整的开始→循环→结束序列。

### 3. **动画优先级**
- **核心功能** > **交互反馈** > **状态切换** > **扩展功能**
- **必须动画** > **可选动画**

### 4. **设计理念**
- **模块化**：每种动画类型职责明确
- **可扩展**：通过Common类型支持自定义动画
- **用户体验**：丰富的交互反馈和状态表现
- **系统完整性**：涵盖了桌宠的完整生命周期
