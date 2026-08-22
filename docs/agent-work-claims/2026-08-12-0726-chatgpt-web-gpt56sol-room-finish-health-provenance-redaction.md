# Work claim — Room Finish health provenance redaction

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-room-finish-health-provenance-redaction`
- Registered: `2026-08-12T07:26:00+07:00`
- Completed: `2026-08-12T07:26:00+07:00`
- Baseline main SHA: `dc50effd3bb626235e818121c65255c18da3d38d`
- Priority: P1 — persisted Room provenance conflicts must fail visible without reflecting raw conflicting identifiers through exception text.
- Task Key: `CORE-ROOM-FINISH-HEALTH-PROVENANCE-REDACTION`

## Confirmed defect

`RoomFinishHealthService.Inspect(...)` caught `InvalidOperationException` from `AutoRoomLifecycle.ResolveRoomReferenceId(...)` and appended `ex.Message` verbatim to `ROOM_PROVENANCE_CONFLICT`. The resolver constructs that exception from persisted Room provenance candidates gathered from `RoomSourceId`, `ParentRoomId`, `SourceRoomId`, `GeneratedFromRoomId`, `RoomId`, and dependencies, so the health provider reflected raw conflicting persisted identifiers. Because the provider handled the exception internally, aggregate health provider redaction could not sanitize the detail.

## Reserved scope

- `src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs`
- `scripts/preflight-room-finish-health-provenance-redaction.py`
- this claim file

`AutoRoomLifecycle`, Room generation, finish quantity rules, native Room Finish Tables, UI, and BricsCAD runtime code were not modified.

## Completed implementation

- Claim registration: `ea4648c45be36f38567a953982d225eab58b3257`.
- Source fix: `885c0d510fcf38816b288c33a8439970c7223b52` (`fix(health): redact room provenance conflicts`).
- Focused regression gate: `34c256e4a83841afe26f6bd6414a4d7a4bde52e1` (`test(health): pin room provenance redaction`).
- `ROOM_PROVENANCE_CONFLICT` remains `HealthSeverity.Error` and still targets the finish element, while the message is stable and no longer appends resolver `Exception.Message` or enumerates conflicting Room provenance candidates.
- The recovery boundary remains specific to `InvalidOperationException`; all existing unlinked/orphan/ambiguous/scope/stale/duplicate Room Finish diagnostics remain unchanged.

## Validation actually performed

- Re-fetched current `main` source after source/gate commits; `RoomFinishHealthService.cs` is blob `b2b4d39eedaa6e643f0c64106b361fcae177c72a` with `catch (InvalidOperationException)` and no raw exception detail.
- Re-fetched the focused gate from `main`; gate blob is `e0241919ebc72138a6be7ba1f7f997d815e6e2a2` and pins the resolver call, Error code/severity, stable message, neighboring diagnostics, absence of `ex.Message`, and read-only mutation exclusions.
- `AutoRoomLifecycle.cs` was read only as evidence and was not edited.
- No GitHub Actions/build/release workflow was dispatched. No executable Core smoke, full solution build, or BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied on merged source contract: Room provenance conflicts remain fail-visible without raw resolver exception detail, focused regression coverage pins the contract, and this claim is closed `COMPLETED`.
