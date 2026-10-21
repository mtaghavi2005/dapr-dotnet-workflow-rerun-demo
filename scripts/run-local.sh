#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$root/.local"
echo "Preparing demo..."
if ! "$root/scripts/build.sh" >"$root/.local/build.log" 2>&1; then
  cat "$root/.local/build.log" >&2
  exit 1
fi
cd "$root"
unset DAPR_API_TOKEN
export DAPR_GRPC_ENDPOINT=http://127.0.0.1:50801
export DAPR_HTTP_ENDPOINT=http://127.0.0.1:3580

# Run the console directly in the foreground so its Enter-key prompts work.
dapr run --app-id workflow-rerun-demo --dapr-http-port 3580 --dapr-grpc-port 50801 --log-level error </dev/null >"$root/.local/dapr.log" 2>&1 &
sidecar_pid=$!
cleanup() {
  kill -TERM "$sidecar_pid" 2>/dev/null || true
  wait "$sidecar_pid" 2>/dev/null || true
}
trap cleanup EXIT
ready=false
for attempt in {1..60}; do
  if ! kill -0 "$sidecar_pid" 2>/dev/null; then
    echo "Local Dapr failed to start:" >&2
    tail -n 20 "$root/.local/dapr.log" >&2
    exit 1
  fi
  if curl -fsS --max-time 1 "$DAPR_HTTP_ENDPOINT/v1.0/healthz/outbound" >/dev/null 2>&1; then
    ready=true
    break
  fi
  sleep 0.5
done
if [[ "$ready" != true ]]; then
  echo "Local Dapr did not become ready:" >&2
  tail -n 20 "$root/.local/dapr.log" >&2
  exit 1
fi
dotnet "$root/src/WorkflowRerunDemo/bin/Debug/net10.0/WorkflowRerunDemo.dll" "$@"
