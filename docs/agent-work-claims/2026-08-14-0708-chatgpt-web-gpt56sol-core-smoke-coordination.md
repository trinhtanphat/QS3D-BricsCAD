# Work Claim — Core smoke coordination and triage

- Agent: `chatgpt-web-gpt56sol`
- Started: `2026-08-14 07:08 +07:00`
- Status: `SOURCE_FIXED / PENDING_FRESH_CI`
- Baseline observed before claim: `main` at `2dc87bf0985c5967f9ca45f09aac22ba85e2e0cd` (must be refreshed after this claim lands)
- Initial claim commit: `3cb24f5d9041209e00704d8270c61218278d8baf`
- Claim-amendment commit: `404e110d954a354541cefaf0c3dddde5e399c0e7`
- Source-fix commit: `d03edf8e4c476ee929d731a2c0c7400a8b8d14e4`

## Scope

1. Record the current multi-agent coordination/blocker state in Markdown so other agents do not duplicate work.
2. Strengthen the claim-first rule for scope expansion: an agent must land and verify a follow-up claim-amendment commit on `origin/main` before touching any newly discovered source/test paths outside its reserved scope.
3. Triage the latest relevant `Run deterministic Core smoke` failure from GitHub Actions, obtain the exact exception/error text, and determine whether a remote-safe unclaimed source/test lane exists.
4. Fix issue `#1092` after the claim amendment below is visible on current `origin/main`, preserving canonical numeric SourceHandle identity and relevant positive/negative smoke coverage.

## Initially reserved paths

- `docs/agent-work-claims/2026-08-14-0708-chatgpt-web-gpt56sol-core-smoke-coordination.md`
- `docs/AGENT-WORK-REGISTRATION.md`
- one new 2026-08-14 coordination/handoff Markdown file under `docs/`

## 2026-08-14 07:xx +07:00 claim amendment — issue #1092

Fresh evidence after the initial claim identified the current CAD-independent blocker handed off by LOCAL-003:

- issue `#1092` is `OPEN`, unassigned and had no repository claim match for `1092` or `ModelHealthSourceHandleSmoke` at the collision check;
- clean exact SHA `2dc87bf0985c5967f9ca45f09aac22ba85e2e0cd` passes Core Release, nine focused Level/static gates and installed-reference V25 build, then full Core smoke fails in `ModelHealthSourceHandleSmoke.NumericAliasesShareIdentity`;
- numeric aliases `A` / `00a` produce zero `DUPLICATE_SOURCE_HANDLE` issues instead of one;
- the local owner explicitly handed this CAD-independent defect to a non-local owner and moved LOCAL-003 to `BLOCKED` without editing Core/tests.

After this amendment commit lands and is read back from current `main`, this claim additionally reserves:

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthSourceHandleSmoke.cs`
- issue `#1092` closeout/status updates tied only to this fix

The amendment does **not** reserve LOCAL-003 native BricsCAD qualification or any other source lane. If the fix proves to require another source/test path, stop and land another claim amendment before reading/editing/testing that new path.

## 2026-08-14 07:17 +07:00 source fix

- `d03edf8e4c476ee929d731a2c0c7400a8b8d14e4` updates `ModelHealthService` so persisted and live source handles use the existing `GeneratedHandleIdentity.Normalize` identity contract.
- Numeric aliases such as `A`, `00a`, and `0xA` now canonicalize to one SourceHandle identity for intra-element duplicate detection, cross-element ownership detection, and liveness matching.
- Malformed textual handles retain the existing trimmed, case-insensitive compatibility path because `GeneratedHandleIdentity.Normalize` falls back to the trimmed text when hexadecimal parsing is not applicable.
- `liveGeneratedSolidHandles` intentionally remains on the pre-existing normalization path; this source-handle fix does not broaden generated-solid semantics.
- Fresh exact-SHA Actions validation is still pending. The available GitHub connector exposes rerun actions but no workflow-dispatch action; rerunning #138 would validate stale SHA `93a5547224a5248ae741ccd8dd4368bac27b6b00`, so it was intentionally not used as evidence for `d03edf8...`.

## Collision rules for this claim

- Do not enter any source lane already marked `ACTIVE`/`BLOCKED` by another agent.
- In particular, re-check current ownership before touching Source Reconcile/#1005 or any LOCAL_ONLY BricsCAD-runtime lane; historical status is not sufficient.
- If `main` advances during investigation, refresh before any claim amendment or write.
- No speculative/no-op source commit and no CI-gate weakening merely to obtain green status.

## Evidence baseline

- `docs/AGENT-WORK-REGISTRATION.md` already requires a claim-only commit on `origin/main` before code diagnosis/editing/testing.
- Recent history showed heavy concurrent movement of `main`.
- V25 workflow run #473 / job `94630732537` on stale SHA `93a5547da20df6d727c271a3ed85c17d2ff225fd` failed at deterministic Core smoke, but a newer local exact-SHA qualification at `2dc87bf...` isolated the concrete `NumericAliasesShareIdentity` regression and handed it off as #1092.

## Completion criteria

- Coordination/handoff Markdown committed and pushed to `main`.
- Claim-expansion rule committed to `docs/AGENT-WORK-REGISTRATION.md`.
- Issue #1092 fixed with deterministic Core smoke coverage and source/test commits pushed to `main`, or documented BLOCKED with exact evidence if collision/new blocker appears.
- LOCAL-003 remains untouched except for consuming the non-local fix through its own claim/reactivation process.
- Final claim status changed to `DONE` or `BLOCKED` with exact evidence and commit SHAs.
