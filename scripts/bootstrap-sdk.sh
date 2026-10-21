#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
source_url="https://github.com/mtaghavi2005/dotnet-sdk.git"
source_commit="f1631362833faf2b2423c3fc1a9aa1b675b46d44"
sdk_dir="${DAPR_SDK_ROOT:-$root/.sdk/dotnet-sdk}"
if [[ ! -e "$sdk_dir/.git" ]]; then
  mkdir -p "$(dirname "$sdk_dir")"
  git clone --no-checkout "$source_url" "$sdk_dir"
else
  if [[ -n "$(git -C "$sdk_dir" status --porcelain)" ]]; then
    echo "SDK checkout has changes; refusing to modify it: $sdk_dir" >&2
    exit 1
  fi
fi
if ! git -C "$sdk_dir" cat-file -e "$source_commit^{commit}" 2>/dev/null; then
  git -C "$sdk_dir" fetch --no-tags "$source_url" "$source_commit"
fi
if [[ "$(git -C "$sdk_dir" rev-parse HEAD)" != "$source_commit" ]]; then
  git -C "$sdk_dir" checkout --detach "$source_commit"
fi
printf 'Fork SDK ready at %s: %s\n' "$source_commit" "$sdk_dir"
