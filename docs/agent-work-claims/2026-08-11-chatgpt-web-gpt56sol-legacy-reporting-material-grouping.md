# Work claim — legacy reporting material grouping integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-legacy-reporting-material-grouping`
- Registered: `2026-08-11T21:31:00+07:00`
- Baseline: newest `main` at claim branch creation
- Priority: prevent legacy quantity rows from merging same-name/category families that carry different materials.

## Confirmed defect

`QuantityReportBuilder.Group(IEnumerable<ElementInstance>)` groups by Floor + Family.Category + Family.Name only. `FamilyDefinition.Material` is omitted from both the grouping key and the produced `QuantityReportRow.Material`. Two same-floor families with the same display name/category but different materials therefore collapse into one plausible row and lose material provenance. Project-backed BQ already includes effective material in its grouping key.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityReportBuilder.cs`
- `tests/QS3D.Core.SmokeTests/LegacyQuantityReportIdentitySmoke.cs`
- this claim file for close-out

## Intended contract

- include the normalized effective legacy family material in the grouping key;
- populate `QuantityReportRow.Material` for every legacy row;
- preserve case-insensitive grouping behavior, first-seen row order, duplicate-ID/null/non-negative guards, source-handle provenance and quantity arithmetic;
- same material differing only by case/outer whitespace remains one group; genuinely different material remains separate.

## Explicit exclusions

- No `FamilyDefinition`/`ElementInstance` mutation or setter changes.
- No ProjectQuantityReportBuilder/schedule/XLSX/UI/quantity-settings/persistence/geometry/rebar/release changes.
- No `SmokeTestRegistration.cs` edit; the legacy smoke is already registered.
- No GitHub Actions/build/release dispatch and no native BricsCAD V25/WPF runtime PASS claim.

## Validation plan

- re-fetch the two reserved files from `main` after this claim is merged;
- add deterministic regression coverage with same-name/category families using distinct materials and a case/whitespace-equivalent material;
- confirm valid existing legacy totals/provenance remain unchanged;
- compare newest `main` before integration and structurally rebase only reviewed blobs when concurrent work is disjoint.

## Coordination

The active Quantity Settings UI lane explicitly excludes Core quantity arithmetic. Core mutation atomicity excludes reporting. Previous reporting identity/provenance/null/non-negative claims are completed and released these two files.

## Completion condition

Legacy BQ material provenance participates in row grouping, different materials cannot silently merge, focused smoke coverage is merged to current `main`, and this claim is closed with exact SHAs and truthful validation scope.
