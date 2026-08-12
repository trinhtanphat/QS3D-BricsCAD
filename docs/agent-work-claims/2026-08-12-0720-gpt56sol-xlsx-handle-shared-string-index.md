# Work claim — XLSX Handle reader shared-string index validation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-shared-string-index-20260812-0720`
- Registered: `2026-08-12T07:20:00+07:00`
- Baseline main SHA: `7cbbb0a6f5fe772ca83c4f0fc2aa4211863e4667`
- Priority: P2 evidence-driven remote-safe XLSX input-integrity hardening

## Confirmed defect

`XlsxHandleReader.ReadCells(...)` currently dereferences a cell with `t="s"` only when `<v>` parses to an in-range shared-string index. If the index is non-numeric, negative or out of range, the method leaves the raw `<v>` text as the cell value. In a CAD Handle column, malformed shared-string XML such as `<c r="A2" t="s"><v>123</v></c>` can therefore be interpreted as hexadecimal handle `123` instead of rejecting the malformed workbook.

## Reserved scope

Fail closed for every `t="s"` cell whose shared-string index is missing, non-numeric, negative or outside the loaded shared-string table. Preserve inline strings, ordinary numeric/text cells, worksheet selection, modern/legacy handle semantics and existing XML/size/column guards.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleSharedStringIndexSmoke.cs`
- this claim file

## Excluded scope

- No XLSX exporter changes.
- No shared-string table size/schema redesign beyond strict reference validation.
- No BLT/ED2 handle parsing policy changes outside malformed shared-string references.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation plan

- Build a minimal XLSX ZIP with a valid shared-string table but an out-of-range `t="s"` handle-cell index and require `InvalidDataException` instead of a synthesized handle.
- Also cover non-numeric and negative shared-string indices if the focused smoke remains small.
- Preserve valid shared-string lookup and inline-string handle reading.
- Re-read current source/test after SHA-guarded integration and preserve concurrent history.
- Source/smoke review only; no .NET or BricsCAD runtime PASS unless actually executed.

## Coordination

Recent current-main searches for `XlsxHandleReader` and `shared string index` returned no active owner. This claim is limited to the malformed shared-string reference fail-open path.

## Completion condition

Completed only when malformed shared-string references fail closed, focused regression source is present on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
