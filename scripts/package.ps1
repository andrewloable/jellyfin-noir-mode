$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$pluginPublishOut = Join-Path $artifacts 'plugin-publish'
$pluginOut = Join-Path $artifacts 'plugin-base'
$wrapperOut = Join-Path $artifacts 'wrapper'
$stagingOut = Join-Path $artifacts 'staging'

function Write-Utf8NoBom {
    param(
        [string] $Path,
        [string] $Content
    )

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

Remove-Item -LiteralPath $artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $pluginPublishOut | Out-Null
New-Item -ItemType Directory -Path $pluginOut | Out-Null
New-Item -ItemType Directory -Path $wrapperOut | Out-Null
New-Item -ItemType Directory -Path $stagingOut | Out-Null

dotnet publish (Join-Path $root 'src/Jellyfin.Plugin.NoirMode/Jellyfin.Plugin.NoirMode.csproj') -c Release -o $pluginPublishOut
foreach ($runtime in @('win-x64', 'linux-x64', 'osx-x64', 'osx-arm64')) {
    dotnet publish (Join-Path $root 'src/Jellyfin.Plugin.NoirMode.Wrapper/Jellyfin.Plugin.NoirMode.Wrapper.csproj') -c Release -r $runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o (Join-Path $wrapperOut $runtime)
}

foreach ($fileName in @(
    'Jellyfin.Plugin.NoirMode.dll',
    'Jellyfin.Plugin.NoirMode.pdb',
    'Jellyfin.Plugin.NoirMode.xml',
    'Jellyfin.Plugin.NoirMode.Core.dll',
    'Jellyfin.Plugin.NoirMode.Core.pdb'
)) {
    $source = Join-Path $pluginPublishOut $fileName
    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination $pluginOut
    }
}

Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $pluginOut
Copy-Item -LiteralPath (Join-Path $root 'src/Jellyfin.Plugin.NoirMode/Images/plugin.png') -Destination (Join-Path $pluginOut 'plugin.png')
Copy-Item -LiteralPath (Join-Path $root 'build.yaml') -Destination $artifacts

$releaseBaseUrl = 'https://github.com/andrewloable/jellyfin-noir-mode/releases/download/v0.1.0'
$pluginImageUrl = 'https://raw.githubusercontent.com/andrewloable/jellyfin-noir-mode/main/src/Jellyfin.Plugin.NoirMode/Images/plugin.png'
$packages = @()

$localManifest = [ordered]@{
    category = 'General'
    changelog = 'Initial MVP implementation.'
    description = 'Per-video black-and-white Noir Mode playback using a server-side FFmpeg wrapper.'
    guid = 'f1bb7d16-9084-4e42-94fb-ff4e0f17470b'
    name = 'Noir Mode'
    overview = 'Apply allowlisted noir filters during Jellyfin transcoding for explicitly configured videos.'
    owner = 'andrewloable'
    targetAbi = '10.11.0.0'
    timestamp = '2026-05-28T00:00:00Z'
    version = '0.1.0.0'
    status = 0
    autoUpdate = $true
    imagePath = 'plugin.png'
    assemblies = @(
        'Jellyfin.Plugin.NoirMode.dll',
        'Jellyfin.Plugin.NoirMode.Core.dll'
    )
}
Write-Utf8NoBom -Path (Join-Path $pluginOut 'meta.json') -Content ((ConvertTo-Json -InputObject $localManifest -Depth 4) + [Environment]::NewLine)

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
        $runtimeOut = Join-Path $bundledWrapperOut $runtimeId
        New-Item -ItemType Directory -Path $runtimeOut | Out-Null
        $wrapperName = if ($runtimeId -eq 'win-x64') { 'Jellyfin.Plugin.NoirMode.Wrapper.exe' } else { 'Jellyfin.Plugin.NoirMode.Wrapper' }
        $wrapperSource = Join-Path (Join-Path $wrapperOut $runtimeId) $wrapperName
        Copy-Item -LiteralPath $wrapperSource -Destination $runtimeOut

        if ($runtimeId -eq 'win-x64') {
            Copy-Item -LiteralPath $wrapperSource -Destination (Join-Path $runtimeOut 'ffmpeg.exe')
            Copy-Item -LiteralPath $wrapperSource -Destination (Join-Path $runtimeOut 'ffprobe.exe')
            Copy-Item -LiteralPath $wrapperSource -Destination (Join-Path $runtimeOut 'ffprobe.Wrapper.exe')
        }
        else {
            Copy-Item -LiteralPath $wrapperSource -Destination (Join-Path $runtimeOut 'ffmpeg')
            Copy-Item -LiteralPath $wrapperSource -Destination (Join-Path $runtimeOut 'ffprobe')
            Copy-Item -LiteralPath $wrapperSource -Destination (Join-Path $runtimeOut 'ffprobe.Wrapper')
        }
    }

    $zipPath = Join-Path $artifacts "$Name.zip"
    Compress-Archive -Path (Join-Path $packageOut '*') -DestinationPath $zipPath -Force
    return $zipPath
}

