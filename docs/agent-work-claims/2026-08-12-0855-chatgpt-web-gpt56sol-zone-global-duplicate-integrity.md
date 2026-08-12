# Work claim — Zone target operations global duplicate integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-zone-global-duplicate-integrity-20260812-0855`
- Registered: `2026-08-12T08:55:00+07:00`
- Baseline main SHA: `816e9cc7a0141749c818e315713a1fdbc8d33e15`
- Priority: P1 — target-based Zone operations must reject globally ambiguous Zone identity state.
- Task Key: `CORE-ZONE-TARGET-OPS-GLOBAL-DUPLICATE-ID`

## Confirmed defect

The historical duplicate-Zone fix routed `FindRequired(...)` through `ProjectState.FindZone(targetId)`. That rejects duplicates only when they match the requested target ID. If a project contains an unrelated duplicate pair (for example `Z1`/`z1`) and a unique target `Z2`, target-based operations can still resolve `Z2` and continue. Mutating paths such as `Update`, `SetActive`, `Assign`, and `Delete` can therefore modify a project whose Zone identity collection is globally invalid under QSDB/interchange identity rules. `ReferenceCount` can also return a normal result from the same ambiguous project.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectZoneGlobalDuplicateIntegritySmoke.cs`
- this claim file

## Intended contract

- Case-insensitive uniqueness of all existing non-null Zone IDs is checked before target resolution.
- `Create` reuses the same uniqueness helper already introduced by the completed Create-specific lane.
- `Update`, `SetActive`, `Assign`, `Delete`, and `ReferenceCount` fail closed on an unrelated duplicate Zone pair before mutation/result production.
- Existing null-entry behavior remains delegated to current Create guard / `ProjectState.FindZone`; valid target semantics and canonical no-op rules remain unchanged.
- No Floor/Family service, Floor/Zone UI, persistence/interchange or native BricsCAD changes.

## Validation plan

Focused auto-registered Core smoke seeds `Z1`/`z1` plus unique `Z2`, then proves representative mutation paths reject before ChangeVersion/zone state changes and read-only ReferenceCount also rejects. Valid controls remain unchanged. Re-fetch current source/claim before writes. No force-push, Actions dispatch, .NET smoke PASS or licensed BricsCAD runtime qualification claim unless actually executed.
