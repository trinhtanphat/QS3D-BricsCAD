# Work claim — XLSX Handle reader shared-string index validation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-shared-string-index-20260812-0720`
- Registered: `2026-08-12T07:20:00+07:00`
- Baseline main SHA: `7cbbb0a6f5fe772ca83c4f0fc2aa4211863e4667`
- Priority: P2 evidence-driven remote-safe XLSX input-integrity hardening

## Confirmed defect

`XlsxHandleReader.ReadCells(...)` dereferenced a cell with `t="s"` only when `<v>` parsed to an in-range shared-string index. Otherwise it left the raw `<v>` text as the cell value, allowing malformed shared-string XML in a CAD Handle column to be interpreted as a valid-looking hexadecimal handle.

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

## Validation implemented

- `t="s"` now always requires a numeric, non-negative, in-range index; invalid references throw `InvalidDataException` instead of preserving raw `<v>` text.
- Focused smoke creates minimal XLSX ZIPs and covers a valid index plus out-of-range, non-numeric, negative and missing index values.
- Source commit readback confirms the implementation diff is limited to strict shared-string reference validation.
- The regression commit remains an ancestor of current `main`; subsequent commit comparison showed no overlap with the reader/smoke files.

## Integration commits

- Claim: `ccb12c9e548263c2db314029e31b2172c59f0293`
- Source fix: `17d33821aa1d8a8ee3335cea7e424fbdf6c6b298`
- Focused smoke: `450a0d41be44e80e332f8c7b5c2e2eab999cb8a4`

## Validation boundary

Remote source/smoke review only. No .NET build, BricsCAD V25/V26 runtime qualification, private-DWG/native execution or GitHub Actions run is claimed by this session.

## Coordination

Recent current-main searches for `XlsxHandleReader` and `shared string index` returned no active owner before registration. This claim remained limited to the malformed shared-string reference fail-open path.

## Completion condition

Completed: malformed shared-string references fail closed, focused regression source is present on current `main`, and exact integration SHAs are recorded above.
