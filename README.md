# kPlug Piston Transition Fix

Koikatu + kPlug 3.6.0 環境で、H中にテンキー7 / Backspaceを使って挿入状態のアニメーションを変更した際に発生する問題を修正・改善するBepInExプラグインです。

この配布物は、役割の異なる2つのDLLで構成されています。

- **KPlugPistonWaitFix v1.0.0**  
  テンキー7 / Backspaceでアニメーション変更後、以後の切り替え操作が反応しなくなることがある問題を修正します。
- **KPlugPistonAutoResume v0.2.0**  
  ピストン動作中にテンキー7 / Backspaceでアニメーションを変更した後、ゲーム本来のGo処理を使ってピストン動作を自動再開します。

## 対象環境

動作確認環境:

- Koikatu 5.1
- kPlug 3.6.0
- BepInEx 5.4.23.5
- Unity 5.6.2f1
- CLR 2.0.50727.1433

動作確認に使用した `kPlug.dll` SHA-256:

`880b34324bf1563a1e09a02a6c0960918270fae7463132d47e5fc813bcf4607f`

## インストール

GitHub Releasesから配布ZIPを取得し、Koikatuのゲームフォルダへ展開してください。

展開後:

```text
Koikatu
└─ BepInEx
   └─ plugins
      └─ KPlugPistonTransitionFix
         ├─ KPlugPistonWaitFix.dll
         └─ KPlugPistonAutoResume.dll
```

両DLLを導入することを推奨します。

`KPlugPistonAutoResume.dll` は `KPlugPistonWaitFix.dll v1.0.0` を置き換えるものではありません。

## Auto Resumeの設定

初回起動後、以下のConfigが生成されます。

```text
BepInEx/config/local.kplug.pistonautoresume.cfg
```

主な設定:

```ini
[Piston Auto Resume]

Enabled = true
ResumeDelaySeconds = 0.8
```

`ResumeDelaySeconds` は、実際のAnimatorが `InsertIdle / A_InsertIdle` に到達してから、自動でGoを実行するまでの待ち時間です。

- `0.0` : 最速
- `0.8` : 標準
- 最大 `3.0` 秒
- 自動再開を使わない場合は `Enabled = false`

待機中に別のH操作やアニメーション変更が行われた場合、自動再開はキャンセルされます。

## 起動確認

正常に読み込まれた場合、BepInExログに以下が出ます。

```text
[PistonWaitFix] v1.0.0 active.
[PistonAutoResume] v0.2.0 active.
```

## アンインストール

以下の2ファイルを削除してください。

```text
BepInEx/plugins/KPlugPistonTransitionFix/KPlugPistonWaitFix.dll
BepInEx/plugins/KPlugPistonTransitionFix/KPlugPistonAutoResume.dll
```

Auto Resumeの設定も削除する場合:

```text
BepInEx/config/local.kplug.pistonautoresume.cfg
```

## ソース構成

```text
src/
├─ WaitFix/
│  └─ KPlugPistonWaitFix_v1.0.0.cs
└─ AutoResume/
   └─ KPlugPistonAutoResume_v0.2.0.cs

scripts/
├─ build_KPlugPistonWaitFix_v1.0.0.ps1
├─ build_KPlugPistonAutoResume_v0.2.0.ps1
└─ Make-Release.ps1

docs/
├─ KPlugPistonWaitFix_v1.0.0_README_ja.txt
└─ KPlugPistonAutoResume_v0.2.0_README_ja.txt
```

## ビルド

各ビルドスクリプトは、既定では以下のゲームフォルダを使用します。

```text
F:\illusion\Koikatu
```

環境が異なる場合はスクリプト内の `$GameRoot` を変更してください。

## 配布ZIPの作成

ゲーム側ですでにビルド・動作確認済みのDLLから配布ZIPを作る場合:

```powershell
cd scripts
.\Make-Release.ps1
```

生成物はリポジトリ直下の `release` フォルダに作成されます。

## 補足

Wait Fixは原因箇所だけを限定的に修正し、`Assembly-CSharp.dll` や `kPlug.dll` 自体は書き換えません。

Auto Resumeも、実際の `InsertIdle / A_InsertIdle` 到達を確認した後、通常のGo経路を使って再開する設計です。
