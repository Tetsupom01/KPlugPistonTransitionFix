# KPlugFixes

Independent BepInEx / Harmony fixes for **Koikatu + kPlug 3.6.0**.

The fixes in this repository stay as separate DLLs so each issue can be updated, tested, enabled, or removed independently.

## Included fixes

### KPlugGaugeSwapFix v0.2.0

Fixes a kPlug H-scene issue where, after inviting a girl with the I-key and swapping her into Main, changing H animation/category could restore the Face/Gauge display to the original H-start state even though the actual partner remained the swapped girl.

Patch boundary: `HSceneProc.ChangeAnimator`.

The vanilla method is allowed to complete normally. The plugin only preserves the current swapped Face/Gauge state across that reinitialization boundary.

### KPlugNullGuardsFix v0.1.0

Reproduces the null-guard changes previously embedded in a modified kPlug build without modifying `kPlug.dll` itself.

It applies:

- 9 H-process guards
- 2 KokanBehavior guards
- 1 MenuCorner guard
- 1 Voice coroutine transpiler

Runtime result: 12 Prefix guards + 1 Voice Transpiler.

## Tested environment

- Koikatu 5.1
- kPlug 3.6.0
- BepInEx 5.4.23.5
- Unity 5.6.2f1
- CLR 2.0.50727.1433
- HF Patch v4.1
- Windows 11

Reference kPlug 3.6.0 SHA-256:

`34c13976108db0a18a7ad6b7cfda5517b4a9a83d85c9a909820144264c743855`

Reference Assembly-CSharp.dll SHA-256:

`0038281caf8df48a7903c55dc389642eeeb3f2a9114bd9d68ac11c8ac0396bc5`

## Repository layout

```text
src/
  GaugeSwapFix/
  NullGuardsFix/
scripts/
docs/
```

## Build policy

The source is intentionally compatible with the legacy .NET Framework compiler used by this Koikatu environment.

Successful runtime fixes are not merged into one DLL. Keeping them separate makes regression testing and rollback straightforward.

## Known successful binary hash

KPlugNullGuardsFix.dll v0.1.0:

`e1e585f3961126adda211d140535ab7590ed2d47c6f0ce97bbd2f146565a1c1e`

The known-good KPlugGaugeSwapFix v0.2.0 binary was runtime-tested successfully, but its successful binary SHA-256 was not recorded at the time of testing. No replacement hash is invented here.

## Related project

The piston transition fixes remain in the separate `KPlugPistonTransitionFix` project.
