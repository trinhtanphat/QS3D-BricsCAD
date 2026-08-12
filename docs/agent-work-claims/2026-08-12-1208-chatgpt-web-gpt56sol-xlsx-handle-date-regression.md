# Work claim — XLSX Handle date semantic regression

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-date-regression-20260812-1208`
- Registered: `2026-08-12T12:08:00+07:00`
- Completed: `2026-08-12T12:12:00+07:00`
- Baseline main SHA: `3c73f7fc4413e55c04796167b85cf410bb86f1de`
- Priority: P1 completed-contract regression

## Confirmed regression

The completed date-cell takeover (`de79c116f1eb10f60780148d9cc040e70a92d5aa`, closed by `295cfaf9f81beae16df8ed6f804593aeb019b150`) established that SpreadsheetML `t="d"` values are preserved as unmistakably non-Handle text (`[Date] <raw>`) before Handle and legacy `$decimal` heuristics.

`XlsxHandleReader.ReadCells(...)` had regressed to `type == "e" || type == "d" => UnsupportedCellSentinel`, while retaining a later `if (type == "d") value = "[Date] " + value` branch that was therefore unreachable.

## Implemented contract

- XLSX error cells (`t="e"`) remain unsupported sentinel values.
- XLSX date cells (`t="d"`) now reach the existing `[Date] <raw>` branch again.
- A Date value under an explicit CAD Handle header still fails as an invalid Handle rather than being synthesized into a Handle.
- Existing numeric/shared-string/Boolean/inline-string and modern identity behavior remain unchanged.

## Integration evidence

- Claim: `1e33be8e9dc238cdf4b4382d41537965cb1b1e27`
- Source fix: `2b36f68361ca5f993917a2978e52da5be00a8a81`
- Focused regression: `72b75acb736b67b1585be3fb64fb76f3830d9a16`
- Exact readback confirmed source blob `f8ed00a6217f2aa288af82acc3499b2cb4f013c3` keeps only `t="e"` in the sentinel branch and routes `t="d"` through `[Date]`.
- Exact readback confirmed focused smoke blob `a548dffc8a0cb940e45703aa98c894e30bd35e53` tests Date-tag rejection and unchanged error-cell rejection.

## Validation boundary

Focused source-safe regression + exact readback only. No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed without execution.
