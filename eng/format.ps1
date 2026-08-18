. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
dotnet format
if (Test-Path (Join-Path (Split-Path $PSScriptRoot -Parent) 'biome.json')) {
    Require-Command bun
    bun run format
}
