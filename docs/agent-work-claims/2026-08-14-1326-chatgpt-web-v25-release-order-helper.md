# V25 release ordering one-shot helper repair

Status: SOURCE_FIXED
Owner: chatgpt-web-gpt56sol
Started: 2026-08-14 13:26 +07:00
Closed: 2026-08-14 13:31 +07:00
Baseline main: `35507fe35b2c49fe41400167d0e28b67aab29c65`

## Evidence

- One-shot workflow run `31776101565`, job `94691733963` failed in `Move release preparation before toolchain setup` with `Expected exactly one toolchain setup block, got 0`.
- Root cause: the original helper embedded `${{ ... }}` workflow expressions inside a `run:` script string. GitHub Actions expanded those expressions before Python ran, so the expected literal workflow block could not match.

## Completion evidence

- Concurrent fix `bea74123100975279a71910c6e1a0521ea1e07a0` replaced brittle exact-text matching with bounded step-boundary markers.
- Fresh one-shot run `31776174257`, job `94691941085` proved the transform step succeeds. Its later push failed only because the Actions token was not allowed to update another workflow file without `workflows` permission; no permission boundary was weakened.
- Target workflow ordering was subsequently landed directly on `main` by `cf732f646573e4f5d690f276d63ddec5d35b992a` (`fix(ci): prepare V25 release source before toolchain setup`).
- Read-back confirms `.github/workflows/release-v25-cloud.yml` now runs checkout -> `Validate cloud prerelease request` -> `Prepare exact release source commit` -> setup-python/setup-dotnet/NuGet cache, preserving the existing stale-main/concurrency guard and release semantics.
- No duplicate helper patch was created after the concurrent fix was observed. No force-push or workflow-token permission expansion was used.

## Exclusions preserved

Curtain/#1105/#1106, LOCAL_ONLY/native qualification lanes, `prepare-v25-cloud-release.ps1`, unrelated workflows/source, and release safety semantics were not modified by this claim.

## Qualification boundary

This helper/reordering lane is SOURCE_FIXED. A fresh V25 cloud release dispatch on a current descendant is still required for workflow acceptance; stale #148/#149 runs do not qualify the reordered workflow, and native licensed BricsCAD V25 acceptance remains separate.
