# Work claim — ProjectFileLock public error redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-file-lock-error-redaction-20260812-1150`
- Registered: `2026-08-12T11:50:00+07:00`
- Completed: `2026-08-12T11:55:00+07:00`
- Baseline main SHA: `ee17eb960a54900c387c3b990beaddf37d700045`
- Priority: P1 persistence privacy / error boundary

## Confirmed defect

`ProjectFileLock.Acquire(...)` constructed the writer rendezvous path from the absolute project path, then on `IOException` reflected that absolute `*.lock` path directly into the outer `InvalidOperationException.Message`. Callers surfacing the public exception text could therefore disclose a user's filesystem root/project path.

## Integrated contract

The stable rendezvous lock path, `FileShare.None` exclusivity, lock payload, stream flush/disposal and successful acquisition semantics are unchanged. The caught `IOException` remains the `InnerException`, while the public outer message is now the stable path-free text `Unable to acquire exclusive QS3D project write lock.`.

## Evidence

- PR: `#847`
- Squash merge: `a4f0cdf54c98bd1b319a3a5ba1c678ba05307dff`
- Source read back from `main`: `src/QS3D.Core/Persistence/ProjectFileLock.cs`
- Regression read back from `main`: `tests/QS3D.Core.SmokeTests/ProjectFileLockErrorRedactionSmoke.cs`
- Smoke source covers contention message redaction, preserved `IOException` causality and successful reacquisition after disposal.

## Exclusions preserved

No project save/load semantics, session auditing, project formats, recovery, UI/commands, BricsCAD integration, or `InnerException` redaction changes were made.

## Validation boundary

Source and smoke were read back from remote `main` after merge. No GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS is claimed without execution.
