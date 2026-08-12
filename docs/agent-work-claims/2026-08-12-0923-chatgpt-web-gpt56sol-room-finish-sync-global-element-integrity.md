# Work claim — Room Finish single-sync global element identity integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish-sync-global-element-integrity-20260812-0923`
- Registered: `2026-08-12T09:23:00+07:00`
- Baseline main SHA: `7c553f8cf0b07915ced53ab833aff495aedbdc3a`
- Priority: P1 — prevent direct Room Finish synchronization from mutating a project with globally ambiguous semantic element identity.

## Confirmed defect

`RoomFinishSynchronizationService.SynchronizeExisting(...)` becomes globally identity-safe through `RoomFinishIdentityService.FindExisting(...)`, which builds a duplicate-rejecting element index before synchronization. The direct overload `Synchronize(project, room, finish)` only validates the requested Room and Finish through target-specific `ProjectState.FindElement(...)`. Unrelated duplicate element IDs can therefore coexist while a unique Room/Finish pair is synchronized, changing Floor/Zone/fingerprint/provenance/metrics, dirty state and project revision despite the project being invalid under QSDB and other Core mutation boundaries.

## Reserved surfaces

- `src/QS3D.Core/Services/RoomFinishSynchronizationService.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishSyncGlobalElementIntegritySmoke.cs` — new focused regression
- this claim file

## Intended fix

- Preflight the complete `project.Elements` collection for null/blank/case-insensitive duplicate semantic IDs before the direct `Synchronize(project, room, finish)` path can mutate.
- Preserve `SynchronizeExisting` behavior, rollback semantics, stale Auto Room guard, finish identity/provenance checks, dependency canonicalization, metric validation and recent idempotency behavior.
- Focused smoke proves unrelated duplicate IDs fail before Finish/project mutation while a valid direct synchronization still updates canonical provenance and advances one project revision.

## Coordination

Recent Room Finish synchronization idempotency work is completed; this lane changes only global identity preflight and a new smoke. Current snapshot/Documentation/Recognition claims own other files.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed.
