# Work claim — XLSX Handle reader Boolean cell semantics

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-boolean-cell-20260812-0805`
- Registered: `2026-08-12T08:05:00+07:00`
- Baseline main SHA: `ff0030d631379071b62d69ae2e238dcd5c5ce387`
- Priority: P2 evidence-driven remote-safe XLSX cell-semantics hardening

## Confirmed defect

`XlsxHandleReader.ReadCells(...)` left every non-inline/non-shared cell's raw `<v>` text unchanged. SpreadsheetML cells with `t="b"` are Boolean cells whose value `0`/`1` means FALSE/TRUE. In an explicitly discovered `CAD Handle (hex)` column, Boolean TRUE therefore produced raw text `1`, which `AddHexHandles(...)` accepted as CAD Handle `1`.

## Reserved scope

Honor Boolean cell semantics while reading values: require Boolean `<v>` to be exactly `0` or `1`, normalize those to `FALSE`/`TRUE`, and thereby prevent them from being interpreted as hexadecimal Handle tokens. Preserve shared strings, inline strings, numeric/default cells, formula string results and all package/identity guards.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleBooleanCellSmoke.cs`
- this claim file

## Excluded scope

- No generalized cell-type validator or date/formula policy redesign.
- No XLSX exporter changes or BLT/ED2 handle semantics changes beyond correct Boolean interpretation.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation implemented

- `t="b"><v>1</v>` and `t="b"><v>0</v>` are normalized to TRUE/FALSE, so the explicit Handle parser rejects them instead of synthesizing Handle values.
- Boolean cells whose value is not exactly `0` or `1` fail closed with `InvalidDataException` during cell reading.
- Focused smoke preserves the existing default/numeric Handle lexical path with `<v>26</v>` and no cell type.
- Source diff was re-read and is limited to the Boolean branch in `ReadCells(...)`.
- Smoke commit remains an ancestor of current `main`; subsequent commits touched only unrelated PhysicalOpening/WallJunction/BOM claims.

## Integration commits

- Claim: `1470d25aa3c57dfc0e7fc51870010755dc2f9fb5`
- Source fix: `63f33f0d20ef193221ebbc7d5c447ad12c897b6f`
- Focused smoke: `ccb9d4c0d992bfff487808ac6f5181df3b3e619a`

## Evidence

Microsoft Learn documents that SpreadsheetML Boolean cells use `t="b"` and values `0`/`1`, corresponding to FALSE/TRUE; Office restricts Boolean cell values accordingly.

## Validation boundary

Remote source/smoke review only. No .NET build, BricsCAD V25/V26 runtime qualification, private-DWG/native execution or GitHub Actions run is claimed by this session.

## Coordination

No active XLSX Handle Boolean-cell owner was found before registration. This claim remained limited to Boolean value semantics and did not reopen prior package/precedence work.

## Completion condition

Completed: Boolean cells cannot be synthesized into CAD Handles, malformed Boolean values fail closed, focused regression source is on current `main`, and exact integration SHAs are recorded above.
