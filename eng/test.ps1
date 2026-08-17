. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
dotnet test --no-build --configuration Debug --treenode-filter '/**[(TestCategory!=Slow)&(TestCategory!=E2E)]'
