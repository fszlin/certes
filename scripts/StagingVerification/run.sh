#!/usr/bin/env bash
set -euo pipefail

# Run from the repository root. CERTES_PACKAGE_VERSION selects a published package;
# ACME_TEST_DOMAIN must be a domain in CLOUDFLARE_ZONE_ID delegated to Cloudflare.
: "${CERTES_PACKAGE_VERSION:?Set the published Certes package version (e.g. 5.0.0-beta.1)}"
: "${CLOUDFLARE_API_TOKEN:?Set a Cloudflare token with DNS edit permission}"
: "${CLOUDFLARE_ZONE_ID:?Set the Cloudflare zone ID}"
: "${ACME_TEST_DOMAIN:?Set the staging test domain}"

dotnet run --project scripts/StagingVerification/StagingVerification.csproj -c Release
