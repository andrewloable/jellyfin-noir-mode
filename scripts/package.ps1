$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$pluginOut = Join-Path $artifacts 'plugin-base'
$wrapperOut = Join-Path $artifacts 'wrapper'
$stagingOut = Join-Path $artifacts 'staging'

Remove-Item -LiteralPath $artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $pluginOut | Out-Null
New-Item -ItemType Directory -Path $wrapperOut | Out-Null
New-Item -ItemType Directory -Path $stagingOut | Out-Null

dotnet publish (Join-Path $root 'src/Jellyfin.Plugin.NoirMode/Jellyfin.Plugin.NoirMode.csproj') -c Release -o $pluginOut
foreach ($runtime in @('win-x64', 'linux-x64', 'osx-x64', 'osx-arm64')) {
    dotnet publish (Join-Path $root 'src/Jellyfin.Plugin.NoirMode.Wrapper/Jellyfin.Plugin.NoirMode.Wrapper.csproj') -c Release -r $runtime --self-contained false -o (Join-Path $wrapperOut $runtime)
}

Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $pluginOut
Copy-Item -LiteralPath (Join-Path $root 'build.yaml') -Destination $artifacts

$pluginZips = @()

function New-PluginPackage {
    param(
        [string] $Name,
        [string[]] $RuntimeIds
    )

    $packageOut = Join-Path $stagingOut $Name
    $bundledWrapperOut = Join-Path $packageOut 'wrapper'
    New-Item -ItemType Directory -Path $packageOut | Out-Null
    New-Item -ItemType Directory -Path $bundledWrapperOut | Out-Null

    Copy-Item -Path (Join-Path $pluginOut '*') -Destination $packageOut -Recurse
    foreach ($runtimeId in $RuntimeIds) {
        Copy-Item -LiteralPath (Join-Path $wrapperOut $runtimeId) -Destination $bundledWrapperOut -Recurse
    }

    $zipPath = Join-Path $artifacts "$Name.zip"
    Compress-Archive -Path (Join-Path $packageOut '*') -DestinationPath $zipPath -Force
    return $zipPath
}

$pluginZips += New-PluginPackage -Name 'jellyfin-plugin-noir-mode-windows-x64-0.1.0' -RuntimeIds @('win-x64')
$pluginZips += New-PluginPackage -Name 'jellyfin-plugin-noir-mode-linux-x64-0.1.0' -RuntimeIds @('linux-x64')
$pluginZips += New-PluginPackage -Name 'jellyfin-plugin-noir-mode-macos-0.1.0' -RuntimeIds @('osx-x64', 'osx-arm64')

$checksumPath = Join-Path $artifacts 'checksums.txt'
Remove-Item -LiteralPath $checksumPath -Force -ErrorAction SilentlyContinue
foreach ($asset in $pluginZips) {
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($asset)
    try {
        $hashBytes = $sha256.ComputeHash($stream)
    }
    finally {
        $stream.Dispose()
        $sha256.Dispose()
    }

    $hash = [System.BitConverter]::ToString($hashBytes).Replace('-', '').ToLowerInvariant()
    Add-Content -LiteralPath $checksumPath -Value "$hash  $(Split-Path -Leaf $asset)"
}

Write-Host "Artifacts written to $artifacts"
