# QS3D V25 updater — bounded final download plan

## Goal

Make the detached final update transfer itself bounded, not merely validate file size after `Invoke-WebRequest` has already written the entire response to disk.

## Current risk

The secure updater already validates manifest schema, selected-release snapshot, signer, product SemVer, package SHA-256, ZIP traversal/expansion limits, installed-state freshness and package identity. However, final manifest/ZIP retrieval still uses `Invoke-WebRequest -OutFile`. A stalled transfer may never reach those checks, while an oversized or endless response can exceed the intended local-disk bound before rejection.

## Implementation

1. Add a single `Invoke-BoundedHttpsDownload` helper in `scripts/update-v25.ps1` using Windows PowerShell/.NET networking primitives available on the supported V25 host.
2. Require the requested URI to be absolute HTTPS and credential-free.
3. Configure explicit request and read/write timeouts, automatic decompression, bounded redirects and a QS3D updater user agent.
4. After response resolution, require the final response URI to remain HTTPS and credential-free.
5. Reject a known positive `ContentLength` larger than the caller-provided maximum before writing response bytes.
6. Copy to a new destination stream in chunks while counting total bytes; throw before writing bytes that would exceed the bound.
7. Delete a partial destination on every failed transfer and dispose request/response/streams deterministically.
8. Use 64 KiB for the final manifest and the existing `MaxPackageSizeMB` value for the ZIP.
9. Retain existing post-download file-size checks, hash verification, archive limits, signer verification and all release-snapshot/product-version guards.

## Regression contract

Add `scripts/preflight-update-bounded-download.py`, automatically discovered by `preflight-all.py`, to require:

- one shared bounded HTTPS helper;
- explicit timeout/read-timeout/redirect limits;
- requested and final HTTPS validation;
- `ContentLength` fast rejection plus streaming byte-count rejection;
- cleanup of partial output on failure;
- manifest cap of 64 KiB;
- package cap derived from `MaxPackageSizeMB`;
- both final transfers routed through the helper and no remaining `Invoke-WebRequest -Uri $manifestAddress... -OutFile` / package equivalent;
- preservation of package SHA-256, archive, signer, release-snapshot and shared-mutex boundaries.

## Validation boundary

This lane provides source/static proof only. Exact Windows PowerShell redirect/timeout behavior, slow/stalled server injection and post-close BricsCAD recovery remain in `LOCAL-009`. No GitHub Actions or release is dispatched by this work.