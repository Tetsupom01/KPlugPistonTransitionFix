# KPlugFixes v1.0.0

Initial source release containing two independent, runtime-tested kPlug fixes.

## KPlugGaugeSwapFix v0.2.0

- Fixes Face/Gauge state reverting after a Main-girl swap when the H category/animation changes.
- Patches only `HSceneProc.ChangeAnimator` with one Prefix/Postfix pair.
- Leaves vanilla animator reinitialization intact.
- Reapplies existing kPlug `faceOverwrite` and restores the preserved gauges afterward.
- Runtime verification completed successfully.

## KPlugNullGuardsFix v0.1.0

- Reproduces the successful null guards formerly embedded in a directly modified kPlug build.
- Uses 12 Prefix guards plus one Voice coroutine transpiler.
- Does not modify `kPlug.dll`.
- Runtime verification completed successfully.
- Known-good DLL SHA-256:
  `e1e585f3961126adda211d140535ab7590ed2d47c6f0ce97bbd2f146565a1c1e`

## Packaging decision

The two fixes remain separate DLLs. They are grouped only at repository/distribution level to preserve independent testing, updates, and rollback.

## Binary release status

This staging release is source-oriented. The successful GaugeSwapFix v0.2.0 runtime binary hash was not recorded, so no binary release package is being fabricated from a newly rebuilt DLL.
