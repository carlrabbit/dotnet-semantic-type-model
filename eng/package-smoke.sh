#!/usr/bin/env sh
set -eu

if [ "$#" -ne 1 ]; then
  echo "Usage: ./eng/package-smoke.sh <version>" >&2
  exit 1
fi

exec dotnet run --project "$(dirname "$0")/Engineering.Commands/Engineering.Commands.csproj" -- package-smoke "$1"
