# Work claim — Family assignment global structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-assignment-global-structural-freshness`
- Registered: `2026-08-12T12:07:00+07:00`
- Baseline main SHA: `6f815d617dfc9b686176f32400931bf1ee49046d`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-FAMILY-ASSIGNMENT-GLOBAL-STRUCTURAL-FRESHNESS`

## Confirmed defect

`ProjectFamilyService.Assign(...)` globally validated Family and project-element identities before caller-owned lazy target enumeration, then re-resolved only the target Family and selected target element IDs afterward. Because `ProjectState.Families` and `ProjectState.Elements` are publicly mutable lists, a lazy target enumerable could directly insert an unrelated duplicate Family or element identity without calling `project.Touch()`. `ChangeVersion` remained unchanged and target-only lookups did not observe the unrelated duplicate pair, allowing assignment to continue on globally identity-invalid state.

This violated completed contracts that Family target operations reject unrelated duplicate Family identities and that Family assignment structural freshness rejects duplicate project identities introduced during caller enumeration before planning/mutation.

## Completed implementation

- Preserved existing revision freshness guards and pre-enumeration validation.
- `RequireCurrentAssignmentOwnership(...)` now re-runs global Family identity validation after caller target enumeration.
- The same boundary rebuilds a case-insensitive map from the complete current `project.Elements` collection, rejecting null entries and any duplicate semantic element ID before target ownership/category checks.
- Target Family object identity, target element object identity and category compatibility remain revalidated afterward.
- Existing target removal/replacement behavior, canonical/no-op assignment, inheritance semantics and unrelated Family operations remain unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/FamilyAssignStructuralFreshnessSmoke.cs` remains auto-registered with `ModuleInitializer` and now covers four no-revision structural mutations:

1. selected target element removed during lazy enumeration;
2. target Family removed during lazy enumeration;
3. an unrelated duplicate Family identity inserted during lazy enumeration;
4. an unrelated duplicate semantic element identity inserted during lazy enumeration.

The duplicate cases require `InvalidOperationException` before assignment mutation and verify unchanged `ProjectState.ChangeVersion`, target `FamilyId`, inherited target properties, dirty flags and timestamp while retaining the deliberate external duplicate insertion.

## Integration evidence

- Claim commit: `f321be98d10d10cae2c1259011bfb4d0aee552c5`
- Production fix: `794a2dcd423d78c49732f1ce73c4ed99825f1818` (`fix(family): revalidate global assignment identity`)
- Focused regression: `3c73f7fc4413e55c04796167b85cf410bb86f1de` (`test(family): guard global structural freshness`)
- Integrated source read-back confirms global Family validation and complete current element-map validation occur post-enumeration before target identity/category checks.
- Integrated smoke read-back confirms both existing removal cases and both unrelated duplicate-insertion cases.

## Excluded scope / validation boundary

- No Zone/Floor/Grid changes, no global `ProjectState` collection redesign, no activation/property propagation redesign, no CAD/UI/runtime work, and no concurrent Grid/Recognition/Selection/Curtain/Auto Room/Interchange changes.
- No force-push and no GitHub Actions dispatch.
- No full-build/executable-smoke PASS or licensed BricsCAD V25/V26 runtime qualification is claimed from this connector-only lane; validation is repository integration/read-back plus focused regression source coverage.