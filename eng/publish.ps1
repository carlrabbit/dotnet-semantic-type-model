. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
if ($args.Count -ne 1) { throw 'Usage: ./eng/publish.ps1 <version>' }
$version = $args[0]; $packageDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/nuget'
$apiKey = $env:NUGET_API_KEY
if ([string]::IsNullOrWhiteSpace($apiKey)) { throw 'NUGET_API_KEY is required to publish packages.' }
if (-not (Test-Path $packageDir -PathType Container)) { throw "Package directory does not exist: $packageDir" }
$excluded = Join-Path $packageDir "SemanticTypeModel.JsonEditor.$version.nupkg"
if (Test-Path $excluded) { throw 'SemanticTypeModel.JsonEditor is not part of the 1.0 package set and will not be published.' }
Get-PackageIds | ForEach-Object {
    $packageId = $_.Trim(); $packageFile = Join-Path $packageDir "$packageId.$version.nupkg"
    if (-not (Test-Path $packageFile -PathType Leaf)) { throw "Expected package is missing: $packageFile" }
    dotnet nuget push $packageFile --api-key $apiKey --source 'https://api.nuget.org/v3/index.json' --skip-duplicate
}
Write-Host "Publish command completed for version $version."
