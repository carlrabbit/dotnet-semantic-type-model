. (Join-Path $PSScriptRoot 'common.ps1')
Require-Command dotnet
dotnet run --configuration Release --project (Join-Path $PSScriptRoot '../benchmarks/SemanticTypeModel.Benchmarks')
