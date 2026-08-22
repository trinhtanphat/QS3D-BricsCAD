# Work claim — BQ report Family-category integrity

- Status: `COMPLETED`
- Agent: `gpt56sol-quantity-report-family-category-integrity-20260814-0845`
- Baseline main SHA: `f38cc3464a11c62df31d50c186012a654b192e1f`
- Scope: `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`; `tests/QS3D.Core.SmokeTests/QuantityReportFamilyCategorySmoke.cs`; `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`.

## Confirmed defect

`ProjectFamilyService.Assign` already prevents assigning a Family whose category differs from the semantic element category, and `ModelHealthService` reports `FAMILY_CATEGORY_MISMATCH` for damaged/imported state. However, `ProjectQuantityReportBuilder` resolved an existing mismatched Family and then inherited its FamilyName/Material/Note/DensityKgM3. A damaged state such as a Slab referencing an ArchitecturalWall Family could therefore produce a plausible but semantically wrong BQ grouping and mass rather than failing visibly.

## Implemented

- Claim-only commit: `fbd7a2bf9062347105fd8ad7f0652c8960b92b0b`.
- Claim test-scope refinement before source/test writes: `499cbecc65116a2eee414f5bb3a4a95e3a01aeac`.
- Source fix: `676a0c6a6333d331f7bd121448f8502db71be553`.
  - after canonical Family resolution and before any Family-derived report data is read, an existing Family whose category differs from the element category now causes `InvalidOperationException` with element/family/category evidence;
  - missing-Family behavior is unchanged;
  - matching-Family inheritance remains unchanged.
- Focused regression: `402ad95b606a008ecc251485300a71d2e6344e7e`.
  - mismatched Family category fails for both grouped and detail BQ;
  - matching Slab Family still inherits Material/Density and computes mass (`2 m3 * 2400 kg/m3 = 4800 kg`).
- Smoke registration: `c2b285f754419f4eb468c4230afbd1983f457e70`.

## Validation actually executed

- Refreshed live `main`/recent commits and checked exact source scope before claim and before source write.
- Read `ProjectFamilyService` and `ModelHealthService` to confirm category mismatch is already an invalid/diagnostic state rather than a supported cross-category Family relation.
- Remote source commit readback shows only the two-line category guard was added to `ProjectQuantityReportBuilder`.
- Remote registry commit readback shows only `QuantityReportFamilyCategorySmoke.Run()` was added.
- Final source/test checkpoint was live `main` at `c2b285f754419f4eb468c4230afbd1983f457e70`.
- GitHub combined status exposes no attached statuses/checks for that SHA; no Actions were dispatched.
- This execution environment has no `dotnet`, `csc` or `mcs`, so no executable managed smoke/build is reported as PASS. No licensed BricsCAD/native validation was executed.

## Excluded scope

Family mutation/repair UI, Revision, persistence/schema, MAP/IFC, update/release UI and CAD host behavior are unchanged.

## Completion

`COMPLETED`: BQ/reporting now fails closed before a mismatched Family can contribute name/material/note/density/mass, focused regression is registered on remote `main`, concurrent work was preserved, and unavailable runtime/native gates are explicitly unclaimed.
