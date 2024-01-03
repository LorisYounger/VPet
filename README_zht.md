# VPet-Simulator

简体中文(./README.md) | 繁體中文 | [English](./README_en.md) | [日本語](./README_ja.md)

《虛擬桌寵模擬器》一個開源的桌寵軟體，可以內建至任何WPF應用程式中。

![主圖](README.assets/%E4%B8%BB%E5%9B%BE.png)

在[Steam](https://store.steampowered.com/app/1920960/VPet)上取得虛擬桌寵模擬器（免費），或透過[Nuget](https://www.nuget.org/packages/VPet-Simulator.Core)內建至應用程式中。

## 虛擬桌寵模擬器 - 詳細介紹

虛擬桌寵模擬器是一款桌寵軟體，支援各種互動，例如投餵等。開源、並支援工作坊。

既然都是免費的，那為何不試試呢？

本遊戲從[虛擬主播模擬器](https://store.steampowered.com/app/1352140/_/)之桌寵功能獨立出來，如果喜歡，歡迎將[虛擬主播模擬器](https://store.steampowered.com/app/1352140/_/)加入至您的願望清單中。

### 超多的互動及動畫

多達32（種類）×4（狀態）×3（類型）種動畫。<br/>
*註：部分種類並未有生病或循環等內容，實際上會略低於此數值。*

#### 動畫範例：

##### 摸頭

![ss0](README.assets/ss0.gif)

##### 提起

![ss4](README.assets/ss4.gif)![ss4](README.assets/ss8.gif)

##### 爬牆

![ss7](README.assets/ss7.gif)

### 免費

本遊戲完全免費！既然都不用錢，那就試試看吧！<br/>
本遊戲主要目的為宣傳[虛擬主播模擬器](https://store.steampowered.com/app/1352140/_/)中的Q版人物主角。

### 開源

本遊戲於Github上開源，歡迎提出自己的想法、創意，或直接參與開發！<br/>
您還可以修改原始碼來製作自己專屬的桌寵！（雖然說大部分內容支援工作坊，毋須修改程式碼）<br/>
專案位置：https://github.com/LorisYounger/VPet

### 支援工作坊

本遊戲支援工作坊，您可以自行製作桌寵角色、動畫或互動內容，並上傳至工作坊中分享給更多人使用。

模組製作器：https://github.com/LorisYounger/VPet.ModMaker

工作坊支援加入／修改下列內容

* 桌寵動畫
* 物品／食品／飲料等
* 自訂桌寵工作內容
* 對話內容
* 主題
* 代碼插件：編撰程式碼來加入新內容
  * 加入新的動畫邏輯或顯示效果（例如：l2d/spine等）
  * 加入新功能（鬧鐘、記事本等）
  * 幾乎無所不能，範例請見[VPet.Plugin.Demo](https://github.com/LorisYounger/VPet.Plugin.Demo)

###聯絡我們

若有建議或意見，可以在Steam商店、Steam社群中評論，在Github發Issue，使用虛擬桌寵模擬器QQ群（907101442）或電子郵件[mailto:service@exlb.net](mailto:service@exlb.net)聯絡作者。

## 程式架構

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
3. 点击启动, 如果一切正常则会报错 `缺少模组Core,无法启动桌宠`
4. 以管理员身份运行 `mklink.bat`, 这会让mod文件链接到生成位置
5. 再次点击启动即可正常运行
