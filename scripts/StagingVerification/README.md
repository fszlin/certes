# Let's Encrypt staging verification

This opt-in harness consumes a **published** Certes package and requests certificates
from Let's Encrypt staging using DNS-01. Use a disposable test hostname in a
Cloudflare-managed zone. It creates a new account in memory on every run and
deletes the TXT records it creates when the process exits normally (including
after an error). If the process is killed, inspect `_acme-challenge` records in
the zone and remove leftovers manually. Do not run against a production ACME
directory.

From the repository root with the .NET 10 SDK installed:

```sh
export CERTES_PACKAGE_VERSION=5.0.0-beta.1
export CLOUDFLARE_API_TOKEN=...  # DNS edit permission for the test zone
export CLOUDFLARE_ZONE_ID=...
export ACME_TEST_DOMAIN=acme-test.example.org  # a hostname in that zone
bash scripts/StagingVerification/run.sh
```

Use a fresh NuGet package cache when verifying a release artifact if a local
package of the same version might already be cached (`NUGET_PACKAGES` may point
to a temporary empty directory). The project restores from nuget.org, not the
local `artifacts/packages` feed. The optional `VERIFICATION_TIMEOUT_SECONDS`
(default 600) and `CHALLENGE_PROPAGATION_WAIT` (default 60) control the overall
timeout and initial DNS propagation wait. The process fails if any advertised
feature it exercises fails; a missing advertised feature is reported and skipped.
`LE_STAGING_URL` can be set only to the Let's Encrypt staging directory.

The harness checks default RSA issuance, SANs, PFX key/certificate round-trip,
and, if advertised, an ES256 `shortlived` profile issuance and ARI lookup plus
creation of a replacement order. It does **not** issue the replacement order's
certificate, validate public trust, or test DNS-PERSIST-01. Save the console
output and package version when recording CA interoperability results; a build
alone is not a staging verification.
