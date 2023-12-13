[简体中文](./CONTRIBUTING.md) | 繁體中文 | [English](./CONTRIBUTING_en.md)

## 參與開發

歡迎參與虛擬桌寵模擬器的開發！為了保證程式碼的可維護性及遊戲性，若想要開發新的功能，請先[電子郵件聯絡](mailto:zoujin.dev@exlb.org)或提交[Issue](https://github.com/LorisYounger/VPet/issues)，標題為想要新增的功能／玩法，以確保該功能／玩法適用於虛擬桌寵模擬器，以免在您完成開發後，因不適合而被拒絕（而浪費您的時間）。<br/>
如果是修正錯誤或BUG，則不需要先行聯絡，修好後直接提交即可。

當您提供的想法被贊同後，您可以使用[Fork](https://github.com/LorisYounger/VPet/fork)功能，將專案程式碼整個複製至個人的Github上，以便撰寫自己的程式碼。撰寫完畢後，使用[Pull Requests](https://github.com/LorisYounger/VPet/compare)提交。<br/>
若您的想法並未被同意，也可以另起爐灶，開發一個不同版本及功能的桌寵軟體。須遵守[Apache License 2.0](https://github.com/LorisYounger/VPet/blob/main/LICENSE)及[動畫版權聲明與授權](https://github.com/LorisYounger/VPet/blob/main/README_zht.md#%E5%8B%95%E7%95%AB%E7%89%88%E6%AC%8A%E8%81%B2%E6%98%8E%E8%88%87%E6%8E%88%E6%AC%8A)。<br/>
註：一般而言，加入新功能都可以透過撰寫模組來達成，詳情請見：[VPet.Plugin.Demo](https://github.com/LorisYounger/VPet.Plugin.Demo)

作者可能會修改、刪減部分您所提交的程式碼，以確保該功能／玩法適用於虛擬桌寵模擬器。

## 動畫版權聲明與授權

在Github中，[桌寵動畫檔案](https://github.com/LorisYounger/VPet/tree/main/VPet-Simulator.Windows/mod/0000_core/pet/vup)之動畫版權歸[虛擬主播模擬器製作組](https://www.exlb.net/VUP-Simulator)所有，在使用本類別庫時，您可能會需要自行準備動畫檔，或遵循下列協定：

### 非商業用途授權

* 需要向使用者告知動畫檔案的來源，並提供造訪[本頁面](https://github.com/LorisYounger/VPet)的連結
* 當您完成上述要求後，可以免費使用動畫檔案

### 商業用途授權（低於10萬）

* 在使用者第一次使用時，需跳出視窗，並醒目向使用者告知動畫檔案的來源，並提供造訪[本頁面](https://github.com/LorisYounger/VPet)的連結
* 在對應的頁面上（使用者能快速造訪的），向使用者告知動畫檔案的來源，並提供造訪[本頁面](https://github.com/LorisYounger/VPet)的連結
* 當您完成上述要求後，可以免費使用動畫檔案

### 商業用途授權（高於10萬或其他）

* 請[電子郵件聯絡](mailto:zoujin.dev@exlb.org)本軟體作者

### 轉發動畫檔案

* 需要告知上述所有授權資訊
* 需要提供造訪[本頁面](https://github.com/LorisYounger/VPet)的連結
* 轉發動畫檔案時，禁止任何付費或收費行為

## 桌面應用程式部署方式

1. 下載本專案，透過VisualStudio開啟`VPet.sln`檔案
2. 在「建置」選項中，選擇位元數`x64`及建置專案`Vpet-Simulator.Windows`
   ![image-20230208004330895](README.assets/image-20230208004330895.png)
3. 點擊「開始」，若一切順利將會報錯`缺少Core模組，無法啟動桌寵`
4. 以管理員身分執行`mklink.bat`，這會讓模組檔案連結至產生的位置
5. 再次點擊啟動即可正常執行

## 軟體架構

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
