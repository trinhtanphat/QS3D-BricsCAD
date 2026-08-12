# Work claim — ProjectFileLock public error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-file-lock-error-redaction-20260812-1150`
- Registered: `2026-08-12T11:50:00+07:00`
- Baseline main SHA: `ee17eb960a54900c387c3b990beaddf37d700045`
- Priority: P1 persistence privacy / error boundary

## Confirmed defect

`ProjectFileLock.Acquire(...)` constructs the writer rendezvous path from the absolute project path, then on `IOException` reflects that absolute `*.lock` path directly into the outer `InvalidOperationException.Message`. Callers that surface the public exception text can therefore disclose a user's filesystem root/project path even though the actionable contract is only that the exclusive QS3D write lock could not be acquired.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectFileLock.cs`
- a new focused Core smoke file for ProjectFileLock public-error redaction
- this claim file for close-out

## Contract

- Preserve the current stable rendezvous lock path, `FileShare.None` exclusivity, lock payload, stream flush/disposal and successful acquisition semantics.
- Preserve the caught `IOException` as `InnerException` for diagnostic causality.
- Make the outer `InvalidOperationException.Message` stable and path-free; it must not contain the project path, filesystem root, or `.lock` suffix.
- Preserve current argument validation and do not change persistence formats or ProjectSession/UI behavior.

## Exclusions

- No changes to project save/load semantics, session auditing, project formats, recovery, UI/commands, or BricsCAD integration.
- No attempt to redact implementation details inside `InnerException`; this lane only defines the public outer message boundary.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Validation plan

Add an auto-registered Core smoke that acquires one project lock, verifies a concurrent second acquire fails with the exact generic public message and no root/project/`.lock` text, disposes the first lock, then verifies the same project lock can be acquired again.

## Coordination

No existing `ProjectFileLock` redaction claim/commit was found in collision checks. Current open Dependency Impact work is a separate scope.

## Completion condition

Source fix and focused smoke source are integrated on current `main`, read back after merge, and this claim is marked `COMPLETED` with exact PR/SHA evidence and validation boundaries.
