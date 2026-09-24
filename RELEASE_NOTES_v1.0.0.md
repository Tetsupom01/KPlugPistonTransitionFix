# v1.0.0

初回配布版。

## 同梱
- KPlugPistonWaitFix v1.0.0
- KPlugPistonAutoResume v0.2.0

## 主な内容
- kPlug 3.6.0でテンキー7 / Backspaceによる挿入アニメーション変更後に操作不能になる問題を修正。
- ピストン中のアニメーション変更後に通常のGo経路で自動再開。
- Auto Resumeの再開待ち時間をConfigで0.0～3.0秒に設定可能。既定値0.8秒。
- 待機中に別操作が入った場合は自動再開をキャンセル。
