#!/usr/bin/env bash
set -euo pipefail

# Run from the repository root. Bound startup waiting independently of test polling.
for ((attempt = 0; attempt < 30; attempt++)); do
  if curl --silent --fail --max-time 2 --noproxy '*' \
      --cacert scripts/Pebble/localhost.pem https://localhost:14000/dir > /dev/null &&
    curl --silent --fail --max-time 2 --noproxy '*' \
      --cacert scripts/Pebble/localhost.pem https://localhost:15000/roots/0 > /dev/null &&
    curl --silent --fail --max-time 2 --noproxy '*' \
      -H 'Content-Type: application/json' -d '{"host":"readiness.example.test"}' \
      http://localhost:8055/http-request-history > /dev/null; then
    exit 0
  fi
  sleep 1
done

printf '%s\n' 'Pebble did not become ready; inspect docker compose -f scripts/Pebble/compose.yml logs.' >&2
exit 1
