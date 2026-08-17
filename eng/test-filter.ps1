. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
Require-Command rg
if ($args.Count -ne 1) { throw 'Usage: ./eng/test-filter.ps1 <search-term|mtp-treenode-filter>' }
$filter = $args[0]
if ($filter.StartsWith('/')) { dotnet test --no-build --configuration Debug --treenode-filter $filter; exit $LASTEXITCODE }
$matched = $false
Get-ChildItem (Join-Path (Split-Path $PSScriptRoot -Parent) 'tests/unit') -Recurse -Filter '*.csproj' -File | ForEach-Object {
    $project = $_.FullName; $directory = $_.DirectoryName
    $sourceMatch = Get-ChildItem $directory -Recurse -Filter '*.cs' -File | Select-String -SimpleMatch $filter -Quiet
    if ($project.Contains($filter) -or $sourceMatch) { $matched = $true; & (Join-Path $PSScriptRoot 'test-project.ps1') $project }
}
if (-not $matched) { throw "No unit test project or C# test source matched focused test term '$filter'." }