function Get-FileHashHex {
    param(
        [string] $Path,
        [System.Security.Cryptography.HashAlgorithm] $Algorithm
    )

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $hashBytes = $Algorithm.ComputeHash($stream)
    }
    finally {
        $stream.Dispose()
        $Algorithm.Dispose()
    }

    return [System.BitConverter]::ToString($hashBytes).Replace('-', '').ToLowerInvariant()
}

function New-RepositoryManifest {
    param(
        [string] $Path,
        [string] $SourceUrl,
        [string] $Checksum
    )

    $manifest = @(
        [ordered]@{
            guid = 'f1bb7d16-9084-4e42-94fb-ff4e0f17470b'
            name = 'Noir Mode'
            description = 'Per-video black-and-white Noir Mode playback using a server-side FFmpeg wrapper.'
            overview = 'Apply allowlisted noir filters during Jellyfin transcoding for explicitly configured videos.'
            owner = 'andrewloable'
            category = 'General'
            imageUrl = $pluginImageUrl
            versions = @(
                [ordered]@{
                    version = '0.1.0.0'
                    changelog = 'Initial MVP implementation.'
                    targetAbi = '10.11.0.0'
                    sourceUrl = $SourceUrl
                    checksum = $Checksum
                    timestamp = '2026-05-28T00:00:00Z'
                }
            )
        }
    )

    Write-Utf8NoBom -Path $Path -Content ((ConvertTo-Json -InputObject $manifest -Depth 6) + [Environment]::NewLine)
}

$packages += [ordered]@{
    ZipPath = New-PluginPackage -Name 'jellyfin-plugin-noir-mode-windows-x64-0.1.0' -RuntimeIds @('win-x64')
    Manifest = 'manifest-windows-x64.json'
}
$packages += [ordered]@{
    ZipPath = New-PluginPackage -Name 'jellyfin-plugin-noir-mode-linux-x64-0.1.0' -RuntimeIds @('linux-x64')
    Manifest = 'manifest-linux-x64.json'
}
$packages += [ordered]@{
    ZipPath = New-PluginPackage -Name 'jellyfin-plugin-noir-mode-macos-0.1.0' -RuntimeIds @('osx-x64', 'osx-arm64')
    Manifest = 'manifest-macos.json'
}

$checksumPath = Join-Path $artifacts 'checksums.txt'
Remove-Item -LiteralPath $checksumPath -Force -ErrorAction SilentlyContinue
foreach ($package in $packages) {
    $asset = $package.ZipPath
    $assetName = Split-Path -Leaf $asset
    $sha256 = Get-FileHashHex -Path $asset -Algorithm ([System.Security.Cryptography.SHA256]::Create())
    $md5 = Get-FileHashHex -Path $asset -Algorithm ([System.Security.Cryptography.MD5]::Create())

    Add-Content -LiteralPath $checksumPath -Value "$sha256  $assetName"
    New-RepositoryManifest -Path (Join-Path $artifacts $package.Manifest) -SourceUrl "$releaseBaseUrl/$assetName" -Checksum $md5
}
Copy-Item -LiteralPath (Join-Path $artifacts 'manifest-linux-x64.json') -Destination (Join-Path $artifacts 'manifest.json')
foreach ($manifestName in @('manifest-windows-x64.json', 'manifest-linux-x64.json', 'manifest-macos.json', 'manifest.json')) {
    Copy-Item -LiteralPath (Join-Path $artifacts $manifestName) -Destination (Join-Path $root $manifestName)
}

Write-Host "Artifacts written to $artifacts"
