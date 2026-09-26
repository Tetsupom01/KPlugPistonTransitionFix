param(
    [string]$GameRoot = "F:\illusion\Koikatu",
    [switch]$Install
)

$ErrorActionPreference = "Stop"

$WorkDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $WorkDir
$SrcCandidates = @(
    (Join-Path $RepoRoot "src\NullGuardsFix\KPlugNullGuardsFix_v0.1.0.cs"),
    (Join-Path $WorkDir "KPlugNullGuardsFix_v0.1.0.cs")
)
$Src = $SrcCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (!$Src) {
    throw "Source not found. Checked:`n  " + ($SrcCandidates -join "`n  ")
}

$BuildDir = Join-Path $WorkDir "build\Test"
$BuildDll = Join-Path $BuildDir "KPlugNullGuardsFix.dll"

$DstDir = Join-Path $GameRoot "BepInEx\plugins\test"
$DstDll = Join-Path $DstDir "KPlugNullGuardsFix.dll"

$BepInEx = Join-Path $GameRoot "BepInEx\core\BepInEx.dll"
$UnityEngine = Join-Path $GameRoot "Koikatu_Data\Managed\UnityEngine.dll"

$HarmonyCandidates = @(
    (Join-Path $GameRoot "BepInEx\core\0Harmony.dll"),
    (Join-Path $GameRoot "BepInEx\plugins\0Harmony.dll"),
    (Join-Path $GameRoot "BepInEx\core\BepInEx.Harmony.dll")
)

$Harmony = $HarmonyCandidates |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1

if (!$Harmony) {
    throw "Harmony assembly not found. Checked:`n  " +
        ($HarmonyCandidates -join "`n  ")
}

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$csc = $cscCandidates |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1

if (!$csc) {
    throw "csc.exe not found."
}

foreach ($path in @($Src, $BepInEx, $UnityEngine, $Harmony)) {
    if (!(Test-Path $path)) {
        throw "Required file not found: $path"
    }
}

New-Item -ItemType Directory -Force -Path $BuildDir |
    Out-Null

if (Test-Path $BuildDll) {
    Remove-Item -Force $BuildDll
}

Write-Host ""
Write-Host "Building KPlugNullGuardsFix v0.1.0..."
Write-Host "Source : $Src"
Write-Host "Harmony: $Harmony"
Write-Host ""

& $csc /nologo /target:library /optimize+ `
    /out:"$BuildDll" `
    /reference:"$BepInEx" `
    /reference:"$UnityEngine" `
    /reference:"$Harmony" `
    "$Src"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed. Exit code: $LASTEXITCODE"
}

if (!(Test-Path $BuildDll)) {
    throw "Build reported success but DLL was not created."
}

$hash = (Get-FileHash $BuildDll -Algorithm SHA256).Hash.ToLower()

Write-Host ""
Write-Host "Build succeeded:"
Write-Host "  $BuildDll"
Write-Host "SHA-256:"
Write-Host "  $hash"

if ($Install) {
    New-Item -ItemType Directory -Force -Path $DstDir |
        Out-Null

    if (Test-Path $DstDll) {
        $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $backup = Join-Path `
            $DstDir `
            ("KPlugNullGuardsFix.dll.backup_" + $stamp)

        Copy-Item -Force $DstDll $backup

        Write-Host ""
        Write-Host "Existing DLL backed up:"
        Write-Host "  $backup"
    }

    Copy-Item -Force $BuildDll $DstDll

    Write-Host ""
    Write-Host "Installed:"
    Write-Host "  $DstDll"
}
else {
    Write-Host ""
    Write-Host "Build only. Game folder was NOT modified."
    Write-Host "To install deliberately, run:"
    Write-Host ('  powershell.exe -ExecutionPolicy Bypass -File "' +
        $MyInvocation.MyCommand.Path + '" -Install')
}

Write-Host ""
Write-Host "Expected startup logs after installation:"
Write-Host "  [NullGuardsFix] Voice guard injected ..."
Write-Host "  [NullGuardsFix] v0.1.0 active. 12 Prefix guards + Voice Transpiler."
