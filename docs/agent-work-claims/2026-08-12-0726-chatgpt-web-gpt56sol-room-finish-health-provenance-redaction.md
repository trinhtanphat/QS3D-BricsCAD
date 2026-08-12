# Work claim — Room Finish health provenance redaction

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-room-finish-health-provenance-redaction`
- Registered: `2026-08-12T07:26:00+07:00`
- Baseline main SHA: `dc50effd3bb626235e818121c65255c18da3d38d`
- Priority: P1 — persisted Room provenance conflicts must fail visible without reflecting raw conflicting identifiers through exception text.
- Task Key: `CORE-ROOM-FINISH-HEALTH-PROVENANCE-REDACTION`

## Confirmed defect

`RoomFinishHealthService.Inspect(...)` catches `InvalidOperationException` from `AutoRoomLifecycle.ResolveRoomReferenceId(...)` and appends `ex.Message` verbatim to `ROOM_PROVENANCE_CONFLICT`. The resolver constructs that exception from persisted Room provenance candidates gathered from `RoomSourceId`, `ParentRoomId`, `SourceRoomId`, `GeneratedFromRoomId`, `RoomId`, and dependencies, so the health provider reflects raw conflicting persisted identifiers. Because the provider handles the exception internally, aggregate health provider redaction cannot sanitize the detail.

## Reserved scope

- `src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

`AutoRoomLifecycle`, Room generation, finish quantity rules, native Room Finish Tables, UI, and BricsCAD runtime code are excluded.

## Intended contract

- Preserve `ROOM_PROVENANCE_CONFLICT` with `HealthSeverity.Error` and the finish element id.
- Preserve the specific `InvalidOperationException` recovery boundary around `ResolveRoomReferenceId(...)`.
- Replace raw `Exception.Message` reflection with a stable actionable message that does not enumerate persisted provenance candidates.
- Preserve all existing Room Finish orphan/ambiguous/scope/stale/duplicate diagnostics and read-only behavior.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Room provenance conflicts remain fail-visible without raw resolver exception detail, focused source regression coverage pins the contract, and this claim is closed after merged-main readback.
