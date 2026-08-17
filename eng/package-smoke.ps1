$ErrorActionPreference = 'Stop'
if ($args.Count -ne 1) { throw 'Usage: ./eng/package-smoke.ps1 <version>' }
dotnet run --project (Join-Path $PSScriptRoot 'Engineering.Commands/Engineering.Commands.csproj') -- package-smoke $args[0]
