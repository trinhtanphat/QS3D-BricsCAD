# Work claim — XLSX Handle date semantic regression

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-date-regression-20260812-1208`
- Registered: `2026-08-12T12:08:00+07:00`
- Baseline main SHA: `3c73f7fc4413e55c04796167b85cf410bb86f1de`
- Priority: P1 completed-contract regression

## Confirmed regression

The completed date-cell takeover (`de79c116f1eb10f60780148d9cc040e70a92d5aa`, closed by `295cfaf9f81beae16df8ed6f804593aeb019b150`) established that SpreadsheetML `t="d"` values are preserved as unmistakably non-Handle text (`[Date] <raw>`) before Handle and legacy `$decimal` heuristics.

Current `XlsxHandleReader.ReadCells(...)` has regressed to `type == "e" || type == "d" => UnsupportedCellSentinel`, while retaining a later `if (type == "d") value = "[Date] " + value` branch that is therefore unreachable. This silently discards the completed Date semantic distinction and leaves dead contradictory code.

## Reserved scope

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- one focused new Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- XLSX error cells (`t="e"`) remain unsupported sentinel values.
- XLSX date cells (`t="d"`) reach the existing `[Date] <raw>` branch.
- A Date value under an explicit CAD Handle header still fails as an invalid Handle rather than being synthesized into a Handle.
- A Date cell containing `$decimal` text still cannot activate legacy Handle fallback.
- Existing numeric/shared-string/Boolean/inline-string and modern identity behavior remain unchanged.

## Validation boundary

Focused source-safe regression + exact readback only. No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed without execution.
