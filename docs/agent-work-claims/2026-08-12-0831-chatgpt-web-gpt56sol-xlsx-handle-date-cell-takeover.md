# Work claim — XLSX Handle reader date-cell takeover

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-date-cell-takeover-20260812-0831`
- Registered: `2026-08-12T08:31:00+07:00`
- Baseline main SHA: `327fa755aec34fc0e7e0df610fef2b3844108f7c`
- Priority: P2 owner-coordinated completion of an unfinished remote-safe XLSX Handle lane

## Reserved scope

Finish the already-demonstrated XLSX Handle Date-cell semantic defect after the repository owner requested `continue all` and asked that all remaining unfinished changes be committed/pushed to `main`. Preserve SpreadsheetML `t="d"` as non-Handle semantic input before explicit CAD Handle and legacy `$decimal` heuristics, reusing the focused regression source already present on `main`.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleDateCellSmoke.cs` (existing regression; change only if current source proves it insufficient)
- `docs/agent-work-claims/2026-08-12-0831-chatgpt-web-gpt56sol-xlsx-handle-date-cell-takeover.md`

## Excluded scope

- No changes to shared-string, inline-string, Boolean, error, formula-string, default/numeric or worksheet/package semantics beyond the typed Date distinction required by the existing regression.
- No generalized date parser or Excel serial-date interpretation.
- No XLSX exporter, BLT/ED2 business-rule, persistence, UI/native BricsCAD/runtime, build/release or GitHub Actions work.

## Validation plan

- Reuse `XlsxHandleDateCellSmoke`: typed Date `1A` under exact Handle header must be rejected rather than accepted as hex Handle `1A`.
- Typed Date `$123` in unrelated data must not activate legacy decimal fallback.
- Default/numeric Handle cell behavior must remain unchanged.
- Re-read the exact integrated source and regression from current `main` and inspect the implementation commit diff.
- Do not claim local .NET/BricsCAD execution or GitHub Actions results unless actually run.

## Coordination

Owner-coordinated takeover of `docs/agent-work-claims/2026-08-12-0822-gpt56sol-xlsx-handle-date-cell.md`, which was explicitly marked `RELEASED` at commit `327fa755aec34fc0e7e0df610fef2b3844108f7c`. The original claim commit `0662bb4f25e76d70be80cecc0ea4c781c9bf0af5` and regression commit `bc72d58f6787d6f59d226ed9c986fe1504a6d5d0` are already on `main`; no source fix or completion commit existed at takeover. Neighboring XLSX Handle semantics remain outside this reservation.

## Completion condition

Completed when current `main` contains the narrowly scoped Date-cell semantic fix, the existing focused regression remains present and aligned, exact integration SHA/evidence is recorded here, and this claim is marked `COMPLETED`.
