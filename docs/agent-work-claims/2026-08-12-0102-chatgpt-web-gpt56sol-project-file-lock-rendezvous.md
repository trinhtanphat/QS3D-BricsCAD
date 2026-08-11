# Work claim — Project file-lock rendezvous integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T01:02:43+07:00`
- Baseline main SHA: `17c51afff0e5faaa7dbe9914807bc1b446c541bb`
- Priority: `Correctness defect found during requested deep repository audit; prevent cross-platform lock ownership split after Dispose.`

## Reserved scope

Harden `ProjectFileLock` release semantics so disposing one owner cannot unlink the shared lock rendezvous path after a concurrent owner has acquired it, and add deterministic Core smoke coverage for the persistent rendezvous invariant.

## Expected surfaces

- `src/QS3D.Core/Persistence/ProjectFileLock.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFileLockSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- `ProjectFileLock.Acquire(...)` / `ProjectFileLock.Dispose()`
- Core smoke registration for project-file-lock rendezvous lifecycle

## Excluded scope

- `ProjectSession`, QSDB backup/recovery, save/reload atomicity, mutation transactions, BricsCAD command/runtime locking, installer/update locking, and any unrelated persistence work.
- Windows/BricsCAD V25/V26 runtime qualification and GitHub Actions dispatch.

## Validation plan

- Preserve exclusive contention behavior while one owner holds the lock.
- Add a deterministic assertion that releasing an owner leaves the rendezvous `.lock` file in place, then reacquire through the same path and preserve contention.
- Inspect the exact source/test diff on current `main`; use only source-level/Core evidence available remotely and do not claim BricsCAD runtime PASS.

## Coordination

No indexed current claim or source search matched `ProjectFileLock` before registration. This lane is intentionally limited to the Core lock rendezvous lifecycle and excludes neighboring persistence/session lanes. Smoke coverage is isolated in a dedicated registration module to avoid modifying the large legacy `Program.cs` surface.

## Completion condition

A pushed `main` commit prevents `Dispose()` from deleting the rendezvous path, pins the invariant in Core smoke coverage, and this claim is updated to `COMPLETED` with exact commit/evidence details.
