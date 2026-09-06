# V26 cloud final-main drift qualification

Scope: V26 cloud final publication admission in `.github/workflows/release-v26-cloud.yml`. This runbook does not claim licensed BricsCAD runtime evidence and does not change package contents, source provenance, release tag derivation, or the admitted V26 publisher's asset semantics.

## Required invariants

1. The exact workflow candidate `GITHUB_SHA` must still be an ancestor of protected `main` at final publication admission.
2. `GITHUB_SHA..finalMain` must contain no release-relevant changes in `src/`, `tests/`, `scripts/`, `external/QS3D-Platform`, `.gitmodules`, `Directory.Build.props`, `QS3D.sln`, or `.github/workflows/release-v26-cloud.yml`.
3. `git diff --quiet` exit `1` means release-relevant drift and rejects publication. Any other non-zero exit is an infrastructure/classification failure and also rejects publication.
4. Protected `main` must be fetched again after release-relevant drift classification and must still equal the exact classified `finalMain` SHA before the admitted candidate publisher is invoked.
5. Existing V26 package checksum, provenance, release/package-tag identity, exact source commit, admitted-script identity, and downstream publisher checks remain independently authoritative.

## Deterministic qualification

Run:

```text
python scripts/preflight-v26-cloud-final-main-drift.py
```

The guard must PASS on the fixed workflow and fail its mutation probes for final-main fetch removal, classifier removal, unscoped diff, second-main confirmation removal, and fail-open git-error handling.

Review ordering manually in the release job:

```text
held artifact existence/tag checks
  -> exact protected-main API read
  -> fetch exact finalMain
  -> GITHUB_SHA ancestry
  -> release-relevant scoped diff
  -> second exact protected-main API read/equality
  -> assert-v26-candidate-identity.ps1 admitted publication
```

## Adversarial scenarios

- Protected main is unchanged from `GITHUB_SHA`: final admission may proceed after all existing checks.
- Protected main advances only on non-release-relevant documentation outside the classifier: ancestry remains true and scoped diff remains clean; publication may proceed only if the second main read is stable.
- Protected main advances on any classified release-relevant path: publication must fail before the admitted publisher is invoked.
- Git cannot resolve the final revision/path classification or returns any status other than `0` or `1`: publication fails closed.
- Protected main changes between drift classification and the second API read: publication fails before candidate publication.

## Acceptance boundary

This package is repository-safe release admission work. Hosted Shared CI must report fresh exact-head `preflight` and `core` terminal `SUCCESS`. No hosted/static result is `LOCAL_PASS`, and no licensed BricsCAD runtime claim is required for this source-only release fence.

Before merge, refresh protected main, verify Reservation-v2/path collision remains clean, reconcile the canonical branch non-force if strict freshness requires it, and merge only the exact verified head through the protected PR path.