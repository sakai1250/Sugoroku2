# Unity 静的 UI 編集版

対象 Unity: **6000.2.15f1**

## 最初に開くとき

1. Unity Hub でこのフォルダを開く。最初の import / C# compile の完了後、Editor 拡張 `StaticUiFirstOpen` が **一度だけ** UI を Scene に書き込む。
2. Console に `[Static UI] 5 scenes baked` と `[Static UI] First-open conversion completed` が出たら完了。以後は普通に Unity Editor で配置を直して Scene を保存できる。
3. 自動処理に失敗したら、Console の error を確認し、**Tools > UI > 1. 全画面を静的化してSceneに保存** を実行する。完了後、Scene が保存される。
4. 変換完了後は、`Assets/Scenes` 内の Scene を開き、Hierarchy の UI を選んで RectTransform や TextMeshPro の内容を編集する。`StaticUiAuthoring` が付いた Canvas の固定配置は、Play 時に自動上書きしない。

**重要**: このアーカイブ生成環境には Unity Editor がない。元の Scene の既存の重なりは `.unity` を直接修正したが、コードが生成する GameObject は *初回 Unity import 時に Editor 内で生成・Scene 保存* する。この ZIP に同梱した `.unity` が、すでに全ての UI を生成済みという意味ではない。

## 変更範囲

- `TitleScene` : 設定 UI、ボタン、見出し。背景の生成を抑止。
- `CharacterSelectScene` : 選択カード全種類、設定・実績の modal、上下配置の調整。
- `GameUIScene` : HUD、リソースバー、スキル、アイテム、ポーズ（長い詳細を ScrollRect に配置）、イベント modal の固定部分。
- `ResultScene` : 結果の固定画面、画像保存・共有・再挑戦ボタン、統計グラフの枠。
- `GameOverScene` : 理由表示欄とボタン。
- 以上の **5 Scene それぞれに HelpOverlayCanvas（遊び方）を静的配置**。Hierarchy で非表示の子を一時的に有効化すると編集できる。
- `GameWorldScene` の 3D 盤面やゲームのイベント処理は変更しない。

**動的なまま残すもの**: イベントに応じて個数・文章が変わる選択ボタン、統計グラフのデータ点、数値、色や状態による表示変化、一時的な通知と演出。固定したい UI の位置と枠は Scene に保存する。

## 重なり対策

- HUD 左欄の状態表示・スキル欄・所持品欄の縦方向を分離した。
- キャラ名・特性名・説明を離して配置した。
- 長い説明は自動文字縮小または ScrollRect を使う。ボタンラベルは省略表示を使用。
- ダイス画像と数値文字の初期位置、タイトル設定画面の部品位置を調整した。

## 注意

- **再実行のたびに UI を生成し直す工具は使わない。** 特に既存の `Tools/Sugoroku/一発セットアップ` は Scene を作り直す可能性がある。
- この新しい変換機能も、Scene を手作業で編集した後に再実行すると生成時の配置に戻る。初回変換以降は通常の Scene 編集と保存を行う。
- ヘルプの初期文字は Scene 作成時の値として保存される。毎回内容が変わる情報は動的に更新する必要がある。
- Scene の実描画、異なる解像度への対応、ゲーム操作の動作検証は **未実施**。初回 import 後、Game View で表示とボタン動作を確認すること。
