# Work claim — legacy reporting material grouping integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-legacy-reporting-material-grouping`
- Registered: `2026-08-11T21:31:00+07:00`
- Baseline: newest `main` at claim branch creation
- Priority: prevent legacy quantity rows from merging same-name/category families that carry different materials.

## Confirmed defect

`QuantityReportBuilder.Group(IEnumerable<ElementInstance>)` grouped by Floor + Family.Category + Family.Name only. `FamilyDefinition.Material` was omitted from both the grouping key and the produced `QuantityReportRow.Material`. Two same-floor families with the same display name/category but different materials could therefore collapse into one plausible row and lose material provenance. Project-backed BQ already includes effective material in its grouping key.

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

## Completion

- Claim-only PR: `#467`, squash merge on `main`: `0a8900d78705a4ab51aac14d9eab0e78fda6eeae`.
- Implementation PR: `#474` — `fix(reporting): preserve legacy material grouping`.
- Reviewed implementation head before squash: `05839db0702f2d31b8d920c4dff6a89914fa179a`.
- Squash merge on `main`: `0ba91e427c1688c24e675542d8ae125d19ce69c1`.
- Legacy grouping key now includes normalized `FamilyDefinition.Material`, and the produced `QuantityReportRow.Material` retains that provenance.
- Case/outer-whitespace-equivalent materials remain one case-insensitive group; genuinely different materials are separated.
- The already-registered legacy smoke covers same-name/category Concrete vs Steel separation and case-equivalent Concrete grouping.
- Existing duplicate-ID/null/non-negative/provenance/totals checks remain present.
- Final implementation PR diff: 2 files / 26 additions / 2 deletions.
- Concurrent-main comparison before integration showed no overlap with the two reserved files.
- GitHub Actions/build/release were not dispatched.
- No native BricsCAD V25/WPF runtime PASS is claimed.

## Completion condition

Satisfied by PR `#474` and merge `0ba91e427c1688c24e675542d8ae125d19ce69c1`: legacy BQ material provenance now participates in grouping identity, different materials cannot silently merge, and focused regression coverage was integrated without overwriting concurrent work.
