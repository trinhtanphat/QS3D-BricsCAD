# Agent concurrency handoff — 2026-08-14

This note is a shared coordination checkpoint for concurrent agents working on `trinhtanphat/QS3D-BricsCAD`.

## Mandatory claim-first rule

Before an agent begins any implementation, diagnosis or test work on a lane:

1. refresh `origin/main` and inspect current `ACTIVE` / `BLOCKED` claims;
2. publish a claim-only Markdown commit under `docs/agent-work-claims/` with exact expected paths/symbols/tests/runtime scope;
3. push that claim to `main` and verify it is an ancestor of the newest `origin/main`;
4. recheck concurrent claims;
5. only then read implementation code, diagnose, edit or test the reserved lane.

If investigation discovers a path/symbol/test/runtime surface not named in the current claim, stop before touching it and land a claim-amendment-only commit first. Immediately before every source/test/script write or PR merge, refresh `main` and recheck both exact-path claims and commits added since the prior check.

The canonical detailed protocol is `docs/AGENT-WORK-REGISTRATION.md`.

## Current deterministic Core smoke blocker handoff

LOCAL-003 qualification on clean exact SHA `2dc87bf0985c5967f9ca45f09aac22ba85e2e0cd` passed Core Release, nine focused Level/static gates and the installed-reference BricsCAD V25 build, then the mandatory full Core smoke failed in:

- `ModelHealthSourceHandleSmoke.NumericAliasesShareIdentity`
- expected one `DUPLICATE_SOURCE_HANDLE` for numeric aliases `A` / `00a`
- actual duplicate-source issue count: `0`
- live alias case includes `0xA`

The local agent correctly did not edit CAD-independent Core/tests and handed the blocker to issue `#1092`; LOCAL-003 is `BLOCKED` until full Core smoke is clean again, then the local owner must reactivate its own claim before resuming exact-SHA native qualification.

An older cloud V25 run is also useful historical evidence but must not be treated as current-HEAD proof:

- workflow: `.github/workflows/release-v25-cloud.yml`
- workflow run number `#138`, run id `31755659447`
- job `94630732537`
- SHA `93a5547224a5248ae741ccd8dd4368bac27b6b00`
- failed at `Run deterministic Core smoke`

Current source-level blocker evidence comes from the newer `2dc87bf...` local exact-SHA run above, not from assuming the stale cloud run still represents `main`.

## #1092 source state and concurrency incident

`chatgpt-web-gpt56sol` published:

- initial coordination claim: `3cb24f5d9041209e00704d8270c61218278d8baf`
- exact #1092 source/test scope amendment: `404e110d954a354541cefaf0c3dddde5e399c0e7`

The amendment reserved:

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthSourceHandleSmoke.cs`
- issue `#1092` closeout tied to that fix

Historical commit `a4b854d1e81355ce35157932443e16761c734988` showed the previously qualified contract: persisted and live semantic source handles must use canonical numeric CAD-handle identity while generated-solid handle normalization remains separate.

While that claim was already visible, concurrent commit `d03edf8e4c476ee929d731a2c0c7400a8b8d14e4` landed directly on `main` and modified the same `ModelHealthService.cs` lane. Its patch canonicalizes persisted and live source handles through `GeneratedHandleIdentity.Normalize` and adds a dedicated live-source normalization set. This is exactly the kind of overlap claim-first registration is intended to prevent.

A redundant recovery commit had already been prepared as `4922d0c8287ea2427cedf3a4526daef4e73b2246`, and PR `#1093` exposed only one file with `+10/-2`; after detecting `d03edf8...`, PR `#1093` was closed unmerged rather than stacking a duplicate patch. Do not reopen/merge that PR unless the repository owner explicitly establishes a new need.

## Required next evidence for #1092

Do not declare #1092 complete merely because the source patch exists. The next remote-safe step is to verify current `main` with the deterministic Core smoke that contains `ModelHealthSourceHandleSmoke.NumericAliasesShareIdentity` and confirm the existing positive/negative cases remain clean. Do not weaken the smoke to obtain green status.

If another failure appears:

- identify the exact exception/test first;
- recheck `ACTIVE` / `BLOCKED` claims;
- claim the new exact lane with a claim-only commit;
- only then inspect or change its source/test paths.

If full Core smoke passes on current `main`, record the exact SHA/evidence in issue `#1092` and the owning claim, then close #1092. LOCAL-003 remains a separate local-only runtime qualification and must be reactivated by its local owner before BricsCAD work resumes.

## Boundaries

- Do not alter LOCAL_ONLY native BricsCAD evidence from a remote agent.
- Do not enter Source Reconcile/#1005 or any other `ACTIVE`/`BLOCKED` lane without a current ownership check and explicit split/transfer.
- Do not force-push, reset or rewrite concurrent work.
- Do not create no-op/speculative source commits or weaken CI/smoke gates merely to report green.
