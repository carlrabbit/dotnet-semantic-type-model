. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
dotnet build --no-restore
