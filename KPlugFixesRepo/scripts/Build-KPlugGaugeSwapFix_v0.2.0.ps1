$ErrorActionPreference = "Stop"

$GameRoot = "F:\illusion\Koikatu"
$ExpectedKPlugSha256 = "34c13976108db0a18a7ad6b7cfda5517b4a9a83d85c9a909820144264c743855"
$ExpectedAssemblyCSharpSha256 = "0038281caf8df48a7903c55dc389642eeeb3f2a9114bd9d68ac11c8ac0396bc5"

$WorkDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $WorkDir
$SrcCandidates = @(
    (Join-Path $RepoRoot "src\GaugeSwapFix\KPlugGaugeSwapFix_v0.2.0.cs"),
    (Join-Path $WorkDir "KPlugGaugeSwapFix_v0.2.0.cs")
)
$Src = $SrcCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (!$Src) {
    throw "Source not found. Checked:`n  " + ($SrcCandidates -join "`n  ")
}

$DstDir = Join-Path $GameRoot "BepInEx\plugins\test"
$DstDll = Join-Path $DstDir "KPlugGaugeSwapFix.dll"
$TempDll = Join-Path $env:TEMP "KPlugGaugeSwapFix_v0.2.0.dll"

$KPlug = Join-Path $GameRoot "BepInEx\plugins\kPlug\kPlug.dll"
$AssemblyCSharp = Join-Path $GameRoot "Koikatu_Data\Managed\Assembly-CSharp.dll"
$BepInEx = Join-Path $GameRoot "BepInEx\core\BepInEx.dll"
$UnityEngine = Join-Path $GameRoot "Koikatu_Data\Managed\UnityEngine.dll"

$HarmonyCandidates = @(
    (Join-Path $GameRoot "BepInEx\core\0Harmony.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\0Harmony.dll"),
    (Join-Path $GameRoot "BepInEx\core\BepInEx.Harmony.dll")
)

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$DiagCandidates = @(
    (Join-Path $GameRoot "BepInEx\plugins\test\KPlugGaugeSwapDiag.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\KPlugGaugeSwapDiag.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\kPlug\KPlugGaugeSwapDiag.dll")
)

$FixCandidates = @(
    (Join-Path $GameRoot "BepInEx\plugins\test\KPlugGaugeSwapFix.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\KPlugGaugeSwapFix.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\kPlug\KPlugGaugeSwapFix.dll")
)

if (!(Test-Path $Src)) { throw "Source not found: $Src" }
if (!(Test-Path $KPlug)) { throw "kPlug.dll not found: $KPlug" }
if (!(Test-Path $AssemblyCSharp)) { throw "Assembly-CSharp.dll not found: $AssemblyCSharp" }
if (!(Test-Path $BepInEx)) { throw "BepInEx.dll not found: $BepInEx" }
if (!(Test-Path $UnityEngine)) { throw "UnityEngine.dll not found: $UnityEngine" }

$ActualKPlugSha256 = (Get-FileHash -Algorithm SHA256 $KPlug).Hash.ToLowerInvariant()
if ($ActualKPlugSha256 -ne $ExpectedKPlugSha256) {
    throw @"
Unexpected kPlug.dll SHA-256.
Expected: $ExpectedKPlugSha256
Actual:   $ActualKPlugSha256
No build/install was performed.
"@
}

$ActualAssemblyCSharpSha256 = (Get-FileHash -Algorithm SHA256 $AssemblyCSharp).Hash.ToLowerInvariant()
if ($ActualAssemblyCSharpSha256 -ne $ExpectedAssemblyCSharpSha256) {
    throw @"
Unexpected Assembly-CSharp.dll SHA-256.
Expected: $ExpectedAssemblyCSharpSha256
Actual:   $ActualAssemblyCSharpSha256
No build/install was performed.
"@
}

$Harmony = $HarmonyCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (!$Harmony) {
    throw "Harmony assembly not found. Checked:`n  " + ($HarmonyCandidates -join "`n  ")
}

$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (!$csc) { throw "csc.exe not found." }

if (Test-Path $TempDll) { Remove-Item -Force $TempDll }

# C#5 / Koikatu CLR2 compatibility checks.
$SourceText = Get-Content -Raw -Path $Src
$Forbidden = @("nameof(", '$"', "?.", "??=")
foreach ($pattern in $Forbidden) {
    if ($SourceText.Contains($pattern)) {
        throw "Compatibility guard failed: unsupported source syntax found: $pattern"
    }
}

