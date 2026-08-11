# Work claim — updater generation-safe publication

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:07:00+07:00`
- Baseline main SHA: `8eacf8db3b264304b168017fa0af1989251c2a33`
- Priority: owner-requested whole-repository audit; modeless updater lifecycle correctness

## Verified defect

`UpdateCoordinator.CheckCoreAsync(...)` and `ScheduleLatestAsync()` currently test lifecycle freshness with `IsGenerationCurrent(generation)` and then call `Publish(...)` in a separate critical section. `Stop()` / `Start()` can advance `_generation` between those operations. A completed async check from the old lifecycle can therefore overwrite `_last` or enqueue `StateChanged` / `AutomaticUpdateFound` after a new lifecycle has started.

The dispatcher callback created by `Publish(...)` also has no generation guard. Even when the state write happened while the generation was current, a queued callback can run after `Stop()` / `Start()` and surface stale update state or an automatic-update notification in the new lifecycle.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs`
- `scripts/preflight-update-generation-publish.py` (new)
- `docs/UPDATER-GENERATION-PUBLISH-PLAN-2026-08-12.md` (new)
- this claim file

## Non-overlap / preservation

- Preserve current release selection, strict SemVer, signed-manifest pre-close validation, stopped-refresh behavior, in-flight single-flight semantics, cross-process update reservation, graceful host close and updater worker contracts.
- Do not edit `SecureUpdateLauncher.cs`, `GitHubReleaseClient.cs`, installer/uninstaller scripts, manifest generation, release workflow, or unrelated product surfaces.
- No GitHub Actions dispatch and no release publication.

## Intended contract

1. Generation freshness and `_last` publication are one atomic coordinator operation.
2. A stale generation cannot overwrite `_last` after `Stop()` / `Start()` advances lifecycle state.
3. Dispatcher-delivered `StateChanged` / `AutomaticUpdateFound` revalidate the same generation before invoking subscribers.
4. `Checking`, successful results, errors and scheduled-state publication all use the same generation-aware publication contract where lifecycle ownership matters.
5. Existing `Stop()` behavior remains fail closed: stopped refreshes do not start network work and stale in-flight work can complete only as an un-published return value.

## Validation / release conditions

- Commit a planning MD before implementation.
- Add an auto-discovered static regression pinning atomic generation publication and dispatcher revalidation, while rejecting the old split `IsGenerationCurrent(...)` + `Publish(...)` pattern in lifecycle-owned call sites.
- Re-fetch exact source/gate and verify ancestry with `behind_by: 0` before closing.
- BricsCAD V25 runtime behavior remains `LOCAL-009 / PENDING_LOCAL`; do not claim remote runtime PASS.
- Mark this claim `COMPLETED` only after source + regression gate are committed on `main`.
