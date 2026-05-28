$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$pluginOut = Join-Path $artifacts 'plugin'
$wrapperOut = Join-Path $artifacts 'wrapper'

Remove-Item -LiteralPath $artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $pluginOut | Out-Null
New-Item -ItemType Directory -Path $wrapperOut | Out-Null

dotnet publish (Join-Path $root 'src/Jellyfin.Plugin.NoirMode/Jellyfin.Plugin.NoirMode.csproj') -c Release -o $pluginOut
dotnet publish (Join-Path $root 'src/Jellyfin.Plugin.NoirMode.Wrapper/Jellyfin.Plugin.NoirMode.Wrapper.csproj') -c Release -r win-x64 --self-contained false -o (Join-Path $wrapperOut 'win-x64')
dotnet publish (Join-Path $root 'src/Jellyfin.Plugin.NoirMode.Wrapper/Jellyfin.Plugin.NoirMode.Wrapper.csproj') -c Release -r linux-x64 --self-contained false -o (Join-Path $wrapperOut 'linux-x64')

Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $pluginOut
Copy-Item -LiteralPath (Join-Path $root 'build.yaml') -Destination $artifacts

Compress-Archive -Path (Join-Path $pluginOut '*') -DestinationPath (Join-Path $artifacts 'jellyfin-plugin-noir-mode-0.1.0.zip') -Force

Write-Host "Artifacts written to $artifacts"
