# V26 release-relevant main drift: `.gitmodules` binding

## Purpose

The final V26 publisher allows protected `main` to advance past the qualified workflow SHA only when the intervening commits are non-release-relevant. Submodule acquisition metadata in root `.gitmodules` is release-relevant even when the tracked `external/` gitlink SHA does not change.

## Contract

- `scripts/publish-v26-release.ps1` must continue to classify `scripts/` and `external/` as release-relevant before the final publish PATCH.
- `scripts/preflight-v26-release-relevant-main-drift.py` contains the reviewed SHA-256 fingerprint of the exact tracked `.gitmodules` bytes.
- The focused guard fails closed when the tracked `.gitmodules` bytes do not match that fingerprint, when `scripts/` leaves the final classifier, or when ancestry/diff/main-confirmation/publish ordering regresses.
- Because the fingerprint is checked in inside an auto-discovered file under `scripts/`, every legitimate `.gitmodules` edit must refresh that guard in the same reviewed candidate. A stale V26 workflow SHA will then observe release-relevant `scripts/` drift even if the `external/` gitlink is unchanged.

## Qualification

For a legitimate `.gitmodules` change, compute SHA-256 over the exact tracked bytes and update `EXPECTED_GITMODULES_SHA256` in `scripts/preflight-v26-release-relevant-main-drift.py` in the same PR. Run the auto-discovered preflights and the full Shared CI on the exact candidate head. Do not update the digest merely to silence CI: review the submodule path/URL semantic change and its release impact first.

## Adversarial checks

1. Change `.gitmodules` without refreshing the fingerprint: the focused preflight must fail.
2. Remove `scripts/` from `$finalReleaseRelevantPaths`: the focused preflight must fail.
3. Preserve unchanged `.gitmodules` bytes and fingerprint: the guard must pass while retaining final workflow-SHA ancestry, protected-main diff, second protected-main confirmation, and publish-after-confirmation ordering.
