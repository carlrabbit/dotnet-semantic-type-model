$ErrorActionPreference = 'Stop'
dotnet run --project (Join-Path $PSScriptRoot 'Engineering.Commands/Engineering.Commands.csproj') -- check-affected @args
