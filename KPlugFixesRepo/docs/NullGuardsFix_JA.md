# KPlugNullGuardsFix v0.1.0

## 目的

kPlug 3.6.0 のバージン版から旧修正版へ直接組み込まれていた null guard 系修正を、kPlug.dll本体を改変せず独立したBepInEx / Harmonyプラグインとして再現する。

## 実装

実機成功版は以下で構成される。

- H処理系 Prefix guard: 9メソッド
- KokanBehavior Prefix guard: 2メソッド
- MenuCorner Prefix guard: 1メソッド
- Voice coroutine Transpiler: 1メソッド

起動ログ:

```text
[NullGuardsFix] Voice guard injected at AudioSource.clip -> AudioClip.length path.
[NullGuardsFix] v0.1.0 active. 12 Prefix guards + Voice Transpiler.
```

## Voice guard

対象:

`kPlug.CmpChara.FaceCtrl+<IE_PlayVoice>d__88::MoveNext()`

`AudioSource.clip -> AudioClip.length` の命令列を意味的アンカーとして使用し、音源またはclipが無効な場合にCoroutineを安全に終了させる。

固定ILオフセットには依存しない。アンカーが1箇所でない場合は曖昧なパッチを行わず失敗させる。

## 実機確認

確認済み:

1. Hシーン進入
2. 7キーでアニメーション変更
3. 途中でスワップ
4. スワップ後も7キー操作
5. 終了ボタンでH終了

結果:

- Hシーン進入 正常
- 7キー操作 正常
- スワップ後7キー操作 正常
- H終了 正常
- 対象guard由来の新規実害なし

## 成功版

KPlugNullGuardsFix.dll v0.1.0

SHA-256:

`e1e585f3961126adda211d140535ab7590ed2d47c6f0ce97bbd2f146565a1c1e`

## 注意

これはすべてのkPlug経路のnull問題を包括的に解決するものではない。対象を限定した成功済み修正として維持する。
