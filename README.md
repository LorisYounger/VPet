# VPet-Simulator

虚拟桌宠模拟器 一个开源的桌宠软件, 可以内置到任何WPF应用程序

![主图](README.assets/%E4%B8%BB%E5%9B%BE.png)

获取虚拟桌宠模拟器 [OnSteam(免费)](https://store.steampowered.com/app/1920960/VPet) 或 通过[Nuget]()内置到你的WPF应用程序

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

详情请参见 开源文档中的 [图像资源](https://github.com/LorisYounger/VPet#%E5%9B%BE%E5%83%8F%E8%B5%84%E6%BA%90).

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
* 桌宠工作
* 说话文本
* 主题

### 反馈&建议&联系我们

如果有建议或者意见,可以在Steam商店评论/社区,Github Issue,虚拟主播模拟器贴吧,虚拟主播模拟器群(430081239)或者邮件联系我 [mailto:service@exlb.net](mailto:service@exlb.net)

## 软件结构

* VPet-Simulator.Windows: 适用于桌面端的虚拟桌宠模拟器
  * MainWindows 主窗体,存放和展示Core
  * WinSetting.xaml 软件设置/MOD 窗口
  * MWController.cs 窗体控制器
* VPet-Simulator.Tool: 方便制作MOD的工具(eg:图片帧生成)
* VPet-Simulator.Core: 软件核心 方便内置到任何WPF应用程序(例如:VUP-Simulator)
  * Handle 接口与控件
    * IController 窗体控制器 (调用相关功能,例如移动到侧边等)
    * Function 通用功能
    * GameCore 游戏核心,包含各种数据等内容
    * Save 游戏存档等
    * Setting 游戏设置
  * Graph 图形渲染
    * IGraph 图形基本接口
    * IEyeTracking 眼部跟踪
    * PNGAnimation 桌宠动画组件
    * Picture 桌宠静态组件
  * Item 所有物品
    * Item 所有物品类
    * Food 可以吃的食物
    * Drink 可以喝的饮料
  * Display 显示
    * Main.xaml 核心显示部件
      * MainDisplay 核心显示方法
      * MainLogic 核心显示逻辑
    * ToolBar 点击人物时候的工具栏
    * MessageBar 人物说话时候的说话栏
    * Theme 主题

## 游戏设计

* 金钱 钱不是是万能的,没钱是万万不能的
* 人物数据
  * 经验/等级
    * 盈利速度加成
    * 解锁更多对话等
  * 体力
    * 工作/摸头/学习 消耗体力
    * 自然百分比回复(在饱腹度>50%)
    * 睡觉回复(饱腹度>=25)
  * 饱腹度 固定上限100
    * 工作/学习 消耗饱腹度
    * 自然下降
    * 进食 回复
  * 心情 固定上限100
    * 工作/学习消耗
    * 摸头回复
    * 心情>=75时同时增加经验
  * 口渴度 固定上限100
    * 自然下降
    * 喝水回复
    * 低于 25 加生病条
  * 隐藏条:
    * 生病条: 
      * 生病的概率
      * 心情<=25增加概率 心情>=75缓慢减少
      * 打工/学习增加固定百分比
      * 体力<=40 增加概率
      * 降低心情
    * 好感度
      * 心情<=25 降低+与心情同步减少
      * 心情>=90与心情同步增加
* 人物互动
  * 摸头
  * 摸身子
  * 喂食
  * 喂水
  * 去打工
  * 去学习
  * 去睡觉
  * 玩耍
* 人物隐藏互动
  * 被拉起 (切换位置)
  * 爬墙
  * 爬地板
  * 躲藏 (被发现加心情)

## 图像资源

图像资源可能需要拆分以至于支持动图

* 模式 (其他状态得制作符合该模式的表情/动作) 部分模式无特殊动作
  * 高兴 - 兴奋的表情/肢体动作
  * 普通 - 一般的表情/肢体动作
  * 状态不佳 - 情绪低落/肢体动作
  * 生病(躺床) - 单独做个生病躺床的拆分
* IDEL 待机状态
  * 发呆 - (可以准备多个)
  * 盯着鼠标看 (需要拆分眼睛和眨眼等)
  * 等等 - 其他待机状态,例如不小心睡着等,或者换几个姿势(换姿势无生病)
* 人物互动
  * 摸头 - 摸头,包含高兴和附魔的手 可以准备不同的动画和表情
  * 摸身子 - 嫌弃/害羞/弹问号/等
  * 喂食 - 吃掉食物动作和动画
    * 水果 ($15 生病条-2饱腹度+10)
    * 汉堡 ($20 饱腹度+40 生病条+1 口渴-10)
    * 沙拉 ($20 饱腹度+20 生病条-2)
    * 药丸
    * 更多
  * 喂水 - 喝水和更多动作和动画
    * 普通水
    * 矿泉水
    * 饮料
    * 药水
    * 更多
  * 去打工 (无生病)
    * 去当虚拟主播 (更高层次eg:高级设备) lv>=20
    * 去当虚拟主播 (符合游戏设定 普通设备) lv>=10
    * 去编写程序 lv>=5
    * 去当外聘客服 全通用
  * 去学习 (无生病)
    * 看书 (不同的颜色决定等级)
  * 去睡觉 
  * 玩耍 (无生病)
    * 玩游戏机
    * 运动(打球等)
    * 小游戏(可以后续设计,例如井字棋等等)
* 人物隐藏互动 
  * 被拉起 (切换位置)(无生病,生病时使用状态不佳)
  * 爬墙 (无状态不佳/生病))
  * 爬地板 (无状态不佳/生病))
  * 躲藏 (被发现加心情 无状态不佳/生病)

