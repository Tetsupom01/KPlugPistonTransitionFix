kPlug Piston Transition Fix v1.0.0

■ 内容

この配布物には以下の2つのBepInExプラグインが含まれます。

1. KPlugPistonWaitFix.dll v1.0.0
   テンキー7 / Backspaceで挿入状態のアニメーションを変更した後、
   以後のアニメーション切り替えが反応しなくなることがある問題を修正します。

2. KPlugPistonAutoResume.dll v0.2.0
   ピストン動作中にテンキー7 / Backspaceでアニメーションを変更した後、
   ゲーム本来のGo処理を使ってピストン動作を自動再開します。

■ 対象

Koikatu 5.1
kPlug 3.6.0
BepInEx 5.4.23.5

動作確認に使用した kPlug.dll SHA-256:
880b34324bf1563a1e09a02a6c0960918270fae7463132d47e5fc813bcf4607f

■ インストール

ZIPの中身をKoikatuのゲームフォルダへ展開してください。

導入後:

BepInEx\plugins\KPlugPistonTransitionFix\KPlugPistonWaitFix.dll
BepInEx\plugins\KPlugPistonTransitionFix\KPlugPistonAutoResume.dll

■ Auto Resume設定

初回起動後:

BepInEx\config\local.kplug.pistonautoresume.cfg

主な設定:

Enabled = true
ResumeDelaySeconds = 0.8

0.0 = 最速
0.8 = 標準
最大3.0秒

自動再開を無効にする場合:
Enabled = false

■ 起動確認

BepInExログ:

[PistonWaitFix] v1.0.0 active.
[PistonAutoResume] v0.2.0 active.

■ アンインストール

以下を削除してください。

BepInEx\plugins\KPlugPistonTransitionFix\KPlugPistonWaitFix.dll
BepInEx\plugins\KPlugPistonTransitionFix\KPlugPistonAutoResume.dll

必要に応じて以下のConfigも削除してください。

BepInEx\config\local.kplug.pistonautoresume.cfg
