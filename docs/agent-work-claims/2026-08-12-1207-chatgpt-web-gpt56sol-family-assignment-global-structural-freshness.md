# Work claim — Family assignment global structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-assignment-global-structural-freshness`
- Registered: `2026-08-12T12:07:00+07:00`
- Baseline main SHA: `6f815d617dfc9b686176f32400931bf1ee49046d`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-FAMILY-ASSIGNMENT-GLOBAL-STRUCTURAL-FRESHNESS`

## Confirmed defect

`ProjectFamilyService.Assign(...)` globally validates Family and project-element identities before caller-owned lazy target enumeration, then re-resolves only the target Family and selected target element IDs afterward. Because `ProjectState.Families` and `ProjectState.Elements` are publicly mutable lists, a lazy target enumerable can directly insert an unrelated duplicate Family or element identity without calling `project.Touch()`. `ChangeVersion` remains unchanged and target-only lookups do not observe the unrelated duplicate pair, so assignment can continue on globally identity-invalid state.

This violates completed contracts that Family target operations reject unrelated duplicate Family identities and that Family assignment structural freshness rejects duplicate project identities introduced during caller enumeration before planning/mutation.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` — post-enumeration global Family/element identity revalidation only
- `tests/QS3D.Core.SmokeTests/FamilyAssignStructuralFreshnessSmoke.cs` — focused regression extension only
- this claim file for close-out

## Intended contract

- Preserve existing revision freshness guards and pre-enumeration validation.
- After caller target enumeration, revalidate global Family uniqueness and the complete project element identity collection before target ownership/category checks and assignment planning.
- Direct no-`Touch()` insertion of an unrelated duplicate Family ID or unrelated duplicate semantic element ID must fail before `project.Touch()`, `FamilyId`/inherited-property mutation, or dirty/timestamp mutation.
- Preserve target removal/replacement/category checks, canonical/no-op assignment, inheritance semantics, and unrelated Family operations.

## Excluded scope

- No Zone/Floor/Grid changes, no global `ProjectState` collection redesign, no activation/property propagation redesign, no CAD/UI/runtime work, and no concurrent Grid/Recognition/Selection/Curtain/Auto Room/Interchange work.
- No force-push, GitHub Actions dispatch, full-build/executable-smoke PASS, or licensed BricsCAD V25/V26 runtime qualification claim.

## Validation plan

Re-fetch exact source and existing auto-registered structural smoke after claim registration. Add only post-enumeration global validation plus regressions for unrelated duplicate Family and element insertion, read back integrated source/test, close this claim with exact SHAs, and verify completion ancestry on current `main`.