$ForbiddenReflectionComparisons = @(
    "TCore == null", "TCore != null",
    "THCtrl == null", "THCtrl != null",
    "THFlag == null", "THFlag != null",
    "THSceneProc == null", "THSceneProc != null",
    "TToolUI == null", "TToolUI != null",
    "FieldInfo == null", "FieldInfo != null",
    "MethodInfo == null", "MethodInfo != null"
)
foreach ($pattern in $ForbiddenReflectionComparisons) {
    if ($SourceText.Contains($pattern)) {
        throw "CLR2 compatibility guard failed: forbidden Reflection null comparison found: $pattern"
    }
}

Write-Host ""
Write-Host "Phase 1/3: Compile to TEMP. No game plugin state is changed yet."
Write-Host "Compiler:"
Write-Host "  $csc"
Write-Host ""

& $csc /nologo /target:library /optimize+ `
    /out:"$TempDll" `
    /reference:"$BepInEx" `
    /reference:"$UnityEngine" `
    /reference:"$Harmony" `
    "$Src"

if ($LASTEXITCODE -ne 0 -or !(Test-Path $TempDll)) {
    if (Test-Path $TempDll) { Remove-Item -Force $TempDll }
    throw "Build failed. Existing game/plugin state was not changed."
}

$BuiltSha = (Get-FileHash -Algorithm SHA256 $TempDll).Hash.ToLowerInvariant()

Write-Host ""
Write-Host "Phase 2/3: Build succeeded. Isolating old GaugeSwap diagnostic/fix DLLs."

$MovedThisRun = @()
$seen = @{}
$toDisable = @($DiagCandidates + $FixCandidates)

foreach ($path in $toDisable) {
    if ($seen.ContainsKey($path)) { continue }
    $seen[$path] = $true

    if (Test-Path $path) {
        $disabled = $path + ".v020_off"
        if (Test-Path $disabled) {
            $disabled = $path + "." + (Get-Date -Format "yyyyMMdd-HHmmss") + ".v020_off"
        }

        Move-Item -Force $path $disabled
        $MovedThisRun += [PSCustomObject]@{ Original = $path; Disabled = $disabled }

        Write-Host "Disabled:"
        Write-Host "  $path"
        Write-Host "  -> $disabled"
    }
}

Write-Host ""
Write-Host "Phase 3/3: Installing KPlugGaugeSwapFix v0.2.0."

try {
    if (!(Test-Path $DstDir)) {
        New-Item -ItemType Directory -Path $DstDir | Out-Null
    }

    Copy-Item -Force $TempDll $DstDll

    if (!(Test-Path $DstDll)) {
        throw "Fix DLL copy failed."
    }

    $InstalledSha = (Get-FileHash -Algorithm SHA256 $DstDll).Hash.ToLowerInvariant()
    if ($InstalledSha -ne $BuiltSha) {
        throw "Installed DLL hash does not match TEMP build."
    }
}
catch {
    Write-Host ""
    Write-Host "Install failed. Rolling back files changed by this run..."

    if (Test-Path $DstDll) { Remove-Item -Force $DstDll }

    for ($i = $MovedThisRun.Count - 1; $i -ge 0; $i--) {
        $x = $MovedThisRun[$i]
        if ((Test-Path $x.Disabled) -and !(Test-Path $x.Original)) {
            Move-Item -Force $x.Disabled $x.Original
            Write-Host "Restored:"
            Write-Host "  $($x.Original)"
        }
    }

    if (Test-Path $TempDll) { Remove-Item -Force $TempDll }
    throw
}

Remove-Item -Force $TempDll

Write-Host ""
Write-Host "SUCCESS"
Write-Host ""
Write-Host "Installed:"
Write-Host "  $DstDll"
Write-Host "SHA-256:"
Write-Host "  $InstalledSha"
Write-Host ""
Write-Host "Verified kPlug.dll SHA-256:"
Write-Host "  $ActualKPlugSha256"
Write-Host "Verified Assembly-CSharp.dll SHA-256:"
Write-Host "  $ActualAssemblyCSharpSha256"
Write-Host ""
Write-Host "Expected startup log:"
Write-Host "  [GaugeSwapFix] v0.2.0 active. Target=HSceneProc::ChangeAnimator. Main-girl swap only."
Write-Host ""
Write-Host "Expected repair log when a post-swap animation/category change occurs:"
Write-Host "  [GaugeSwapFix] Rebound swapped main-girl Face/Gauge after ChangeAnimator. ..."
Write-Host ""
Write-Host "One in-game verification:"
Write-Host "  1) Enter H"
Write-Host "  2) Invite with I-key and swap invited girl to Main"
Write-Host "  3) Confirm swapped Face/Gauge"
Write-Host "  4) Reproduce the previous failing sequence (for example Bed -> Chair -> another category)"
Write-Host "  5) Confirm Face/Gauge stay on the swapped girl after every change"
Write-Host "  6) Stop if the bug reappears; provide the new output_log"
