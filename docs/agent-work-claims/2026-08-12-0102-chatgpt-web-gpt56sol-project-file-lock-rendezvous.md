# Work claim — Project file-lock rendezvous integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T01:02:43+07:00`
- Baseline main SHA: `17c51afff0e5faaa7dbe9914807bc1b446c541bb`
- Priority: `Correctness defect found during requested deep repository audit; prevent cross-platform lock ownership split after Dispose.`

## Reserved scope

Harden `ProjectFileLock` release semantics so disposing one owner cannot unlink the shared lock rendezvous path after a concurrent owner has acquired it, and add deterministic Core smoke coverage for the persistent rendezvous invariant.

## Expected surfaces

- `src/QS3D.Core/Persistence/ProjectFileLock.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFileLockSmoke.cs`
- `ProjectFileLock.Acquire(...)` / `ProjectFileLock.Dispose()`
- Core smoke registration for project-file-lock rendezvous lifecycle

## Excluded scope

- `ProjectSession`, QSDB backup/recovery, save/reload atomicity, mutation transactions, BricsCAD command/runtime locking, installer/update locking, and any unrelated persistence work.
- Windows/BricsCAD V25/V26 runtime qualification and GitHub Actions dispatch.

## Validation executed

- Source fix commit: `40ea5e808d6dc3fc74b491555789e2d0ebafdd16`.
- Regression smoke commit: `1c4a85aa454a3da2d7f1e8a6c6f233999166f63b`.
- Re-read both source and regression smoke from current `main` after both commits landed; the source blob is `e5cee7e796f02c20cef7d3e322ce0c82059e1a21` and smoke blob is `e832df4ba03df3ea09883c232a7eb6a67f6e63f0`.
- Smoke covers held-owner contention, persistent rendezvous after release, reacquire, and contention after reacquire. It self-registers via `ModuleInitializer`, so the shared `SmokeTestRegistration.cs` was intentionally left unchanged to reduce concurrent-edit collision.
- GitHub combined status returned no published statuses and the commit had no pull-request workflow runs at close-out time. No GitHub Actions were dispatched and no BricsCAD runtime PASS is claimed.

## Coordination

No indexed current claim or source search matched `ProjectFileLock` before registration. The implemented lane remained limited to the Core lock rendezvous lifecycle and did not touch neighboring persistence/session work. Two low-level non-fast-forward push attempts were safely rejected while `main` moved; final writes used current blob-aware GitHub Contents API commits without force-push or overwriting concurrent work.

## Completion condition

Completed: `Dispose()` no longer deletes the shared rendezvous path; acquisition uses `OpenOrCreate` and truncates metadata only after the exclusive `FileStream` is established; deterministic Core smoke coverage is pushed on `main`.
