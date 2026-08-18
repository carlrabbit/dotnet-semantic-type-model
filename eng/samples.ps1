. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
$repoRoot = Split-Path $PSScriptRoot -Parent
$packageDir = Join-Path $repoRoot 'artifacts/nuget'
if (-not (Test-Path $packageDir -PathType Container)) { throw "Sample validation requires local SemanticTypeModel packages in $packageDir. Run ./eng/package.ps1 <version> before ./eng/samples.ps1." }
$packageIds = @(Get-PackageIds | ForEach-Object Trim)
$versions = foreach ($id in $packageIds) {
    $matches = Get-ChildItem $packageDir -Filter "$id.*.nupkg" -File | Where-Object Name -NotLike '*.snupkg' | ForEach-Object { $_.BaseName.Substring($id.Length + 1) } | Select-Object -Unique
    if (-not $matches) { throw "Missing local package for sample validation: $id" }
    $matches
}
$version = $versions | Group-Object | Where-Object Count -eq $packageIds.Count | Sort-Object Name | Select-Object -Last 1 -ExpandProperty Name
if (-not $version) { throw "Could not find one SemanticTypeModel package version shared by all packages in $packageDir." }
$projects = @('samples/code-first-json-schema/code-first-json-schema.csproj','samples/code-first-ef-core/code-first-ef-core.csproj','samples/code-first-powerbi/code-first-powerbi.csproj','samples/system-text-json-resolver/system-text-json-resolver.csproj','samples/runtime-di/runtime-di.csproj')
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempRoot | Out-Null
try {
    $env:NUGET_PACKAGES = Join-Path $tempRoot 'packages'
    foreach ($project in $projects) {
        dotnet restore (Join-Path $repoRoot $project) --no-cache --force-evaluate "-p:SemanticTypeModelSamplePackageVersion=$version" | Out-Null
        dotnet run --no-restore --project (Join-Path $repoRoot $project) --configuration Debug "-p:SemanticTypeModelSamplePackageVersion=$version"
    }
} finally { Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue }
Write-Host "Package-based sample validation passed for SemanticTypeModel package version $version."
