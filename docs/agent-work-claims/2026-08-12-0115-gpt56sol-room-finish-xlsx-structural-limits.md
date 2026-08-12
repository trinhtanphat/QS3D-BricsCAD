# Work claim — Room Finish XLSX structural limits

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-room-finish-xlsx-limits-20260812-0115`
- Registered: `2026-08-12T01:15:00+07:00`
- Completed: `2026-08-12T01:19:00+07:00`
- Baseline main SHA: `e84a5a28322254c240eae68b5e32c01cde9de09e`
- Claim commit: `0fbd1f92934adc5ed103b074a55bc3b22b2ea75b`
- Source fix commit: `f7a65c28f6a5b885f2c0cc2f5d8923eb2b3cfd68`
- Regression commit: `d8bb70cd8c788dc6d824e9479327b9e72b38262b`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defects

`RoomFinishXlsxExporter` had no Excel worksheet row bound and no 32,767-character inline-string cell bound. It could therefore accept more than 1,048,575 data rows (overflowing the 1,048,576-row worksheet including the header) or publish oversized scalar/aggregated text cells such as Floor, Room, Family, Material, Element IDs or Room IDs.

## Implemented

- Added `MaxDataRows = 1048575` and reject larger `IReadOnlyList` inputs before row indexing/enumeration or filesystem mutation.
- Added `MaxCellTextCharacters = 32767` and preflight every scalar inline-string field: Floor, Room, Category, FamilyName, Material and UnitHint.
- Added separator-inclusive length preflight for ElementIds and RoomIds before `string.Join(...)` can allocate an oversized cell payload.
- Runtime null string/list entries retain the prior empty-string join/serialization behavior.
- Existing package construction, numeric finite checks and atomic publish behavior remain unchanged.
- Added module-initializer smoke `RoomFinishXlsxStructuralLimitSmoke` proving oversized row-count rejection before indexer/enumerator access, exact 32,767-character scalar acceptance, 32,768-character scalar rejection, and separator-driven aggregate ElementIds overflow rejection before filesystem mutation.

## Implemented surfaces

- `src/QS3D.Core/Export/RoomFinishXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishXlsxStructuralLimitSmoke.cs`
- this claim file

## Excluded scope honored

- No `RoomFinishSchedule.cs` grouping/business-rule changes.
- No XML sanitization-policy change in this claim.
- No Door/Material/Curtain/Quantity/Rebar exporters.
- No UI/native BricsCAD/runtime or GitHub Actions work.

## Validation actually performed

- Claim commit was verified as an ancestor of current `main`; commits after registration before implementation were disjoint V26/source-reconcile work.
- Exact current exporter blob was re-fetched and SHA-guarded before the source write.
- Current `main` was re-read after integration and contains row/text preflight before path/directory/temp-file mutation.
- Focused smoke source was re-read and contains all four structural-limit scenarios.
- Regression commit `d8bb70cd8c788dc6d824e9479327b9e72b38262b` was verified as an ancestor of later current `main` `154c166473800d2a0b71a803f06d650bd908d4cb`; subsequent commits were disjoint Grid/V26/Floor work.
- No force push/reset/revert was used.
- No local .NET smoke execution is claimed in this connector-only lane.
- No BricsCAD V25/V26 runtime or GitHub Actions execution is claimed.

## Coordination

The active Room Finish schedule group-key claim explicitly excluded XLSX. This claim did not reopen schedule grouping or runtime work.

## Completion condition

Completed. Room Finish XLSX now enforces Excel worksheet/cell structural limits before filesystem mutation, aggregate ID length is bounded before joining, focused regression source is present on current `main`, and exact integration SHAs are recorded above.
