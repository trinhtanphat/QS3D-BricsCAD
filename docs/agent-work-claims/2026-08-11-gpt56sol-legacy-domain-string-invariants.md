# Agent Work Claim — Legacy domain string invariants

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11` (UTC+7)
- Baseline main SHA: `3aa1670dd314f4c41663e80671e45cea7b114724`

## Confirmed defect

The legacy reporting domain models normalize required/defaulted strings during construction but expose unguarded public setters afterward:

- `FamilyDefinition.Name` is required and trimmed by the constructor, but later assignments can set null, whitespace or an untrimmed name.
- `FamilyDefinition.Material` maps blank constructor input to `Khác` and trims accepted input, but later assignments can set null, whitespace or an untrimmed material.
- `ElementInstance.Floor` maps blank constructor input to `Nền 0.00` and trims accepted input, but later assignments can set null, whitespace or an untrimmed floor.

Those post-construction mutations can violate the same identity/grouping invariants that the constructors establish. `QuantityReportBuilder` consumes these properties as grouping/provenance values, so malformed mutations can produce inconsistent legacy report identity even though reporting now has separate null/non-negative guards.

## Reserved scope

- `src/QS3D.Core/Domain/FamilyDefinition.cs` — `Name` and `Material` setter invariants only.
- `src/QS3D.Core/Domain/ElementInstance.cs` — `Floor` setter invariant only.
- `tests/QS3D.Core.SmokeTests/LegacyDomainStringInvariantSmoke.cs` — focused regression coverage.
- `tests/QS3D.Core.SmokeTests/LegacyDomainStringInvariantSmokeRegistration.cs` — registration for that smoke only.
- this claim file for close-out evidence.

## Intended contract

- Preserve existing constructor compatibility exactly: blank material still defaults to `Khác`; blank floor still defaults to `Nền 0.00`; blank family name remains rejected.
- Normalize accepted post-construction assignments with `Trim()`.
- Reject null/blank post-construction family names without changing the previously valid value.
- For post-construction material/floor assignments, preserve constructor semantics by mapping null/blank to their existing defaults and trim accepted values.
- Do not change category validation, quantity arithmetic, source-handle behavior, reporting grouping implementation, persistence schema or project-backed domain models.

## Coordination / validation boundary

- The active Core mutation-atomicity claim is focused on QSDB/`ProjectSession` persistence/session surfaces and does not reserve these legacy model files.
- Completed legacy reporting null/material/non-negative claims explicitly left `FamilyDefinition`/`ElementInstance` mutation semantics out of scope.
- Re-read current `main` and exact target blobs immediately before each write; never overwrite a concurrent change.
- Add deterministic Core smoke coverage; do not dispatch GitHub Actions.
- No BricsCAD V25/native Windows runtime PASS is claimed by this remote lane.
