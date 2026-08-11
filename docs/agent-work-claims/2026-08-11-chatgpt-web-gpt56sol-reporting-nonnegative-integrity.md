# Work claim — Core reporting non-negative quantity integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-reporting-nonnegative-integrity`
- Registered: `2026-08-11T21:20:00+07:00`
- Baseline: newest `main` at claim branch creation
- Priority: prevent negative physical quantity magnitudes from reaching legacy BQ, project-backed BQ/ED2, or aggregate totals as plausible report output.

## Confirmed defect

`QuantityReportMath.Finite(...)` rejects only NaN/Infinity. Legacy `QuantityReportBuilder.Group(...)` and `QuantityReportTotals.FromRows(...)` rely on `QuantityReportMath.Add(...)`, so negative physical magnitudes can be accumulated. `ProjectQuantityReportBuilder.Q(...)` also only calls `Finite(...)`, allowing negative stored Length/Area/Volume/Formwork/Perimeter/Deduction/Net quantities into BQ/ED2. Specialized schedule builders already reject negative physical quantities, so the generic reporting paths are inconsistent.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityReportMath.cs`
- `src/QS3D.Core/Reporting/QuantityReportBuilder.cs`
- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- `src/QS3D.Core/Reporting/QuantityReportTotals.cs`
- `tests/QS3D.Core.SmokeTests/LegacyQuantityReportIdentitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectQuantitySmoke.cs`
- this claim file for close-out

## Intended contract

- add a focused reusable non-negative finite validator without changing generic signed arithmetic semantics elsewhere;
- reject negative physical quantity magnitudes before they are accumulated in legacy grouping and totals;
- make project-backed quantity reads reject negative stored/fallback physical quantities;
- specifically reject a negative derived legacy net (`GrossConcreteM3 - DeductionM3 < 0`) rather than publishing it;
- preserve zero values, positive values, duplicate-ID guards, provenance, grouping, density/mass rules and overflow behavior.

## Explicit exclusions

- No `ElementInstance` setters/domain mutation changes.
- No schedule builder/XLSX exporter/UI/BQ window/Right Panel/quantity-settings changes.
- No persistence/session/core-mutation, Room Auto, Ribbon, updater, geometry, rebar or release changes.
- No `SmokeTestRegistration.cs` edit; both target smoke classes are already registered.
- No GitHub Actions/build/release dispatch and no native BricsCAD V25/WPF runtime PASS claim.

## Validation plan

- re-fetch all reserved files from `main` after this claim is merged;
- add focused smoke cases for negative legacy physical quantity, negative legacy derived net, negative totals row and negative project-backed quantity;
- confirm existing valid zero/positive reporting behavior remains unchanged;
- compare newest `main` immediately before integration and structurally rebase only the reviewed target blobs if concurrent work is disjoint.

## Coordination

The active Core mutation atomicity claim explicitly excludes Core reporting. Current XLSX, Ribbon, UI, updater and quantity-settings lanes reserve disjoint surfaces. The preceding legacy reporting null/provenance claims are completed and released these reporting files.

## Completion condition

Generic reporting paths fail closed on negative physical quantity magnitudes with deterministic regression coverage, changes are integrated onto current `main` without overwriting concurrent work, and this claim is closed with exact SHAs and truthful validation scope.
