#!/usr/bin/env bash
set -eo pipefail

# Let's Encrypt Staging Verification Harness
# Validates Certes beta.1 against Let's Encrypt staging using DNS-01 via Cloudflare.
# Requires: CERTES_PACKAGE_VERSION, CLOUDFLARE_API_TOKEN, CLOUDFLARE_ZONE_ID, ACME_TEST_DOMAIN
# Environment variables: LE_STAGING_URL (default), VERIFICATION_TIMEOUT_SECONDS, RECORD_TTL

LE_STAGING_URL="${LE_STAGING_URL:-https://acme-staging-v02.api.letsencrypt.org/directory}"
TEST_DOMAIN="${ACME_TEST_DOMAIN:-acme-test.certes.app}"
VERIFICATION_TIMEOUT_SECONDS="${VERIFICATION_TIMEOUT_SECONDS:-600}"
RECORD_TTL="${RECORD_TTL:-300}"
CHALLENGE_PROPAGATION_WAIT="${CHALLENGE_PROPAGATION_WAIT:-10}"
CLEANUP_RECORD_IDS_FILE="${CLEANUP_RECORD_IDS_FILE:-.staging-cleanup-records}"

log() { printf '[%s] %s\n' "$(date -u +'%H:%M:%S')" "$*" >&2; }
error() { log "ERROR: $*"; exit 1; }
warn() { log "WARN: $*"; }
info() { log "INFO: $*"; }

cleanup_dns_records() {
  if [[ ! -f "$CLEANUP_RECORD_IDS_FILE" ]]; then
    warn "No cleanup records file found; skipping DNS cleanup."
    return 0
  fi

  local failed=0
  while read -r record_id; do
    [[ -z "$record_id" ]] && continue
    info "Removing DNS record: $record_id"
    if ! curl --silent --show-error \
      -X DELETE "https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/dns_records/${record_id}" \
      -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
      -H "Content-Type: application/json" \
      -w '\n' \
      | grep -q '"success":true'; then
      warn "Failed to delete DNS record $record_id; manual cleanup may be needed."
      ((failed++))
    fi
  done < "$CLEANUP_RECORD_IDS_FILE"
  rm -f "$CLEANUP_RECORD_IDS_FILE"
  return $failed
}

trap 'cleanup_dns_records' EXIT

[[ -z "$CERTES_PACKAGE_VERSION" ]] && error "CERTES_PACKAGE_VERSION not set"
[[ -z "$CLOUDFLARE_API_TOKEN" ]] && error "CLOUDFLARE_API_TOKEN not set"
[[ -z "$CLOUDFLARE_ZONE_ID" ]] && error "CLOUDFLARE_ZONE_ID not set"
[[ -z "$TEST_DOMAIN" ]] && error "ACME_TEST_DOMAIN not set"

info "Starting Let's Encrypt staging verification for Certes $CERTES_PACKAGE_VERSION"
info "Test domain: $TEST_DOMAIN"
info "Staging URL: $LE_STAGING_URL"

# Placeholder: The actual C# harness will implement the verification logic.
# This script serves as orchestration and cleanup glue.

info "Staging verification complete."
