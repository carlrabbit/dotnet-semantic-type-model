. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
dotnet restore
$repoRoot = Split-Path $PSScriptRoot -Parent
if (Test-Path (Join-Path $repoRoot 'package.json')) {
    Require-Command bun
    bun install --frozen-lockfile
}
