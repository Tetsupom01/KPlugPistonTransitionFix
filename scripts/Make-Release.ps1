param(
    [string]$GameRoot = "F:\illusion\Koikatu",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$ReleaseDir = Join-Path $RepoRoot "release"
$StageRoot = Join-Path $ReleaseDir "_staging"
$PackageName = "KPlugPistonTransitionFix_v$Version"
$Stage = Join-Path $StageRoot $PackageName
$PluginDir = Join-Path $Stage "BepInEx\plugins\KPlugPistonTransitionFix"
$ZipPath = Join-Path $ReleaseDir ($PackageName + ".zip")

$WaitFixDll = Join-Path $GameRoot "BepInEx\plugins\test\KPlugPistonWaitFix.dll"
$AutoResumeDll = Join-Path $GameRoot "BepInEx\plugins\test\KPlugPistonAutoResume.dll"

$ReleaseReadme = Join-Path $RepoRoot "docs\README_配布用.txt"
$WaitFixReadme = Join-Path $RepoRoot "docs\KPlugPistonWaitFix_v1.0.0_README_ja.txt"
$AutoResumeReadme = Join-Path $RepoRoot "docs\KPlugPistonAutoResume_v0.2.0_README_ja.txt"

foreach ($p in @($WaitFixDll, $AutoResumeDll, $ReleaseReadme, $WaitFixReadme, $AutoResumeReadme)) {
    if (!(Test-Path $p)) {
        throw "Required file not found: $p"
    }
}

if (Test-Path $StageRoot) {
    Remove-Item -Recurse -Force $StageRoot
}
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null

Copy-Item -Force $WaitFixDll (Join-Path $PluginDir "KPlugPistonWaitFix.dll")
Copy-Item -Force $AutoResumeDll (Join-Path $PluginDir "KPlugPistonAutoResume.dll")
Copy-Item -Force $ReleaseReadme (Join-Path $Stage "README.txt")

$DocsDir = Join-Path $Stage "docs"
New-Item -ItemType Directory -Force -Path $DocsDir | Out-Null
Copy-Item -Force $WaitFixReadme (Join-Path $DocsDir "KPlugPistonWaitFix_v1.0.0_README_ja.txt")
Copy-Item -Force $AutoResumeReadme (Join-Path $DocsDir "KPlugPistonAutoResume_v0.2.0_README_ja.txt")

$hashLines = @()
foreach ($dll in @(
    (Join-Path $PluginDir "KPlugPistonWaitFix.dll"),
    (Join-Path $PluginDir "KPlugPistonAutoResume.dll")
)) {
    $h = Get-FileHash -Algorithm SHA256 $dll
    $hashLines += ($h.Hash.ToLower() + "  " + (Split-Path -Leaf $dll))
}
$hashLines | Set-Content -Encoding ASCII (Join-Path $Stage "SHA256SUMS.txt")

if (Test-Path $ZipPath) {
    Remove-Item -Force $ZipPath
}

Compress-Archive -Path (Join-Path $Stage "*") -DestinationPath $ZipPath -CompressionLevel Optimal

Write-Host ""
Write-Host "Release package created:"
Write-Host "  $ZipPath"
Write-Host ""
Write-Host "Included DLL hashes:"
Get-Content (Join-Path $Stage "SHA256SUMS.txt") | ForEach-Object { Write-Host "  $_" }
Write-Host ""
Write-Host "Upload this ZIP to GitHub Releases."
