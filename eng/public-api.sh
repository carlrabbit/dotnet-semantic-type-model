#!/usr/bin/env sh
set -eu

required_files="
public-docs/api/public-api.md
public-docs/api/compatibility.md
"

for file in $required_files; do
  if [ ! -f "$file" ]; then
    echo "Missing public API baseline document: $file" >&2
    exit 1
  fi

  if [ ! -s "$file" ]; then
    echo "Public API baseline document is empty: $file" >&2
    exit 1
  fi
done

echo "Public API baseline documentation check passed."
