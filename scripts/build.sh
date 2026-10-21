#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
mode="${DEMO_BUILD_MODE:-auto}"
case "$mode" in
  auto)
    if [[ "$(uname -s)" == Darwin && "$(uname -m)" == arm64 ]]; then
      mode=container
    else
      mode=native
    fi
    ;;
  native|container) ;;
  *) echo "DEMO_BUILD_MODE must be auto, native, or container." >&2; exit 2 ;;
esac
if [[ "$mode" == container ]]; then
  if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is required for this build. Start Docker Desktop, or use DEMO_BUILD_MODE=native with compatible host protobuf tools." >&2
    exit 1
  fi
  echo "Building in the linux/amd64 .NET SDK container."
  exec docker run --rm --platform linux/amd64 \
    -e DEMO_BUILD_MODE=native \
    -e DEMO_CONTAINER_BUILD=true \
    -v "$root:/work" \
    -v workflow-rerun-demo-nuget:/root/.nuget/packages \
    -w /work \
    mcr.microsoft.com/dotnet/sdk:10.0 \
    bash -c 'git config --global --add safe.directory /work/.sdk/dotnet-sdk; exec ./scripts/build.sh'
fi
"$root/scripts/bootstrap-sdk.sh"
build_args=()
if [[ "${DEMO_CONTAINER_BUILD:-false}" == true ]]; then
  # Apply to every project reference, including the SDK fork.
  build_args+=(-p:BaseIntermediateOutputPath=obj/container/)
fi
dotnet build "$root/src/WorkflowRerunDemo/WorkflowRerunDemo.csproj" -p:DaprSdkRoot="${DAPR_SDK_ROOT:-$root/.sdk/dotnet-sdk}" "${build_args[@]}"
