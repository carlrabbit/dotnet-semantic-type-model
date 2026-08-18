. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
if ($args.Count -ne 1) { throw 'Usage: ./eng/release-check.ps1 <version>' }
$version = $args[0]
& (Join-Path $PSScriptRoot 'check.ps1')
dotnet build --configuration Release
& (Join-Path $PSScriptRoot 'package.ps1') $version
& (Join-Path $PSScriptRoot 'package-smoke.ps1') $version
& (Join-Path $PSScriptRoot 'samples.ps1')
& (Join-Path $PSScriptRoot 'public-docs.ps1')
Write-Host "Release check passed for version $version."
