# V25 cloud pre-mutation protected-main stability qualification

Scope: REMOTE_SAFE source/workflow qualification for issue #5951. This runbook does not claim a real GitHub release publication, licensed BricsCAD runtime execution, or production release acceptance.

## Contract

Before the first persistent GitHub Release mutation (`POST /releases`), the V25 cloud workflow must bind the current protected `main` through both the authenticated GitHub API and an exact git fetch, prove that `SOURCE_SHA` is still its ancestor, classify `SOURCE_SHA..currentMain` only across the release-relevant path set, fail closed on both relevant drift and git-classification errors, then perform a second authenticated `main` read and require exact SHA stability. The existing final pre-publication fence remains required as defense in depth.

Release-relevant paths are `src/`, `tests/`, `scripts/`, `external/QS3D-Platform`, `.gitmodules`, `Directory.Build.props`, `QS3D.sln`, and `.github/workflows/release-v25-cloud.yml`.

## Deterministic qualification

Run `python scripts/preflight-v25-cloud-pre-mutation-main-stability.py` and `python scripts/preflight-all.py`. The focused guard mutation-probes removal of the initial main API binding, loss of the scoped diff, loss of the second-main confirmation, fail-open handling of git errors, and any release POST moved ahead of the fence.

Adversarial source scenarios to preserve:

- API/fetch SHA mismatch: reject before draft creation.
- `SOURCE_SHA` no longer ancestor of protected main: reject before draft creation.
- Release-relevant main drift: reject before draft creation.
- `git diff` exit 1: classify as relevant drift and reject.
- `git diff` exit other than 0/1: classify as infrastructure/error and reject.
- Protected main changes between classification and the second API read: reject before draft creation.
- Main advances only through paths outside the release-relevant set and remains stable through the second read: pre-mutation admission may continue; the later final fence still revalidates before publication.
- Exact stable main with no relevant drift: source guard should pass.

## Self-review checklist

Confirm exact `SOURCE_SHA`/protected-main ancestry semantics, API/fetch identity equality, dedicated remote ref use, fail-closed `$LASTEXITCODE` handling, path quoting on Windows PowerShell, fence ordering before `POST /releases`, unchanged held-asset SHA-256/size verification, unchanged exact `RELEASE_COMMIT_SHA` and tag binding, unchanged final draft verification, and unchanged final protected-main publication fence.

Do not weaken the classifier, convert failures to warnings, add `continue-on-error`, or claim a real release/runtime PASS from this source-only qualification.
