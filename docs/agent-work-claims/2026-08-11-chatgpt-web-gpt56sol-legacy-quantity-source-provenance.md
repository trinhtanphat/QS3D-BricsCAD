# Work claim — legacy quantity source provenance normalization

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-legacy-quantity-source-provenance`
- Registered: `2026-08-11T20:52:00+07:00`
- Baseline main SHA: `30e15375da1c85a7770d9fb2467deb3a57257bad`
- Priority: prevent duplicate CAD provenance handles in the legacy quantity report when equivalent handles differ only by surrounding whitespace or case.

## Confirmed defect

`QuantityReportBuilder.Group` checked `row.SourceHandles.Contains(handle, StringComparer.OrdinalIgnoreCase)` before trimming `handle`, then stored `handle.Trim()`. As a result, existing `"AA"` followed by `" AA "` passed the pre-trim duplicate check and stored a second `"AA"`. The row therefore exposed duplicate provenance despite equivalent CAD identity.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityReportBuilder.cs`
- `tests/QS3D.Core.SmokeTests/LegacyQuantityReportIdentitySmoke.cs`
- reuse the completed `ReportingRowProvenance` helper without changing its contract unless a proven defect requires it
- this claim file for close-out

## Intended change

Replace the legacy ad-hoc source-handle loop with the shared reporting provenance helper already used by Core schedules. Preserve element-ID fail-closed behavior, grouping order and all quantity math. Extend the existing registered legacy smoke with trimming/case-deduplication coverage and deterministic first-seen handle spelling.

## Explicit exclusions

- No ProjectQuantityReportBuilder, schedule-builder, BQ/WPF/Right Panel, persistence/mutation, Room Auto, Ribbon or geometry changes.
- No quantity arithmetic or grouping-key changes.
- No GitHub Actions/build/release dispatch and no native BricsCAD V25/WPF runtime PASS claim.

## Validation plan

- Re-fetch target files after claim publication before source write.
- Verify only the provenance loop changes in `QuantityReportBuilder.cs`.
- Regression should prove `"AA"`, `" aa "`, blank and `"Bb"` become exactly `AA`, `Bb` in first-seen order.
- Re-read latest `main` and compare target surfaces before integration.

## Coordination

The earlier repository-audit reporting-identity claim is `COMPLETE` and explicitly released these paths. Current BQ/UI/Core mutation/Room Auto claims do not reserve these two files. This lane reuses the just-completed schedule provenance normalization rather than creating a second policy.

## Completion

- PR: `#454` — `fix(reporting): normalize legacy quantity source provenance`
- Reviewed feature head before squash: `4ea41f83a8b193e4ed2e200e87b649cb91fe6aad`
- Squash merge on `main`: `22d2aac87404bd7596c83a1dbc3c0638f2c40ae1`
- `QuantityReportBuilder.Group` now calls the shared `ReportingRowProvenance.AppendSourceHandles(...)` contract instead of its pre-trim ad-hoc duplicate check.
- The existing registered `LegacyQuantityReportIdentitySmoke` now proves `"AA"`, `" aa "`, blank and `"Bb"` produce exactly `AA`, `Bb` in deterministic first-seen order.
- Final PR diff: 2 files / 6 additions / 3 deletions.
- Repeated current-main comparisons confirmed concurrent updater/UI/claim work did not touch either reserved file before integration.
- Element-ID fail-closed handling, quantity arithmetic and grouping keys were unchanged.
- GitHub Actions/build/release were not dispatched.
- No native BricsCAD V25/WPF runtime PASS is claimed.

## Completion condition

Satisfied by PR `#454` and merge `22d2aac87404bd7596c83a1dbc3c0638f2c40ae1`: legacy quantity reporting now uses the shared normalized source-provenance contract, the existing smoke guards the regression, and the change was merged without overwriting concurrent work.
