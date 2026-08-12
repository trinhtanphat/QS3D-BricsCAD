# Work claim — Active Floor/Zone health canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-active-floor-zone-health-canonicality`
- Registered: `2026-08-12T09:22:00+07:00`
- Baseline main SHA: `cb55b30fd16e4d613ac5a105badb99376a149884`
- Priority: P1 — baseline Model Health must surface non-canonical persisted active Floor/Zone aliases before mutation repair.
- Task Key: `CORE-MODEL-HEALTH-ACTIVE-FLOOR-ZONE-CANONICALITY`

## Confirmed defect

`ProjectFloorService.SetActive(...)` and `ProjectZoneService.SetActive(...)` were hardened in `3fa9a709307fbd9e9f1614f6b072efd2affe449f` to repair any case/whitespace alias to the exact project-owned `Floor/Zone.Id`. Baseline `ModelHealthService.ValidateActiveFloor(...)` and `ValidateActiveZone(...)` still trim the persisted active ids and perform case-insensitive dictionary lookup, so a malformed alias can look healthy until a mutation path repairs it.

## Non-overlap check

The completed active Floor/Zone canonical-id lane changes mutation semantics and smoke coverage, not baseline health. Recent Floor/Zone UI no-op audit work is also separate. No dedicated Active Floor/Zone health canonicality claim/commit was found.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- one focused Core smoke regression for active Floor/Zone health canonicality
- this claim file

Do not modify `ProjectFloorService`, `ProjectZoneService`, UI audit behavior, persistence format or BricsCAD runtime code.

## Intended contract

- If a unique active Floor/Zone target exists but the stored active id differs from the exact project-owned id by case and/or surrounding whitespace, health emits a dedicated `HealthSeverity.Error` canonicality diagnostic.
- Missing active ids preserve existing `INVALID_ACTIVE_*` warnings.
- Duplicate target ids preserve existing `AMBIGUOUS_ACTIVE_*` errors without selecting an arbitrary canonical target.
- Exact canonical active ids preserve existing behavior.
- Inspection remains read-only.

## Completion condition

Non-canonical active aliases are fail-visible, focused smoke coverage pins padded/case aliases plus canonical/missing controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
