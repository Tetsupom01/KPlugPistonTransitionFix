> **Repository migration notice**  
> This project has been consolidated into **Tetsupom01/KPlugFixes** under the `PistonTransitionFix` folders.  
> This repository is kept as historical reference for the original standalone release. New maintenance should be done in `KPlugFixes`.

---

# kPlug Piston Transition Fix

BepInEx plugins for Koikatu + kPlug 3.6.0 that fix insertion-animation switching issues and optionally resume piston motion automatically after using Numpad 7 / Backspace.

This project contains two separate plugins with different roles:

- **KPlugPistonWaitFix v1.0.0**  
  Fixes an issue where animation navigation can stop responding after changing an insertion-state animation with Numpad 7 / Backspace.
- **KPlugPistonAutoResume v0.2.0**  
  Automatically resumes piston motion through the game's normal Go route after switching insertion/piston animations while motion is active.

## Target environment

Validated with:

- Koikatu 5.1
- kPlug 3.6.0
- BepInEx 5.4.23.5
- Unity 5.6.2f1
- CLR 2.0.50727.1433

Validated `kPlug.dll` SHA-256:

`880b34324bf1563a1e09a02a6c0960918270fae7463132d47e5fc813bcf4607f`

## Installation

Download the release ZIP from GitHub Releases and extract it into your Koikatu game folder.

Expected layout:

```text
Koikatu
└─ BepInEx
   └─ plugins
      └─ KPlugPistonTransitionFix
         ├─ KPlugPistonWaitFix.dll
         └─ KPlugPistonAutoResume.dll
```

Installing both DLLs is recommended.

`KPlugPistonAutoResume.dll` does **not** replace `KPlugPistonWaitFix.dll v1.0.0`.

## Auto Resume configuration

After the first launch, the following config file is created:

```text
BepInEx/config/local.kplug.pistonautoresume.cfg
```

Main settings:

```ini
[Piston Auto Resume]

Enabled = true
ResumeDelaySeconds = 0.8
```

`ResumeDelaySeconds` controls how long Auto Resume waits after the real Animator state reaches `InsertIdle` / `A_InsertIdle` before triggering the normal Go action.

- `0.0` = fastest
- `0.8` = default
- up to `3.0` seconds
- set `Enabled = false` to disable Auto Resume

If another H-scene command or animation change occurs while Auto Resume is waiting, the scheduled resume is cancelled instead of overwriting that action.

## Startup check

When both plugins load successfully, the BepInEx log should contain:

```text
[PistonWaitFix] v1.0.0 active.
[PistonAutoResume] v0.2.0 active.
```

## Uninstallation

Remove:

```text
BepInEx/plugins/KPlugPistonTransitionFix/KPlugPistonWaitFix.dll
BepInEx/plugins/KPlugPistonTransitionFix/KPlugPistonAutoResume.dll
```

To remove the Auto Resume configuration as well:

```text
BepInEx/config/local.kplug.pistonautoresume.cfg
```

## Repository structure

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
├─ KPlugPistonAutoResume_v0.2.0_README_ja.txt
└─ README_配布用.txt
```

Japanese documentation is available in the `docs/` folder.

## Building from source

The included PowerShell build scripts use the following game path by default:

```text
F:\illusion\Koikatu
```

If your installation is elsewhere, edit `$GameRoot` in the script.

## Creating a release ZIP

If you already have tested DLLs installed in your game folder:

```powershell
cd scripts
.\Make-Release.ps1
```

The generated ZIP is placed in the repository's `release` folder.

## Technical notes

Wait Fix patches only the `WaitUntil` predicate used by `AI_Main.IE_AskPistonChange`. It does not directly modify `Assembly-CSharp.dll` or `kPlug.dll`.

Auto Resume waits until the actual female Animator reaches `InsertIdle` / `A_InsertIdle`, then uses the game's normal Go path. It also cancels the pending resume when the context changes, so user input is not overwritten.
