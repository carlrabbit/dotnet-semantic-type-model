$ErrorActionPreference = 'Stop'

function Require-Command([string] $Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

function Get-PackageIds {
    dotnet run --project (Join-Path $PSScriptRoot 'Engineering.Commands/Engineering.Commands.csproj') -- package-ids
}

function Get-PackageProjects {
    dotnet run --project (Join-Path $PSScriptRoot 'Engineering.Commands/Engineering.Commands.csproj') -- package-projects
}
