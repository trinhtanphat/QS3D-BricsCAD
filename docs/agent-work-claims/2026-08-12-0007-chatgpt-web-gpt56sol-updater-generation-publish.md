# Work claim — updater generation-safe publication

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:07:00+07:00`
- Baseline main SHA: `8eacf8db3b264304b168017fa0af1989251c2a33`
- Priority: owner-requested whole-repository audit; modeless updater lifecycle correctness

## Verified defect

`UpdateCoordinator.CheckCoreAsync(...)` and `ScheduleLatestAsync()` previously tested lifecycle freshness with `IsGenerationCurrent(generation)` and then called `Publish(...)` in a separate critical section. `Stop()` / `Start()` could advance `_generation` between those operations, allowing an old async lifecycle to overwrite `_last` or enqueue state/automatic-update events after a newer lifecycle had started.

The old dispatcher callback also had no generation revalidation, so an event queued while generation N was active could run after `Stop()` / `Start()` moved the coordinator to a later generation.

## Reserved scope used

- `src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs`
- `scripts/preflight-update-generation-publish.py`
- `scripts/preflight-update-coordinator-lifecycle.py`
- `docs/UPDATER-GENERATION-PUBLISH-PLAN-2026-08-12.md`
- this claim file

## Implemented contract

1. `TryPublishCurrent(...)` now checks `_started` and the captured generation under `_sync` before mutating `_last`, so lifecycle freshness and state publication are one coordinator-critical operation.
2. The dispatcher callback re-enters `_sync` and verifies the same generation again before invoking `StateChanged` or `AutomaticUpdateFound`, preventing queued callbacks from leaking across Stop/Start lifecycle boundaries.
3. `Checking`, final success, final error, schedule failure and scheduled state all route through the generation-aware publisher where lifecycle ownership applies.
4. Existing stopped-refresh behavior remains fail closed: `CheckAsync(...)` returns `_last` without launching network work when the coordinator is stopped.
5. Existing lock-linearized `TryScheduleCurrentGeneration(...)` authorization is preserved; `SecureUpdateLauncher` and all signing/manifest/install/restart contracts were left untouched.

## Commits

- Claim registration: `d15cf5c19fe24350379c03d0c6dc0bb5bdb336cf`
- Planning: `1ad5fcdb75f23803d077aadd607e7f45fce6ad31`
- Claim scope reconciliation for existing lifecycle gate: `ff81ef7719c8d85b20a5d3f980e5aeb5761494ca`
- Source fix: `f32808c84d8b509d64dfe1d46da6e1f39e4caec5`
- Existing lifecycle gate reconciliation: `59c9ec210abd88e0e3636b3bf7c04a089eebfc6c`
- Focused generation-publication regression gate: `7e4a16ae144406947ad930220715b7e9b2445e6d`

## Verification

- Re-fetched exact committed `UpdateCoordinator.cs`, `preflight-update-coordinator-lifecycle.py` and `preflight-update-generation-publish.py` from current `main` after the gate commits.
- Verified the real source-fix commit object `f32808c84d8b509d64dfe1d46da6e1f39e4caec5` changes only `UpdateCoordinator.cs` for this defect.
- Against observed `main` `b26dfb463369d00b5c84f32c26047d62f59c9337`:
  - source fix compare: `behind_by: 0`;
  - lifecycle gate reconciliation compare: `behind_by: 0`;
  - focused gate compare: `behind_by: 0`.
- The old split `IsGenerationCurrent(...)` + `Publish(...)` implementation is no longer present in the committed coordinator; the lifecycle gate now rejects its return.
- These are source/static regression contracts only. Real WPF dispatcher timing and BricsCAD V25 lifecycle qualification remain `LOCAL-009 / PENDING_LOCAL`.
- No GitHub Actions workflow was dispatched and no release was created/published in this batch.

## Outcome

The updater no longer has a source-level race window where an obsolete lifecycle can pass a freshness check and publish after a newer lifecycle takes ownership, and queued dispatcher notifications are generation-checked again at delivery time. Claim released.
