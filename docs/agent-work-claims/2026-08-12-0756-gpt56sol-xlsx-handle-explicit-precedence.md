# Work claim — XLSX Handle reader explicit-handle precedence

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-explicit-precedence-20260812-0756`
- Registered: `2026-08-12T07:56:00+07:00`
- Baseline main SHA: `9071c380d8cfe76bc43b02dc10c084d28fd0283c`
- Priority: P2 evidence-driven remote-safe XLSX lookup correctness

## Confirmed defect

`XlsxHandleReader.ReadHandleLookup(...)` parses legacy `$decimal` handles from every target-row cell before it parses the explicitly discovered CAD Handle column. In a non-modern workbook that has a clear `CAD Handle (hex)` header/value plus an unrelated cell such as `$123`, `preferLegacy` becomes true and returns hexadecimal `7B`, silently ignoring the explicit Handle value.

This defect does not depend on inferring BLT's proprietary layout: whenever an explicit CAD Handle column has been discovered from workbook headers, that explicit semantic column must take precedence over a fallback heuristic scanning unrelated cells.

## Reserved scope

Keep the existing `$decimal` legacy fallback unchanged for worksheets that do not expose an explicit Handle header, but do not allow the fallback to override a discovered Handle column. Preserve modern QS3D schema behavior, legacy decimal conversion, header discovery and explicit hexadecimal token parsing.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleExplicitPrecedenceSmoke.cs`
- this claim file

## Excluded scope

- No attempt to infer or redesign BLT proprietary column/layout semantics.
- No removal of `$decimal` compatibility fallback.
- No XLSX exporter changes.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation plan

- With `CAD Handle (hex)=1A` and another target-row cell `$123`, reader must return `1A` and must not mark the result as legacy-decimal.
- A metadata-free/no-Handle-header row containing `$123` must still preserve legacy fallback and return `7B` with `UsesLegacyDecimalHandles=true`.
- Preserve modern schema behavior.
- Re-read source/test after SHA-guarded integration and preserve concurrent history.

## Coordination

Recent search found no active decimal-handle precedence owner. The existing BLT compatibility path remains supported; this claim only fixes precedence when a semantic Handle header is already present.

## Completion condition

Completed only when explicit Handle columns cannot be overridden by unrelated `$decimal` cells, legacy fallback remains covered without a Handle header, focused regression source is on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
