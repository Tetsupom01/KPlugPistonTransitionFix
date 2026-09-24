$ErrorActionPreference = "Stop"

$GameRoot = "F:\illusion\Koikatu"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

$Src = Join-Path $RepoRoot "src\AutoResume\KPlugPistonAutoResume_v0.2.0.cs"
$DstDir = Join-Path $GameRoot "BepInEx\plugins\test"
$DstDll = Join-Path $DstDir "KPlugPistonAutoResume.dll"
$TempDll = Join-Path $env:TEMP "KPlugPistonAutoResume.dll"

$BepInEx = Join-Path $GameRoot "BepInEx\core\BepInEx.dll"
$UnityEngine = Join-Path $GameRoot "Koikatu_Data\Managed\UnityEngine.dll"

$HarmonyCandidates = @(
    (Join-Path $GameRoot "BepInEx\core\0Harmony.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\0Harmony.dll"),
    (Join-Path $GameRoot "BepInEx\core\BepInEx.Harmony.dll")
)

$Harmony = $HarmonyCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (!$Harmony) {
    throw "Harmony assembly not found. Checked:`n  " + ($HarmonyCandidates -join "`n  ")
}

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (!$csc) { throw "csc.exe not found." }

if (!(Test-Path $Src)) { throw "Source not found: $Src" }
if (!(Test-Path $BepInEx)) { throw "BepInEx.dll not found: $BepInEx" }
if (!(Test-Path $UnityEngine)) { throw "UnityEngine.dll not found: $UnityEngine" }

if (Test-Path $TempDll) { Remove-Item -Force $TempDll }

& $csc /nologo /target:library /optimize+ `
    /out:"$TempDll" `
    /reference:"$BepInEx" `
    /reference:"$UnityEngine" `
    /reference:"$Harmony" `
    "$Src"

if ($LASTEXITCODE -ne 0) { throw "Build failed. Exit code: $LASTEXITCODE" }

if (!(Test-Path $DstDir)) {
    New-Item -ItemType Directory -Path $DstDir | Out-Null
}

if (Test-Path $DstDll) {
    $BackupDll = Join-Path $DstDir ("KPlugPistonAutoResume_backup_" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".dll")
    Copy-Item -Force $DstDll $BackupDll
    Write-Host "Existing DLL backed up:"
    Write-Host "  $BackupDll"
    Write-Host ""
}

Copy-Item -Force $TempDll $DstDll
Remove-Item -Force $TempDll

Write-Host ""
Write-Host "Built and installed:"
Write-Host "  $DstDll"
Write-Host ""
Write-Host "Keep KPlugPistonWaitFix.dll v1.0.0 installed."
Write-Host ""
Write-Host "Config file will be created/updated at:"
Write-Host "  F:\illusion\Koikatu\BepInEx\config\local.kplug.pistonautoresume.cfg"
Write-Host ""
Write-Host "Expected startup log:"
Write-Host "  [PistonAutoResume] v0.2.0 active. Enabled=True, ResumeDelaySeconds=0.80"
