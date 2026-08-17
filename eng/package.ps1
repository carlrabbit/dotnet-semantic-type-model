. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
if ($args.Count -ne 1) { throw 'Usage: ./eng/package.ps1 <version>' }
$version = $args[0]
$outputDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/nuget'
if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
Get-PackageProjects | ForEach-Object { dotnet pack $_.Trim() --configuration Release --output $outputDir "-p:PackageVersion=$version" }
Write-Host "Packages produced in $outputDir for version $version."
