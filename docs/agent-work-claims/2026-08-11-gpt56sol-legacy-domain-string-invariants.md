# Agent Work Claim — Legacy domain string invariants

- Status: `RELEASED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11` (UTC+7)
- Released: `2026-08-11` (UTC+7)
- Baseline main SHA: `3aa1670dd314f4c41663e80671e45cea7b114724`

## Confirmed defect

The legacy reporting domain models normalized required/defaulted strings during construction but exposed unguarded public setters afterward:

- `FamilyDefinition.Name` was required and trimmed by the constructor, but later assignments could set null, whitespace or an untrimmed name.
- `FamilyDefinition.Material` mapped blank constructor input to `Khác` and trimmed accepted input, but later assignments could set null, whitespace or an untrimmed material.
- `ElementInstance.Floor` mapped blank constructor input to `Nền 0.00` and trimmed accepted input, but later assignments could set null, whitespace or an untrimmed floor.

Those post-construction mutations could violate the same identity/grouping invariants that the constructors established. `QuantityReportBuilder` consumes these properties as grouping/provenance values.

## Released scope

- `src/QS3D.Core/Domain/FamilyDefinition.cs` — `Name` and `Material` setter invariants only.
- `src/QS3D.Core/Domain/ElementInstance.cs` — `Floor` setter invariant only.
- `tests/QS3D.Core.SmokeTests/LegacyDomainStringInvariantSmoke.cs` — focused regression coverage.
- `tests/QS3D.Core.SmokeTests/LegacyDomainStringInvariantSmokeRegistration.cs` — smoke registration.
- this claim file.

## Completed changes

- Claim registered first on `main`: `e7112ad839aab62d3b3e620369527a3373423871`.
- `b8bb47f784e8f04838b0920d44ec8865ab4f5c7c` — hardened `FamilyDefinition`:
  - `Name` now rejects null/blank assignments and trims accepted values;
  - rejected names leave the previous valid value unchanged;
  - `Material` now trims accepted values and maps null/blank assignments to the existing `Khác` fallback;
  - constructor compatibility and category validation remain unchanged.
- `f9eef3bfcd7056a0e3ae465cc0d6cc70c8cac978` — hardened `ElementInstance.Floor`:
  - accepted assignments are trimmed;
  - null/blank assignments retain the constructor contract by mapping to `Nền 0.00`.
- `baf206195e7de1e8128a3fa7eb031aa21f774bdf` — added deterministic smoke coverage for constructor/setter normalization, fallback behavior and failed-name state preservation.
- `500d3fd31b05cb33cf51c347484d8befe96273ff` — registered the focused smoke via `ModuleInitializer` using the existing smoke-test pattern.

## Coordination / validation actually performed

- The active Core mutation-atomicity claim remains focused on QSDB/`ProjectSession` persistence/session surfaces and did not reserve these legacy model files.
- Completed legacy reporting null/material/non-negative claims explicitly left `FamilyDefinition`/`ElementInstance` mutation semantics out of scope.
- Exact target blobs were re-read after claim registration before the source writes.
- Both final source files were re-read from current `main` after implementation and contain the intended hardened setters.
- A concurrent-main race caused the first new-test create request to return `409`; the test did not exist, `main` was refreshed, and the create was retried without force push or overwrite.
- No GitHub Actions workflow or release was dispatched.
- The connector environment did not provide a usable local build/native BricsCAD runtime, so committed smoke coverage is not reported as an executed runtime PASS.

## Result

Legacy family/floor identity strings can no longer drift away from their constructor-established normalization/default contracts through later public setter mutation. Reporting can therefore consume those model values without seeing null/blank/untrimmed states introduced through these setters.
