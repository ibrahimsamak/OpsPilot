#!/usr/bin/env bash

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
PROJ="src/OpsPilot.Orchestrator"
OUT="web/opspilot-web/src/app/dev-tokens.ts"

mint() {
  local name="$1"; shift
  local args=(--project "$PROJ" --name "$name" --audience opspilot -o token)
  local r; for r in "$@"; do args+=(--role "$r"); done
  dotnet user-jwts create "${args[@]}" 2>/dev/null | tail -1
}

echo "==> Minting dev JWTs..."
ALICE=$(mint alice ops.viewer)
BOB=$(mint bob ops.viewer ops.operator)
CAROL=$(mint carol ops.admin)
MALLORY=$(mint mallory)

for t in ALICE BOB CAROL MALLORY; do
  case "${!t}" in eyJ*) ;; *) echo "!! failed to mint $t token" >&2; exit 1;; esac
done

cat > "$OUT" <<EOF
export interface DevUser {
  name: string;
  label: string;
  token: string;
}

export const DEV_USERS: DevUser[] = [
  { name: 'alice', label: 'alice (ops.viewer)', token: '${ALICE}' },
  { name: 'bob', label: 'bob (ops.viewer + ops.operator)', token: '${BOB}' },
  { name: 'carol', label: 'carol (ops.admin)', token: '${CAROL}' },
  { name: 'mallory', label: 'mallory (no roles)', token: '${MALLORY}' },
];
EOF

echo "==> Wrote $OUT"
