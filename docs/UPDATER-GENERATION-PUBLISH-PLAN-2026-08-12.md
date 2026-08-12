# Updater generation-safe publication plan — 2026-08-12

## Goal

Remove the lifecycle race between async updater work and `UpdateCoordinator.Stop()` / `Start()` without weakening any existing updater security or availability guards.

## Current defect

The coordinator uses a two-step pattern for lifecycle-owned results:

1. check freshness with `IsGenerationCurrent(generation)`;
2. call `Publish(...)` later, where `_last` is updated and dispatcher events are queued.

Those operations are protected by separate lock acquisitions. `Stop()` / `Start()` can advance `_generation` in the gap. The queued dispatcher callback also runs without re-checking generation. Therefore an old updater lifecycle can publish state or notifications into a newer lifecycle.

## Invariants to preserve

- `Start()` remains idempotent while already started and advances generation only when opening a new lifecycle.
- `Stop()` invalidates in-flight work, clears single-flight references and prevents stopped refreshes from starting network work.
- A stale in-flight check may finish and return its local result to its awaiting caller, but must not mutate coordinator-visible state or notify subscribers.
- One generation owns each lifecycle-visible `Checking`, final result, error and scheduled result publication.
- `ScheduleLatestAsync()` must still re-check release freshness immediately before scheduling and must not schedule if lifecycle generation changed.
- Release selection, signed-manifest probe, strict SemVer and `SecureUpdateLauncher` behavior remain untouched.

## Implementation

### 1. Replace split freshness/publish with one generation-aware publisher

Add a private helper similar to `TryPublishCurrent(int generation, UpdateCheckResult result, bool automaticNotification)`.

Inside one `_sync` critical section it will:

- require `_started && generation == _generation`;
- update `_last` only when current;
- capture the dispatcher for delivery;
- return false without side effects when stale.

This closes the race between freshness validation and `_last` mutation.

### 2. Revalidate generation on dispatcher delivery

The dispatcher callback must re-enter `_sync` and verify the same generation is still active before invoking `StateChanged` or `AutomaticUpdateFound`.

This prevents a callback queued under generation N from firing after `Stop()` / `Start()` has moved the coordinator to generation N+2.

### 3. Route lifecycle-owned publications through the helper

Use generation-aware publication for:

- initial `Checking` state in `CheckCoreAsync`;
- successful final check state;
- error state;
- `Scheduled` state after a successful schedule;
- schedule failure state only when the lifecycle remains current.

Remove the old `IsGenerationCurrent(...)` then `Publish(...)` split at these call sites.

### 4. Keep non-lifecycle initialization simple

Constructor initialization of `_last` remains direct; no dispatcher event is required before `Start()`.

## Regression gate

Add `scripts/preflight-update-generation-publish.py` to statically assert:

- a generation-aware publication helper exists and checks `_started` plus generation under `_sync` before `_last = result`;
- dispatcher delivery performs a second generation check before subscriber invocation;
- `CheckCoreAsync` publishes `Checking`, success and error through the generation-aware helper;
- `ScheduleLatestAsync` publishes scheduled/failure state through the same lifecycle-aware contract;
- the old source pattern `if (IsGenerationCurrent(generation)) Publish(...)` is absent;
- security-critical updater files outside the reserved scope are not required to change.

The preflight is source-only and must not be described as BricsCAD runtime proof.

## Validation

1. Re-fetch latest `main` before source mutation and confirm no concurrent updater coordinator claim/change.
2. Commit the coordinator fix to `main`.
3. Add/commit the focused static preflight.
4. Re-fetch both files and inspect exact committed source.
5. Compare the claim baseline/implementation commits against current `main`; require `behind_by: 0` before closure.
6. Mark the work claim `COMPLETED` with the exact source/gate SHAs.
7. Do not dispatch GitHub Actions or publish a release.

## Local qualification

Real WPF dispatcher timing and BricsCAD plugin lifecycle behavior remain covered by `LOCAL-009 / PENDING_LOCAL`. The remote work closes the source race contract only; it does not claim licensed V25 runtime PASS.
