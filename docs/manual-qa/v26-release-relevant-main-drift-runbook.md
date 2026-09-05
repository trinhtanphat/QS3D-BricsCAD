# V26 release-relevant main drift: `.gitmodules` binding

## Purpose

The final V26 publisher allows protected `main` to advance past the qualified workflow SHA only when the intervening commits are non-release-relevant. Submodule acquisition metadata in root `.gitmodules` is release-relevant even when the tracked `external/` gitlink SHA does not change.

## Contract

- `scripts/publish-v26-release.ps1` must continue to classify exactly one active `scripts/` literal and exactly one active `external/` literal as release-relevant before the final publish PATCH.
- `scripts/preflight-v26-release-relevant-main-drift.py` contains the reviewed SHA-256 fingerprint of the exact tracked `.gitmodules` Git blob, not checkout-materialized working-tree bytes. This keeps the binding deterministic across Windows/Linux line-ending settings.
- The focused guard fails closed when the tracked `.gitmodules` blob does not match that fingerprint, when either binding path is removed/commented out/duplicated in the final classifier, or when the exact tracked blob cannot be read.
- Existing V26 publication guards remain responsible for workflow-SHA ancestry, protected-main diff semantics, second-main confirmation, and publish ordering. This focused guard does not duplicate those larger contracts.
- Because the fingerprint is checked in inside an auto-discovered file under `scripts/`, every legitimate `.gitmodules` edit must refresh that guard in the same reviewed candidate. A stale V26 workflow SHA will then observe release-relevant `scripts/` drift even if the `external/` gitlink is unchanged.

## Qualification

For a legitimate `.gitmodules` change, compute SHA-256 over the exact tracked Git blob (for example, `git cat-file blob HEAD:.gitmodules`), review the submodule path/URL semantic change, and update `EXPECTED_GITMODULES_SHA256` in `scripts/preflight-v26-release-relevant-main-drift.py` in the same PR. Do not hash a checkout-materialized copy because Windows CRLF conversion can change working-tree bytes without changing the reviewed Git blob.

Run the auto-discovered preflights and the full Shared CI on the exact candidate head. Do not update the digest merely to silence CI: the metadata change and its release impact must be reviewed first.

## Adversarial checks

1. Change the tracked `.gitmodules` blob without refreshing the fingerprint: the focused preflight must fail.
2. Materialize identical tracked `.gitmodules` content with different checkout line endings: the tracked-blob fingerprint must remain stable.
3. Remove or comment out `scripts/` from `$finalReleaseRelevantPaths`: the focused preflight must fail.
4. Duplicate `scripts/` in `$finalReleaseRelevantPaths`: the focused preflight must fail rather than accepting an ambiguous classifier.
5. Preserve the tracked `.gitmodules` blob and exactly one active `scripts/` plus `external/` classifier entry: the focused guard must pass while the broader V26 guards continue to own ancestry, final-main confirmation, and publish-after-confirmation ordering.
