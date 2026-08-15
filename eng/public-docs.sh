#!/usr/bin/env sh
set -eu
exec dotnet run --project "$(dirname "$0")/Engineering.Commands/Engineering.Commands.csproj" -- public-docs
