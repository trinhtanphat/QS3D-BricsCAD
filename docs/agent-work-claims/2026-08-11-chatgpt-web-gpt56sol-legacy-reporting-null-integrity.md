# Work claim — legacy reporting null-input integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-legacy-reporting-null-integrity`
- Registered: `2026-08-11T21:07:00+07:00`
- Baseline main SHA: `cc38e41349bcb113367670feafbd17238220586c`
- Priority: prevent legacy reporting APIs from silently dropping null elements/rows and returning plausible-but-incomplete totals.

## Confirmed defect

`QuantityReportBuilder.Group(IEnumerable<ElementInstance>)` currently executes `if (element == null) continue;`, and `QuantityReportTotals.FromRows(IEnumerable<QuantityReportRow>)` executes `if (row == null) continue;`. A malformed caller sequence can therefore lose one or more report records without any error while still receiving a valid-looking grouped report or total.

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

## Completion condition

Both legacy reporting entry points fail closed on null members, the already-registered smoke guards the contract, changes are merged onto current `main` without overlap, and this claim is closed with exact SHAs and truthful validation scope.
