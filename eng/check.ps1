$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'restore.ps1')
& (Join-Path $PSScriptRoot 'build.ps1')
& (Join-Path $PSScriptRoot 'test.ps1')
dotnet format --verify-no-changes
if (Test-Path (Join-Path (Split-Path $PSScriptRoot -Parent) 'biome.json')) { bun run check }
