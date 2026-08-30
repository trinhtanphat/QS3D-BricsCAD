# V26 release asset held generations

## Scope

The manual V26 production-release workflow must verify the exact local and downloaded draft-asset generations that it admitted. Pathname identity alone is insufficient because a file may be replaced or mutated between an earlier size/identity observation and a later hash reopen.

## Source-ready acceptance

- `scripts/verify-v26-held-file.ps1` rejects reparse ancestors and final reparse/non-file inputs, binds canonical path + admitted length + UTC write ticks, opens with read-only sharing, immediately rebinds pathname metadata, and hashes/copies through the held stream.
- `.github/workflows/release-v26.yml` computes both local and downloaded asset SHA-256 through that helper; direct `Get-FileHash` reopening of release assets is forbidden.
- Draft creation, exact remote tag/workflow-SHA validation, exact asset-set and size validation, held local/remote hash comparison, a second remote tag/SHA check, and publish-last ordering remain unchanged.
- `scripts/preflight-v26-release-asset-held-generations.py` is auto-discovered and carries deterministic negative probes for pathname hashing, writable sharing, and weakened reparse admission.
- `scripts/preflight-v26-package-release.py` remains the broad release isolation/order guard and must agree with this hardened contract.

## Validation

Run the repository Shared CI on the exact candidate. Require all preflight/source guards, tracked PowerShell syntax, Core deterministic smoke tests, and required V25/V26 source/build checks to pass according to the current repository lifecycle before merge.

## Production/runtime boundary

This work does not dispatch `.github/workflows/release-v26.yml`, sign or timestamp artifacts, publish a GitHub Release, or claim licensed BricsCAD V26 runtime evidence. Stable publication still requires the explicit repository release confirmation, licensed self-hosted V26 runtime, signing credentials, and the qualification boundary tracked separately by #1462.
