# Work claim — Core reporting non-negative quantity integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-reporting-nonnegative-integrity`
- Registered: `2026-08-11T21:20:00+07:00`
- Baseline: newest `main` at claim branch creation
- Priority: prevent negative physical quantity magnitudes from reaching legacy BQ, project-backed BQ/ED2, or aggregate totals as plausible report output.

## Confirmed defect

`QuantityReportMath.Finite(...)` rejected only NaN/Infinity. Legacy `QuantityReportBuilder.Group(...)` and `QuantityReportTotals.FromRows(...)` relied on `QuantityReportMath.Add(...)`, so negative physical magnitudes could be accumulated. `ProjectQuantityReportBuilder.Q(...)` also only called `Finite(...)`, allowing negative stored Length/Area/Volume/Formwork/Perimeter/Deduction/Net quantities into BQ/ED2. Specialized schedule builders already rejected negative physical quantities, so the generic reporting paths were inconsistent.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityReportMath.cs`
- `src/QS3D.Core/Reporting/QuantityReportBuilder.cs`
- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- `src/QS3D.Core/Reporting/QuantityReportTotals.cs`
- `tests/QS3D.Core.SmokeTests/LegacyQuantityReportIdentitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectQuantitySmoke.cs` (reviewed only; no edit required)
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
- No `SmokeTestRegistration.cs` edit; target smoke coverage was already registered.
- No GitHub Actions/build/release dispatch and no native BricsCAD V25/WPF runtime PASS claim.

## Validation plan

- re-fetch all reserved files from `main` after this claim is merged;
- add focused smoke cases for negative legacy physical quantity, negative legacy derived net, negative totals row and negative project-backed quantity;
- confirm existing valid zero/positive reporting behavior remains unchanged;
- compare newest `main` immediately before integration and structurally rebase only the reviewed target blobs if concurrent work is disjoint.

## Coordination

The active Core mutation atomicity claim explicitly excludes Core reporting. Current XLSX, Ribbon, UI, updater and quantity-settings lanes reserve disjoint surfaces. The preceding legacy reporting null/provenance claims are completed and released these reporting files.

## Completion

- Claim-only PR: `#462`, squash merge on `main`: `8fc958c3b67d7466928d7933451aca7a148b1a82`.
- Implementation PR: `#464` — `fix(reporting): reject negative physical quantities`.
- Reviewed implementation head before squash: `672a605c026006bfd9b40c782b3c4033ee479d1d`.
- Squash merge on `main`: `156f4c2d97ab727bacf7d9b2eba72d492e5a7088`.
- Added `QuantityReportMath.NonNegative(...)` while leaving generic `Add(...)` signed semantics unchanged.
- Legacy grouping now rejects negative physical quantities, including a negative derived `NetConcreteM3` when deduction exceeds gross.
- Project-backed BQ/ED2 quantity reads and fallback values now fail closed on negative physical magnitudes.
- Aggregate `QuantityReportTotals` now rejects negative row values with row/quantity labels.
- The already-registered `LegacyQuantityReportIdentitySmoke` covers negative legacy length, negative derived net, negative totals row and negative project-backed semantic quantity; existing valid grouping/totals checks remain.
- Final implementation PR diff: 5 files / 60 additions / 24 deletions.
- Concurrent `main` comparison before integration showed no overlap with the five implementation files.
- GitHub Actions/build/release were not dispatched.
- No native BricsCAD V25/WPF runtime PASS is claimed.

## Completion condition

Satisfied by PR `#464` and merge `156f4c2d97ab727bacf7d9b2eba72d492e5a7088`: generic reporting paths now fail closed on negative physical quantity magnitudes with deterministic regression coverage, without changing generic signed arithmetic or overwriting concurrent work.
