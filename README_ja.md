# VPet

[简体中文](./README.md) | [繁體中文](./README_zht.md) | [English](./README_en.md) | 日本語

![Header](README.assets/%E4%B8%BB%E5%9B%BE.png)

オープンソースのデスクトップペット/shimeji/バーチャルペットアプリで、Windows Presentation Framework アプリにも組み込めます。

VPet を [Steam で](https://store.steampowered.com/app/1920960/VPet)無料で入手するか、[NuGet](https://www.nuget.org/packages/VPet-Simulator.Core) を使って WPF アプリにコアライブラリをインストールしましょう。

## はじめに

***VPet*** は、様々なインタラクションが実装されたデスクトップペットアプリです。オープンソースで無料、Steam ワークショップの MOD にも対応しています。~~無料なんだし、試してみてよ？~~

このゲームはもともと、*[VUP-Simulator](https://store.steampowered.com/app/1352140/_/)* のチュートリアルに付随するデスクトップペットとして開発されていましたが、その後独立したアプリに分割されました。気に入っていただけましたら、VUP-Simulator をウィッシュリストに追加してください。

### たくさんのインタラクションとアニメーション！

最大 32<sub>(タイプ)</sub> * 4<sub>(状態)</sub> * 3<sub>(バリエーション)</sub> = 384 のアニメーションが含まれています！ (ill バージョンやループなどを持たないタイプもあるので、実際の数値はもっと小さくなることに注意。)

#### いくつかの例:

##### 頭を撫でる

![ss0](README.assets/ss0.gif)

##### 持ち上げる

![ss4](README.assets/ss4.gif) ![ss4](README.assets/ss8.gif)

##### 壁のぼり

![ss7](README.assets/ss7.gif)

### 無料！

このゲームは **100% 無料**です! ~~だから、もし良ければ試してみてね。~~ <br/>
このゲームの主な目的は、バーチャル アンカー シミュレーターの人気テーブルである [バーチャル アンカー シミュレーター](https://store.steampowered.com/app/1352140/_/) を宣伝することです。

### オープンソース！

このゲームのソースは GitHub で公開されています。次の場所で見つけることができます: https://github.com/LorisYounger/VPet

機能リクエストやプルリクエストは大歓迎です！私たちのコードをあなたの好みに合わせて修正することもできます。(ほとんどのコンテンツはその必要はなく、改造が可能ですが。)

### Steam Workshop 対応！

Steam Workshop の MOD に対応しています。MOD を使用すると、独自のペット（アニメーション/インタラクション）を追加したり、Workshop を通じて他の人と共有したりすることができます。

MOD プロデューサ:  https://github.com/LorisYounger/VPet.ModMaker

以下のコンテンツは、Workshop の MOD によって追加または変更することができます:

* ペットアニメーション
* アイテム、食べ物/飲み物など
* 仕事
* 対話
* テーマ
* プラグイン - コードでペットに追加コンテンツを追加します。例えば:
  * 新しいアニメーションロジック/ソリューション（Live2D や Spine など）
  * 新しい機能 (アラームやメモなど)
  * 基本的には何でも - 例は [VPet.Plugin.Demo](https://github.com/LorisYounger/VPet.Plugin.Demo) を参照してください。

### お問い合わせ（ご意見ご感想）

ご意見、ご感想はこちらまでお寄せください：
  * Steam のコメントまたはコミュニティ
  * GitHub [Issues](https://github.com/LorisYounger/VPet/issues/new)
  * または私の E メール ([service@exlb.net](mailto:service@exlb.net))

## ソフトウェアアーキテクチャ

* **VPet-Simulator.Windows** - デスクトップ用ペットシミュレーター
  * *Function* - 機能コード用
    * CoreMOD - mod の管理
    * MWController - ウィンドウコントローラー

  * *WinDesign* - ウィンドウと UI デザイン用
    * winBetterBuy - Betterbuy ウィンドウ
    * winCGPTSetting - ChatGPT 設定
    * winSetting - アプリ設定/mod 設定
    * winConsole - 開発用コンソール
    * winGameSetting - ゲーム設定
    * winReport - フィードバックセンター

  * MainWindows - メインウィンドウ; コアを保存し提示する
  * PetHelper - 素早くペットを切り替えるためのもの
* **VPet-Simulator.Tool** - フレームジェネレーターなど MOD の作成を補助するツール
* **VPet-Simulator.Core** - アプリのコアで、VUP-Simulator などの他の WPF アプリケーションに組み込むためのもの
  * Handle - インターフェースとコントロール
    * IController - フォームコントローラ; サイドへの移動など、関連する機能と設定が含まれます
    * Function - 一般的な関数
    * GameCore - ゲームのコア; 様々なデータなどが含まれます。
    * GameSave - セーブ機能
    * IFood - アイテムと食べ物のインターフェイス
    * PetLoader - アニメーションローダー
  * Graph - グラフィックスレンダリング
    * IGraph - アニメーションの基本インターフェース
    * GraphCore - アニメーション表示のコア
    * GraphHelper - アニメーションヘルパークラス
    * GraphInfo - アニメーション情報
    * FoodAnimation - 3 層サンドイッチアニメーションの表示に特化したサポート（必ずしも食べ物だけとは限らない。）
    * PNGAnimation - アニメーション用のコンポーネント
    * Picture - 静的アニメーション用コンポーネント
  * Display - 表示用
    * basestyle/Theme - 基本スタイル
    * Main.xaml - 表示の中核となるコンポーネント
      * MainDisplay - コアの表示メソッド
      * MainLogic - コアの表示ロジック
    * ToolBar - ペットがクリックされたときに表示されるツールバー
    * MessageBar - ペットが話すときのダイアログバブル
    * WorkTimer - 作業タイマー (duh)

## コントリビュート

開発への参加を歓迎します！コードの保守性とプレイアビリティを確保するため、新しい機能やゲームプレイを開発したい場合は、まず私に連絡してください（[mail](mailto:zoujin.dev@exlb.org) を送るか、[Issue](https://github.com/LorisYounger/VPet/issues/new) を開いて）。これは、あなたの貢献がゲームに適合していることを確認するためであり、適合していない（あなたの努力が無駄になる）ことを理由に却下されることはありません。エラーやバグの修正に関して私に連絡する必要はありません。

私があなたのアイデアを承認した後、あなたはコードリポジトリを[フォーク](https://github.com/LorisYounger/VPet/fork)して変更を加え、[プルリクエスト](https://github.com/LorisYounger/VPet/compare)を開いて提出することができます。もし承認されなかったら、いつでもこのゲームのあなた自身のバージョンを作ることができます（その場合、[Apache License version 2.0](LICENSE) と[アニメーションの著作権表示と許諾条件](#アニメーションの著作権表示と許諾条件)が適用されます）。

あなたが貢献した機能/ゲームプレイがゲームに合っていることを確認するために、私はあなたのコードに変更を加えるかもしれないことに注意してください。

また、新機能の追加は通常プラグインで実現できることに注意してください。詳しくは [VPet.Plugin.Demo](https://github.com/LorisYounger/VPet.Plugin.Demo) を参照してください。

以下の参加開発者および翻訳者に感謝します

<a href="https://github.com/LorisYounger/VPet/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=LorisYounger/VPet" />
</a>

そして、Steam ワークショップのユーザーたちは、翻訳などをコミュニティと共有しています。

## アニメーションの著作権表示と許諾条件

ソースコードで提供されている[ペットアニメーションファイル](./VPet-Simulator.Windows/mod/0000_core/pet/vup)の著作権は [VUP-Simulator チーム](https://www.exlb.net/VUP-Simulator)に帰属します。このゲームのコアライブラリをあなたのアプリケーションで使用する場合、あなた自身のアニメーションファイルを使用することも、私たちのアニメーションファイルを使用することもできます。以下の条件に従っていただければ、私たちのアニメーションを自由に使用することができます:

> **注**
> この著作権表示は、デフォルトのアニメーションファイルにのみ適用され、サードパーティによって作成されたカスタムアニメーションファイルには適用されません。

### 非商用目的での使用

私たちのアニメーションファイルのソースをユーザーに知らせ、[このページ](https://github.com/LorisYounger/VPet)へのリンクを**提供しなければなりません**。

### 商業目的で使用する場合

* まずは[メールで](mailto:zoujin.dev@exlb.org)ご連絡ください。
* あなたのアプリケーションの最初の使用時に、あなたはポップアップ・ウィンドウを表示し、私たちのアニメーションファイルのソースを目立つように知らせ、[このページ](https://github.com/LorisYounger/VPet)へのリンクを**提供しなければなりません**。
* 適切なページ(ユーザーが簡単にアクセスできる)で、あなたは私たちのアニメーションファイルのソースをユーザーに知らせ、[このページ](https://github.com/LorisYounger/VPet)へのリンクを**提供しなければなりません**。
* 私たちのアニメーションファイルを販売することで、**利益を得てはなりません**。

### 配布用

* 上記の承認情報はすべて**開示されなければならない**。
* [このページ](https://github.com/LorisYounger/VPet)へのリンクを提供しなければなりません。
* 私たちのファイルで**利益を得るべきではありません**。

### 画像の著作権に関する声明と許可

* プログラムに組み込まれている画像の著作権は上記と同じです。
* Zipフォトギャラリーの商用利用は禁止します

## Vpet-Simulator.Windows のデプロイ方法

1. ソースコードをダウンロードし、Visual Studio で `VPet.sln` を開く。
2. 生成するプロジェクトを `Vpet-Simulator.Windows` に、設定を `x64` に変更する。
   ![Demonstration of the above](README.assets/image-20230208004330895.png)
3. `Run` をクリックする。問題がなければ、以下のメッセージが表示されます: `Lack Mod Core, Unable start desktop pet` と表示されます
4. `Vpet-Simulator.Windows/mklink.bat` を管理者として実行します。これは `mod` フォルダをビルドフォルダにリンクします。
5. もう一度 `Run` をクリックすると、今度はアプリが実行されます。
