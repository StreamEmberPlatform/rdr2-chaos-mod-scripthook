#Requires -Version 5.1
<#
.SYNOPSIS
    StreamEmber Chaos Mod (RDR2): build, package and (optionally) install.

.DESCRIPTION
    1. Version: VERSION (major.minor) + commits since it changed = patch (tools/StreamEmber.Build.psm1).
    2. Compile references (never shipped): StreamEmber.Scripting.RDR2.dll (StreamEmber Runtime) and
       StreamEmber.Overlay.Bridge.dll (StreamEmber Overlay). Source, in order: -ReferenceDir (CI: the latest releases),
       the sibling builds ..\rdr2-runtime-scripthook\bin\Release and ..\ui-runtime\build\managed.
    3. dotnet build src\StreamEmber.ChaosMod.RDR2.csproj. No .pdb / .xml. Needs StreamEmber Runtime (RDR2) 1.1 or newer.
    4. dist\RDR2\ = the game-folder layout, then artifacts\StreamEmber.ChaosMod.RDR2-<version>.zip (+ .sha256):
         StreamEmber\Scripts\StreamEmber.ChaosMod.RDR2.dll
         StreamEmber\Config\ChaosMod.ini
         StreamEmber\UI\ChaosMod\index.html, chaos.css, chaos.js, MHUD-LICENSE.txt   (the menu page, from web\)
         StreamEmber\Manifests\StreamEmber.ChaosMod.RDR2.json
       The chaos mod opens the page from the game folder; only the MHud kit (css/js) comes from the jsDelivr CDN.
    5. -Deploy: copies dist\RDR2 into the game folder (keeps ChaosMod.ini).

.EXAMPLE
    .\build.ps1
.EXAMPLE
    .\build.ps1 -Deploy -GamePath "D:\SteamLibrary\steamapps\common\Red Dead Redemption 2"
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',
    # Explicit product version (CI passes the computed one); default: computed, with a -dev suffix
    [string]$Version = '',
    # Folder with StreamEmber.Scripting.RDR2.dll and StreamEmber.Overlay.Bridge.dll
    [string]$ReferenceDir = '',
    [switch]$Deploy,
    # Game folder (RDR2.exe). Default: RDR2_GAME_PATH environment variable
    [string]$GamePath = '',
    # Overwrite the game's ChaosMod.ini with the template
    [switch]$ResetConfig
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$Root = $PSScriptRoot
$Platform = Split-Path $Root -Parent   # StreamEmberPlatform\ (sibling repositories)
Import-Module (Join-Path $Root 'tools\StreamEmber.Build.psm1') -Force

$Id = 'StreamEmber.ChaosMod.RDR2'
$Game = 'RDR2'
$Preserve = @('StreamEmber/Config/ChaosMod.ini')
$Conflicts = @()

function Find-Reference([string]$File, [string]$Sibling) {
    $candidates = @()
    if ($ReferenceDir) { $candidates += Join-Path $ReferenceDir $File }
    $candidates += Join-Path $Platform (Join-Path $Sibling $File)
    foreach ($c in $candidates) { if (Test-Path $c) { return (Resolve-Path $c).Path } }
    throw "$File not found (looked in: $($candidates -join '; ')). Pass -ReferenceDir or build the sibling repository."
}

if (-not $Version) { $Version = Get-SEVersion -RepositoryRoot $Root -Kind Dev }
Write-Host "StreamEmber Chaos Mod (RDR2) $Version" -ForegroundColor Cyan

# --- Build ------------------------------------------------------------------------------------------------------
$scripting = Find-Reference "StreamEmber.Scripting.$Game.dll" 'rdr2-runtime-scripthook\bin\Release'
$bridge = Find-Reference 'StreamEmber.Overlay.Bridge.dll' 'ui-runtime\build\managed'
$runtimeVersion = (Get-Item $scripting).VersionInfo.ProductVersion
$overlayVersion = (Get-Item $bridge).VersionInfo.ProductVersion
Write-Host "Against StreamEmber.Scripting.$Game $runtimeVersion, StreamEmber.Overlay.Bridge $overlayVersion"
# The effects use the 1.1 API (ScriptStruct, Ped.GetNearbyPeds(max), mounts, outfits, weather, story-safe model change)
$runtimeCore = [regex]::Match([string]$runtimeVersion, '^\d+\.\d+').Value
if ($runtimeCore -and ([version]$runtimeCore -lt [version]'1.1')) {
    throw "StreamEmber.Scripting.$Game $runtimeVersion is too old: the chaos mod needs StreamEmber Runtime (RDR2) 1.1 or newer."
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { throw '.NET SDK not found (dotnet).' }
$out = Join-Path $Root 'build'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
& $dotnet.Source build (Join-Path $Root "src\$Id.csproj") -c $Configuration -o $out --nologo -v minimal `
    "-p:SE_VERSION=$Version" "-p:ScriptingReference=$scripting" "-p:BridgeReference=$bridge" | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)." }

# --- Stage (game-folder layout) ---------------------------------------------------------------------------------
$stage = Join-Path $Root "dist\$Game"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
$scriptsDir = Join-Path $stage 'StreamEmber\Scripts'
$configDir = Join-Path $stage 'StreamEmber\Config'
$uiDir = Join-Path $stage 'StreamEmber\UI\ChaosMod'
New-Item -ItemType Directory -Force -Path $scriptsDir, $configDir, $uiDir | Out-Null
Copy-Item (Join-Path $out "$Id.dll") $scriptsDir
Copy-Item (Join-Path $Root 'package\Config\ChaosMod.ini') $configDir
foreach ($f in 'index.html', 'chaos.css', 'chaos.js', 'MHUD-LICENSE.txt') { Copy-Item (Join-Path $Root "web\$f") $uiDir }

New-SEManifest -StageDirectory $stage -Id $Id -Name 'StreamEmber Chaos Mod (RDR2)' -Version $Version -Game $Game `
    -Preserve $Preserve -Conflicts $Conflicts -RepositoryRoot $Root `
    -Requires @([ordered]@{ file = 'ScriptHookRDR2.dll'; name = 'Script Hook RDR2 (Alexander Blade)'; url = 'http://www.dev-c.com/rdr2/scripthookrdr2/' }) `
    -Depends @([ordered]@{ id = "StreamEmber.Runtime.$Game"; builtAgainst = $runtimeVersion },
               [ordered]@{ id = "StreamEmber.Overlay.$Game"; builtAgainst = $overlayVersion }) | Out-Null

$zip = New-SEPackage -StageDirectory $stage -OutputDirectory (Join-Path $Root 'artifacts') -Id $Id -Version $Version
Write-Host "Package: $zip" -ForegroundColor Green

# --- Install ----------------------------------------------------------------------------------------------------
if ($Deploy) {
    $gameDir = if ($GamePath) { $GamePath } else { $env:RDR2_GAME_PATH }
    if (-not $gameDir) { throw 'Game folder unknown: pass -GamePath or set RDR2_GAME_PATH.' }
    Install-SEPackage -StageDirectory $stage -GameDirectory $gameDir -GameExecutable 'RDR2.exe' -ProcessName 'RDR2' `
        -Preserve $Preserve -Conflicts $Conflicts -ResetConfig:$ResetConfig
    foreach ($need in "StreamEmber.Runtime.$Game.asi", "StreamEmber.Overlay.$Game.asi") {
        if (-not (Test-Path (Join-Path $gameDir $need))) { Write-Warning "$need missing in the game folder: the chaos mod needs it." }
    }
    Write-Host "Installed into $gameDir" -ForegroundColor Green
}
