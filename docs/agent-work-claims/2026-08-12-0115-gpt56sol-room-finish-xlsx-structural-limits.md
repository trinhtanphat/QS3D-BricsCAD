# Work claim — Room Finish XLSX structural limits

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish-xlsx-limits-20260812-0115`
- Registered: `2026-08-12T01:15:00+07:00`
- Baseline main SHA: `e84a5a28322254c240eae68b5e32c01cde9de09e`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defects

`RoomFinishXlsxExporter` currently has no Excel worksheet row bound and no 32,767-character inline-string cell bound. It can therefore accept more than 1,048,575 data rows (overflowing the 1,048,576-row worksheet including the header) or publish oversized scalar/aggregated text cells such as Floor, Room, Family, Material, Element IDs or Room IDs.

## Reserved scope

Enforce Excel's worksheet data-row capacity and inline-string text capacity before any destination filesystem mutation. Aggregated ElementIds/RoomIds length must be checked from the list entries before `string.Join(...)` builds an oversized string.

## Expected surfaces

- `src/QS3D.Core/Export/RoomFinishXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishXlsxStructuralLimitSmoke.cs`
- this claim file

## Excluded scope

- No `RoomFinishSchedule.cs` grouping/business-rule changes; its active collision claim explicitly excludes XLSX.
- No XML sanitization-policy change in this claim.
- No Door/Material/Curtain/Quantity/Rebar exporters.
- No UI/native BricsCAD/runtime or GitHub Actions work.

## Validation plan

- Reject 1,048,576 data rows before indexing/enumeration or filesystem mutation.
- Accept exactly 32,767 characters in a scalar text cell and reject 32,768.
- Reject an aggregated ElementIds/RoomIds cell whose separator-inclusive length exceeds 32,767 before joining it and before filesystem mutation.
- Preserve ordinary package generation, numeric finite checks and atomic publish behavior.
- SHA-guard writes and re-read current `main` after integration.

## Coordination

The active Room Finish schedule group-key claim explicitly excludes XLSX. Recent searches found no Room Finish XLSX row/cell-limit owner. This claim does not reopen schedule grouping or runtime work.

## Completion condition

Completed only after the exporter enforces row and cell limits pre-filesystem, focused regression source is present on current `main`, exact SHAs are recorded, and the claim is marked `COMPLETED`.
