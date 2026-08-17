. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
if ($args.Count -ne 1) { throw 'Usage: ./eng/test-project.ps1 <test-project.csproj>' }
$project = $args[0]
if (Test-Path $project -PathType Container) {
    $projects = @(Get-ChildItem $project -Filter '*.csproj' -File)
    if ($projects.Count -ne 1) { throw "Expected exactly one test project in directory: $project" }
    $project = $projects[0].FullName
}
if (-not (Test-Path $project -PathType Leaf)) { throw "Test project not found: $project" }
dotnet test $project --configuration Debug --treenode-filter '/**[(TestCategory!=Slow)&(TestCategory!=E2E)]'
