param(
    [string]$Configuration = "Debug",
    [string]$SteamLibraryPath = "",
    [string]$Sts2Dir = "",
    [switch]$SkipModCopy
)

$ErrorActionPreference = "Stop"

$appId = "2868840"
$gameName = "Slay the Spire 2"
$steamappsCandidates = New-Object System.Collections.Generic.List[string]

function Add-SteamApps {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $normalized = $Path.Replace('\', [System.IO.Path]::DirectorySeparatorChar).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    if ([System.IO.Path]::GetFileName($normalized.TrimEnd([System.IO.Path]::DirectorySeparatorChar)) -ine "steamapps") {
        $normalized = Join-Path $normalized "steamapps"
    }

    if (-not (Test-Path -LiteralPath $normalized -PathType Container)) {
        return
    }

    $fullPath = [System.IO.Path]::GetFullPath($normalized)
    if (-not $steamappsCandidates.Contains($fullPath)) {
        $steamappsCandidates.Add($fullPath)
    }
}

function Add-SteamRoot {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    Add-SteamApps $Path

    $libraryFolders = Join-Path $Path "steamapps/libraryfolders.vdf"
    if (-not (Test-Path -LiteralPath $libraryFolders -PathType Leaf)) {
        return
    }

    foreach ($line in Get-Content -LiteralPath $libraryFolders) {
        if ($line -match '"path"\s*"([^"]+)"') {
            Add-SteamApps $Matches[1].Replace('\\', '\')
        }
    }
}

function Get-InstallDir {
    param([string]$ManifestPath)

    foreach ($line in Get-Content -LiteralPath $ManifestPath) {
        if ($line -match '"installdir"\s*"([^"]+)"') {
            return $Matches[1]
        }
    }

    return $gameName
}

function Find-Sts2Dir {
    if (-not [string]::IsNullOrWhiteSpace($Sts2Dir)) {
        return [System.IO.Path]::GetFullPath($Sts2Dir)
    }

    Add-SteamApps $SteamLibraryPath
    Add-SteamRoot $env:STEAM_DIR
    Add-SteamRoot $env:STEAM_HOME
    Add-SteamRoot (Join-Path $HOME ".local/share/Steam")
    Add-SteamRoot (Join-Path $HOME ".steam/steam")
    Add-SteamRoot (Join-Path ${env:ProgramFiles(x86)} "Steam")
    Add-SteamRoot (Join-Path $env:ProgramFiles "Steam")

    foreach ($drive in Get-PSDrive -PSProvider FileSystem) {
        Add-SteamRoot (Join-Path $drive.Root "Steam")
        Add-SteamApps (Join-Path $drive.Root "SteamLibrary/steamapps")
    }

    foreach ($steamapps in $steamappsCandidates) {
        $manifest = Join-Path $steamapps "appmanifest_$appId.acf"
        if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
            continue
        }

        $installDir = Get-InstallDir $manifest
        $candidate = Join-Path (Join-Path $steamapps "common") $installDir
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    foreach ($steamapps in $steamappsCandidates) {
        $candidate = Join-Path (Join-Path $steamapps "common") $gameName
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    throw "Could not find $gameName. Pass -Sts2Dir or -SteamLibraryPath."
}

$detectedSts2Dir = Find-Sts2Dir
Write-Host "Detected $gameName at $detectedSts2Dir"

$project = Join-Path $PSScriptRoot "CardOrderRecord.csproj"
$buildArgs = @("build", $project, "-c", $Configuration, "/p:Sts2Dir=$detectedSts2Dir")
if ($SkipModCopy) {
    $buildArgs += "/p:SkipModCopy=true"
}

& dotnet @buildArgs
exit $LASTEXITCODE
