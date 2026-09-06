# V26 publisher protected-main mutation-boundary qualification

Scope: repository-safe V26 cloud publisher transaction admission. This runbook does not claim licensed BricsCAD runtime or a live publication PASS.

## Defect boundary

The release workflow has a final protected-main drift fence before candidate admission, and `publish-v26-release.ps1` has a final drift fence before the draft-to-published PATCH. Before this package, however, the publisher could create/reuse a release tag, create a draft release, and upload held assets without an equivalent protected-main revalidation immediately before those mutation phases. Protected main could therefore advance in release-relevant paths after workflow admission while the transaction accumulated remote side effects that would only be rejected later.

## Required transaction fences

The publisher must independently re-read protected `main` from the authenticated GitHub API, validate and fetch that exact SHA, prove `GITHUB_SHA` remains its ancestor, and classify release-relevant drift fail-closed. The release-relevant classifier must cover the same product/build/release surfaces used by final publication admission. A second API read must prove `main` did not move during each admission check.

Invoke this admission immediately before mutation phases that can create externally visible transaction state: before tag creation/reuse advances to a tag mutation, before draft-release creation, and before held asset upload. Existing final publication admission before the PATCH remains authoritative and must not be weakened.

Documentation-only/non-release drift may remain admissible when ancestry holds and the scoped diff is clean. Any release-relevant drift, malformed API SHA, API/fetch identity mismatch, ancestry failure, git classifier error, or movement between the two API reads fails closed.

## Deterministic qualification

Run:

```text
python scripts/preflight-v26-publisher-final-main-stability.py
```

The focused guard must reject mutation probes that remove the authenticated main read, remove exact API/fetch binding, remove ancestry or release-relevant diff classification, change git error handling to fail-open, remove the second main confirmation, or move the first transaction admission until after the first remote POST.

Hosted Shared CI must then report fresh exact-head `preflight` and `core` terminal `SUCCESS`. No hosted result is licensed runtime evidence.

## Merge boundary

Before protected merge, refresh exact `main`, collision-scan the reserved paths, reconcile the canonical branch non-force when strict freshness requires it, and merge only the exact verified head through the protected PR path.