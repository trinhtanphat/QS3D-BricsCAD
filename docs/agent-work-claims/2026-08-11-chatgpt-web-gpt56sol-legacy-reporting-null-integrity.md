# Work claim — legacy reporting null-input integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-legacy-reporting-null-integrity`
- Registered: `2026-08-11T21:07:00+07:00`
- Baseline main SHA: `cc38e41349bcb113367670feafbd17238220586c`
- Priority: prevent legacy reporting APIs from silently dropping null elements/rows and returning plausible-but-incomplete totals.

## Confirmed defect

`QuantityReportBuilder.Group(IEnumerable<ElementInstance>)` executed `if (element == null) continue;`, and `QuantityReportTotals.FromRows(IEnumerable<QuantityReportRow>)` executed `if (row == null) continue;`. A malformed caller sequence could therefore lose one or more report records without any error while still receiving a valid-looking grouped report or total.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityReportBuilder.cs`
- `src/QS3D.Core/Reporting/QuantityReportTotals.cs`
- `tests/QS3D.Core.SmokeTests/LegacyQuantityReportIdentitySmoke.cs`
- this claim file for close-out

## Intended contract

- reject a null `ElementInstance` at the first encountered zero-based input index with `ArgumentException` bound to `elements`;
- reject a null `QuantityReportRow` at the first encountered zero-based input index with `ArgumentException` bound to `rows`;
- preserve all valid grouping order, duplicate-ID fail-closed behavior, normalized source provenance and checked quantity arithmetic;
- keep validation read-only/local to the report call with no mutation or filesystem side effects.

## Explicit exclusions

- No XLSX exporter changes; the active XLSX null-row claim owns those files.
- No ProjectQuantityReportBuilder, schedule builders/rows, BQ/WPF/Right Panel, persistence/mutation, Room Auto, Ribbon, updater, geometry or release changes.
- No `SmokeTestRegistration.cs` edit; the existing `LegacyQuantityReportIdentitySmoke` is already registered.
- No GitHub Actions/build/release dispatch and no native BricsCAD V25/WPF runtime PASS claim.

## Validation plan

- Re-fetch the three reserved files from current `main` after claim publication.
- Add deterministic smoke coverage proving null element/row inputs throw with the first bad index while valid rows still aggregate exactly as before.
- Review final diff and compare against newest `main` immediately before integration; do not overwrite concurrent work.

## Coordination

The previous legacy reporting provenance/identity claims are completed and released these paths. Current Ribbon, XLSX exporter, Workspace, Start Center, updater, BQ/Quantity UI, Core mutation and Room Auto lanes do not reserve the three files above.

## Completion

- Claim-only PR: `#458`, merged to `main` before substantive source work as `3abc5637ad65b81ee489b1ae8cf3c0198a95dd5c`.
- Implementation PR: `#460` — `fix(reporting): fail closed on null legacy report members`.
- Reviewed implementation head: `84325aa44ab618f10f1ebf68706540e50d1ea0d4`.
- Squash merge on `main`: `81205012d1255dd652830e45cb9b6e0281cd4173`.
- `QuantityReportBuilder.Group` now rejects a null element with its zero-based input index and `elements` parameter binding instead of silently skipping it.
- `QuantityReportTotals.FromRows` now rejects a null row with its zero-based input index and `rows` parameter binding instead of silently skipping it.
- The already-registered `LegacyQuantityReportIdentitySmoke` now covers both null-member contracts and confirms valid totals remain unchanged.
- Final implementation PR diff: 3 files / 35 additions / 2 deletions.
- Concurrent-main comparison before integration showed no changes to the three reserved files.
- GitHub Actions/build/release were not dispatched.
- No native BricsCAD V25/WPF runtime PASS is claimed.

## Completion condition

Satisfied by PR `#460` and merge `81205012d1255dd652830e45cb9b6e0281cd4173`: both legacy reporting entry points fail closed on null members, the existing smoke guards the behavior, and the implementation was merged without overwriting concurrent work.
