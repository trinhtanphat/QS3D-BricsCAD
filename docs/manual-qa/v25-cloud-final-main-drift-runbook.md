# V25 cloud final-main drift qualification

Scope: `.github/workflows/release-v25-cloud.yml` final publication admission only. This runbook does not change V25 product behavior or package contents.

## Invariants

1. The verified `SOURCE_SHA` must still be an ancestor of protected `main` at final publication admission.
2. `SOURCE_SHA..finalMain` must contain no release-relevant changes in `src/`, `tests/`, `scripts/`, `external/QS3D-Platform`, `.gitmodules`, `Directory.Build.props`, `QS3D.sln`, or `.github/workflows/release-v25-cloud.yml`.
3. `git diff --quiet` exit `1` is treated as detected release-relevant drift and rejects publication; every other non-zero status is treated as an infrastructure/classification failure and also rejects publication.
4. Protected `main` is read again after drift classification and must still equal the exact classified `finalMain` SHA before the publication PATCH.
5. Draft state, tag/target identity, exact asset IDs, asset sizes, and byte hashes remain independently enforced.

## Deterministic qualification

Run `python scripts/preflight-v25-cloud-final-main-drift.py`. The guard must PASS on the fixed workflow and must fail its built-in mutation probes for classifier removal, unscoped diff, second-main confirmation removal, and fail-open git-error handling.

Review the workflow ordering manually: final draft/asset identity checks -> protected-main API/fetch identity -> `SOURCE_SHA` ancestry -> release-relevant scoped diff -> second exact protected-main API confirmation -> publication PATCH.

## Adversarial scenarios

- Main unchanged from `SOURCE_SHA`: admission may proceed after all existing release checks.
- Main advances only on non-release-relevant documentation outside the classifier: ancestry remains true, scoped diff remains clean, and publication may proceed if the second main read is stable.
- Main advances in any classified release path: publication must remain draft and fail before PATCH.
- `git diff` cannot resolve a revision/path or exits with a status other than `0`/`1`: publication must fail closed rather than interpret the error as clean.
- Main advances after classification but before the second API read: SHA mismatch must reject publication.
- Draft assets or release identity mutate independently: existing asset/release checks must reject publication regardless of main state.

## Platform notes

The release job runs on `windows-latest` under Windows PowerShell. Path arguments are passed as a PowerShell array after Git's `--` separator, so each classifier entry is one pathspec and shell quoting does not collapse the list. No Bash-only syntax is introduced.

## Exit criteria

A carrier is mergeable only after fresh exact-head required CI is terminal GREEN, reservation/path-collision checks are GREEN, the PR is reconciled with current protected `main`, review threads are resolved, and the merge is performed against the verified exact head SHA.