$ErrorActionPreference = 'Stop'
dotnet run --project (Join-Path $PSScriptRoot 'Engineering.Commands/Engineering.Commands.csproj') -- public-docs