## 参与开发

欢迎参与虚拟桌宠模拟器的开发! 为保证代码可维护度和游戏性,如果想要开发新的功能,请先[邮件联系](mailto:zoujin.dev@exlb.org)我想要添加的功能/玩法, 以确保该功能/玩法适用于虚拟桌宠模拟器. 以免未来提交时因不合适被拒(而造成代码浪费)<br/>
如果是修复错误或者BUG,无需联系我,修好后直接PR即可

当想法通过后,您可以通过 [fork](https://github.com/LorisYounger/VPet/fork) 功能拷贝代码至自己的github以方便编写自己的代码, 编写完毕后通过[pull requests](https://github.com/LorisYounger/VPet/compare) 提交<br/>
如果您想法没有被通过,也可以另起炉灶,写个不同版本功能的桌宠软件. 但需遵守 [Apache License 2.0](https://github.com/LorisYounger/VPet/blob/main/LICENSE) 与 [动画版权声明与授权](https://github.com/LorisYounger/VPet#%E5%8A%A8%E7%94%BB%E7%89%88%E6%9D%83%E5%A3%B0%E6%98%8E%E4%B8%8E%E6%8E%88%E6%9D%83)

我可能会对您的提交的代码进行修改,删减等以确保该功能/玩法适用于虚拟桌宠模拟器.

感谢以下参与的开发人员(按贡献程度排序)

* 占位符

## 动画版权声明与授权

在github中 [VPet/VPet-Simulator.Windows/mod/0000_core/pet/vup/](https://github.com/LorisYounger/VPet/tree/main/VPet-Simulator.Windows/mod/0000_core/pet/vup) 动画版权归 [虚拟主播模拟器制作组](https://www.exlb.net/VUP-Simulator)所有, 当使用本类库时,您可能需要自行准备动画文件,或遵循以下协议

### 非商用用途授权

* 需要向用户告知动画文件来源并提供访问 [该页面](https://github.com/LorisYounger/VPet) 的链接
* 当您完成以上要求后,您可以免费使用动画文件

### 商用用途授权(低于10万)

* 第一次使用时需弹窗并醒目的向用户告知动画文件来源并提供访问 [该页面](https://github.com/LorisYounger/VPet) 的链接
* 在相应页面(用户可以快捷访问)向用户告知动画文件来源并提供访问 [该页面](https://github.com/LorisYounger/VPet) 的链接
* 当您完成以上要求后,您可以免费使用动画文件

### 商用用途授权(高于10万或其他)

* 请[邮件联系](mailto:zoujin.dev@exlb.org)我

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
5. 下次点击启动即可正常运行