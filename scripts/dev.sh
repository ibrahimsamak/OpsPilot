#!/usr/bin/env bash

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
LOGDIR="$ROOT/logs"
mkdir -p "$LOGDIR"

RED=$'\033[31m'; GRN=$'\033[32m'; YLW=$'\033[33m'; BLU=$'\033[34m'; MAG=$'\033[35m'; DIM=$'\033[2m'; RST=$'\033[0m'

say() { printf '%s==>%s %s\n' "$GRN" "$RST" "$*"; }
warn() { printf '%s==>%s %s\n' "$YLW" "$RST" "$*"; }

cleaned=0
cleanup() {
  [ "$cleaned" = 1 ] && return; cleaned=1
  echo
  say "Stopping app processes..."
  pkill -f 'OpsPilot.OpsApi' 2>/dev/null || true
  pkill -f 'OpsPilot.Orchestrator' 2>/dev/null || true
  pkill -f 'ng serve' 2>/dev/null || true
  kill 0 2>/dev/null || true
  wait 2>/dev/null || true
  say "App processes stopped. Infra still running, run '${DIM}make down${RST}' to stop Docker."
}
trap cleanup INT TERM EXIT

command -v docker  >/dev/null || { warn "docker not found"; exit 1; }
command -v dotnet  >/dev/null || { warn "dotnet not found"; exit 1; }
docker info >/dev/null 2>&1    || { warn "Docker isn't running, start Docker Desktop first."; exit 1; }

if ! dotnet user-secrets list --project src/OpsPilot.Orchestrator 2>/dev/null | grep -q '^AzureOpenAI:Endpoint'; then
  warn "AzureOpenAI:Endpoint user-secret is not set for the Orchestrator."
  warn "Set it, e.g.:"
  warn "  dotnet user-secrets set \"AzureOpenAI:Endpoint\" \"https://<resource>.services.ai.azure.com/\" --project src/OpsPilot.Orchestrator"
  exit 1
fi

say "Starting infra (Postgres + Aspire dashboard)..."
docker compose up -d >/dev/null

printf '%s==>%s Waiting for Postgres to be healthy' "$GRN" "$RST"
for _ in $(seq 1 60); do
  status="$(docker inspect -f '{{.State.Health.Status}}' opspilot-postgres 2>/dev/null || echo missing)"
  [ "$status" = healthy ] && { echo " ok"; break; }
  printf '.'; sleep 1
done
[ "${status:-}" = healthy ] || { echo; warn "Postgres did not become healthy in time."; exit 1; }

if [ -s "$HOME/.nvm/nvm.sh" ]; then
  export NVM_DIR="$HOME/.nvm"
  . "$NVM_DIR/nvm.sh"
  nvm use 24.15.0 >/dev/null 2>&1 || nvm use --lts >/dev/null 2>&1 || true
fi

if [ ! -d web/opspilot-web/node_modules ]; then
  say "Installing Angular dependencies (first run)..."
  ( cd web/opspilot-web && npm install )
fi
if [ ! -f web/opspilot-web/src/app/dev-tokens.ts ]; then
  warn "web/opspilot-web/src/app/dev-tokens.ts is missing, run 'make tokens' to generate dev JWTs."
  exit 1
fi

run() {
  local color="$1" tag="$2" log="$3"; shift 3; shift
  ( "$@" 2>&1 | while IFS= read -r line; do printf '%s[%s]%s %s\n' "$color" "$tag" "$RST" "$line"; done | tee "$log" ) &
}

say "Starting OpsApi (:5101), Orchestrator (:5102), Angular (:4200)..."
echo

run "$BLU" "opsapi" "$LOGDIR/opsapi.log" -- \
  dotnet run --project src/OpsPilot.OpsApi

run "$MAG" "orch  " "$LOGDIR/orchestrator.log" -- \
  env AZURE_TOKEN_CREDENTIALS=dev dotnet run --project src/OpsPilot.Orchestrator

run "$YLW" "web   " "$LOGDIR/web.log" -- \
  bash -c 'cd web/opspilot-web && npm start'

echo
say "Open ${BLU}http://localhost:4200${RST} | dashboard: ${DIM}http://localhost:18888${RST}"
say "Ctrl+C to stop the app processes."
echo

wait
