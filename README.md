# VPet-Simulator

简体中文 | [English](./README_en.md)

虚拟桌宠模拟器 一个开源的桌宠软件, 可以内置到任何WPF应用程序

![主图](README.assets/%E4%B8%BB%E5%9B%BE.png)

获取虚拟桌宠模拟器 [OnSteam(免费)](https://store.steampowered.com/app/1920960/VPet) 或 通过[Nuget](https://www.nuget.org/packages/VPet-Simulator.Core)内置到你的WPF应用程序

## 虚拟桌宠模拟器 详细介绍

虚拟桌宠模拟器是一款桌宠软件,支持各种互动投喂等. 开源免费并且支持创意工坊.

反正免费为啥不试试呢(

该游戏为 [虚拟主播模拟器](https://store.steampowered.com/app/1352140/_/) 内置桌宠(教程)程序独立而来, 如果喜欢的话欢迎添加 [虚拟主播模拟器](https://store.steampowered.com/app/1352140/_/) 至愿望单

### 超多的互动和动画

多达 32(种) * 4(状态) * 3(类型) 种动画, *注:部分种类没有生病状态或循环等内容,实际动画数量会偏少*

#### 一些动画例子:

##### 摸头

![ss0](README.assets/ss0.gif)

##### 提起

![ss4](README.assets/ss4.gif)![ss4](README.assets/ss8.gif)

##### 爬墙

![ss7](README.assets/ss7.gif)

### 免费

该游戏完全免费! 反正不要钱,试试不要紧(<br/>
该游戏主要目的是宣传下 [虚拟主播模拟器](https://store.steampowered.com/app/1352140/_/), 游戏中Q版人物为虚拟主播模拟器的主人公.

### 开源

该游戏在github上开源, 欢迎提出自己的想法,创意或者参与开发!<br/>
您还可以修改代码来制作自己专属的桌宠!(虽然说大部分内容都支持创意工坊,不需要修改代码)<br/>
项目地址: https://github.com/LorisYounger/VPet

### 支持创意工坊

该游戏支持创意工坊,您可以制作别的人物桌宠动画或者互动,并上传至创意工坊分享给更多人使用.

创意工坊支持添加/修改以下内容

* 桌宠动画
* 物品/食物/饮料等
* 自定义桌宠工作
* 说话文本
* 主题
* 代码插件 - 通过编写代码给桌宠添加内容
  * 添加新的动画逻辑/显示方案 (eg: l2d/spine 等)
  * 添加新功能 (闹钟/记事板等等)
  * 几乎无所不能, 示例例子参见 [VPet.Plugin.Demo](https://github.com/LorisYounger/VPet.Plugin.Demo)


### 反馈&建议&联系我们

如果有建议或者意见,可以在Steam商店评论/社区,Github Issue,虚拟主播模拟器贴吧,虚拟桌宠模拟器MODDer群(907101442)或者邮件联系我 [mailto:service@exlb.net](mailto:service@exlb.net)

## 软件结构

* **VPet-Simulator.Windows: 适用于桌面端的虚拟桌宠模拟器**
  * *Function 功能性代码存放位置*
    * CoreMOD Mod管理类
    * MWController 窗体控制器
  
  * *WinDesign 窗口和UI设计
    * winBetterBuy 更好买窗口
    * winCGPTSetting ChatGPT 设置
    * winSetting 软件设置/MOD 窗口
    * winConsole 开发控制台
    * winGameSetting 游戏设置
    * winReport 反馈中心
  
  * MainWindows 主窗体,存放和展示Core
  * PetHelper 快速切换小标
* **VPet-Simulator.Tool: 方便制作MOD的工具(eg:图片帧生成)**
* **VPet-Simulator.Core: 软件核心 方便内置到任何WPF应用程序(例如:VUP-Simulator)**
  * Handle 接口与控件
    * IController 窗体控制器 (调用相关功能和设置,例如移动到侧边等)
    * Function 通用功能
    * GameCore 游戏核心,包含各种数据等内容
    * GameSave 游戏存档
    * IFood 食物/物品接口
    * PetLoader 宠物图形加载器
  * Graph 图形渲染
    * IGraph 动画基本接口
    * GraphCore 动画显示核心
    * GraphHelper 动画帮助类
    * GraphInfo 动画信息
    * FoodAnimation 食物动画 支持显示前中后3层夹心动画 不一定只用于食物,只是叫这个名字
    * PNGAnimation 桌宠动态动画组件
    * Picture 桌宠静态动画组件
  * Display 显示
    * basestyle/Theme 基本风格主题
    * Main.xaml 核心显示部件
      * MainDisplay 核心显示方法
      * MainLogic 核心显示逻辑
    * ToolBar 点击人物时候的工具栏
    * MessageBar 人物说话时候的说话栏
    * WorkTimer 工作时钟

## 参与开发

欢迎参与虚拟桌宠模拟器的开发! 为保证代码可维护度和游戏性,如果想要开发新的功能,请先[邮件联系](mailto:zoujin.dev@exlb.org)或发[Issues](https://github.com/LorisYounger/VPet/issues)我想要添加的功能/玩法, 以确保该功能/玩法适用于虚拟桌宠模拟器. 以免未来提交时因不合适被拒(而造成代码浪费)<br/>
如果是修复错误或者BUG,无需联系我,修好后直接PR即可

当想法通过后,您可以通过 [fork](https://github.com/LorisYounger/VPet/fork) 功能拷贝代码至自己的github以方便编写自己的代码, 编写完毕后通过[pull requests](https://github.com/LorisYounger/VPet/compare) 提交<br/>
如果您想法没有被通过,也可以另起炉灶,写个不同版本功能的桌宠软件. 但需遵守 [Apache License 2.0](https://github.com/LorisYounger/VPet/blob/main/LICENSE) 与 [动画版权声明与授权](https://github.com/LorisYounger/VPet#%E5%8A%A8%E7%94%BB%E7%89%88%E6%9D%83%E5%A3%B0%E6%98%8E%E4%B8%8E%E6%8E%88%E6%9D%83)
注: 一般来讲, 添加新功能都可以通过编写代码插件MOD实现, 详情请参见 [VPet.Plugin.Demo](https://github.com/LorisYounger/VPet.Plugin.Demo)

我可能会对您的提交的代码进行修改,删减等以确保该功能/玩法适用于虚拟桌宠模拟器.


感谢以下参与的开发和翻译人员

<a href="https://github.com/LorisYounger/VPet/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=LorisYounger/VPet" />
</a>

和提供社区翻译和更多内容的创意工坊人员

## 动画版权声明与授权

在github中 [桌宠动画文件](https://github.com/LorisYounger/VPet/tree/main/VPet-Simulator.Windows/mod/0000_core/pet/vup) 动画版权归 [虚拟主播模拟器制作组](https://www.exlb.net/VUP-Simulator)所有, 当使用本类库时,您可能需要自行准备动画文件,或遵循以下协议

> **注 **
> 本动画声明仅限于桌宠自带的动画, 若有画师/开发者画自己的动画适配给桌宠,并不遵循用本声明

### 非商用用途授权

* 需要向用户告知动画文件来源并提供访问 [该页面](https://github.com/LorisYounger/VPet) 的链接
* 当您完成以上要求后,您可以免费使用动画文件

### 商用用途授权

* 第一次使用时需弹窗并醒目的向用户告知动画文件来源并提供访问 [该页面](https://github.com/LorisYounger/VPet) 的链接
* 在相应页面(用户可以快捷访问)向用户告知动画文件来源并提供访问 [该页面](https://github.com/LorisYounger/VPet) 的链接

* 禁止通过出售动画文件进行盈利
* 请[邮件联系](mailto:zoujin.dev@exlb.org)我
* 当您完成以上要求后,您可以免费使用动画文件

### 分发动画文件

* 需要告知以上所有授权信息
* 需要提供访问 [该页面](https://github.com/LorisYounger/VPet) 的链接
* 分发动画文件时禁止任何付费/收费行为

## 桌面端部署方法

1. 下载本项目, 通过VisualStudio打开 `VPet.sln` 文件
2. 在生成栏中, 选择 位数为 `x64` 和生成项目为 `Vpet-Simulator.Windows`
   ![image-20230208004330895](README.assets/image-20230208004330895.png)
3. 点击启动, 即可正常运行