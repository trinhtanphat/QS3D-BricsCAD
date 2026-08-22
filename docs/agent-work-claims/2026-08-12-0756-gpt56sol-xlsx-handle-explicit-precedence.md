# Work claim — XLSX Handle reader explicit-handle precedence

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-explicit-precedence-20260812-0756`
- Registered: `2026-08-12T07:56:00+07:00`
- Baseline main SHA: `9071c380d8cfe76bc43b02dc10c084d28fd0283c`
- Priority: P2 evidence-driven remote-safe XLSX lookup correctness

## Confirmed defect

`XlsxHandleReader.ReadHandleLookup(...)` parsed legacy `$decimal` handles from every target-row cell before it parsed the explicitly discovered CAD Handle column. In a non-modern workbook with a clear `CAD Handle (hex)` header/value plus an unrelated cell such as `$123`, the fallback returned hexadecimal `7B` and silently ignored the explicit Handle value.

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

## Validation implemented

- Legacy fallback is now gated by `handleColumns.Count == 0`, so a discovered explicit Handle column wins over unrelated `$decimal` cells.
- Focused smoke proves `CAD Handle (hex)=1A` plus an unrelated `$123` cell returns `1A` with `UsesLegacyDecimalHandles=false`.
- A metadata-free row with no Handle header and `$123` still returns hexadecimal `7B` with `UsesLegacyDecimalHandles=true`.
- Source commit readback confirms the implementation diff is exactly one precedence condition.
- Regression commit remains an ancestor of current `main`; the only subsequent commit touched an unrelated Floor generated-identity claim.

## Integration commits

- Claim: `d49b796820cb371e5e0388f272f21f5fd6926e8b`
- Source fix: `b37b859138f2bb779babe5aa2f0818f4f4fb725e`
- Focused smoke: `6297b9caf24a8dd31cd204a2a8166f8a30000713`

## Validation boundary

Remote source/smoke review only. No .NET build, BricsCAD V25/V26 runtime qualification, private-DWG/native execution or GitHub Actions run is claimed by this session.

## Coordination

No active decimal-handle precedence owner was found before registration. BLT compatibility remains supported; this claim fixed only precedence when a semantic Handle header is already present.

## Completion condition

Completed: explicit Handle columns cannot be overridden by unrelated `$decimal` cells, legacy fallback remains covered without a Handle header, focused regression source is on current `main`, and exact integration SHAs are recorded above.
