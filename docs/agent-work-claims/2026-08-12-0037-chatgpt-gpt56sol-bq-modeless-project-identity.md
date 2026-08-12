# Work claim — BQ modeless project identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-bq-modeless-project-identity`
- Registered: `2026-08-12T00:37:17+07:00`
- Last Updated: `2026-08-12T00:52:00+07:00`
- Baseline main SHA: `987f3da5230636e192d7283d71198e3660a23c99`
- Priority: evidence-driven stale modeless mutation gap found during owner-requested continue-all audit
- Task Key: `BQ-MODELESS-PROJECT-IDENTITY`

## Confirmed defect

`QuantitySummaryWindow` was source-DWG-bound but did not retain the semantic `ProjectId` represented by the rows/window. `EnsureCurrentProject(...)` only proved that some project currently existed for the same DWG, while `PersistColumnPreferences()` then bound and mutated that current canonical project.

This preserved the intended same-project reload rebind, but it also allowed an already-open BQ window to write its visible-column preference into a different replacement project loaded into the same DWG. That was inconsistent with the modeless exact-project freshness boundary already enforced for other mutating manager windows and with the documented requirement that modeless writes rebind canonical same-ProjectId state or fail closed.

## Implemented scope

`QuantitySummaryWindow` now captures the current existing `ProjectId` during synchronous construction before `InitializeComponent()` and retains only that stable string identity, not a mutable `ProjectState`. The current `QS3DBQ` launcher recalculates the initial rows and constructs the window synchronously with no prompt/await/modeless boundary between those two statements, so `Commands.cs` remains untouched.

Every callback that claims a current project now resolves existing state and verifies the same `ProjectId`. The canonical column-preference write also verifies project identity after `ExistingProjectMutationContext.TryGet(...)` and before rollback snapshot / metadata / `Touch()`. Same-`ProjectId` reload therefore remains accepted while a different replacement project in the same DWG fails closed.

## Committed evidence

- Source fix: `c11e4ded41d0e49541f5d5fb3bb467a5d9c0953e` — `fix(bq): bind modeless callbacks to project identity`
- Focused regression gate: `c6d7f7ee9a6ef4a0ad3583c5ae9e12e32111c6ce` — `test(bq): guard modeless project identity`
- Source blob re-read on later moving-main snapshots, including `de02fb0253f9caeeddf312a76ab93817ac161562`, still contains stable `_projectId` capture and canonical preference-write identity verification.
- Preflight blob re-read on `de02fb0253f9caeeddf312a76ab93817ac161562` still requires constructor identity capture, same-ProjectId equality, exact mutation ordering, existing-project-only behavior, and forbids a retained mutable project field.
- No GitHub Actions were dispatched. No BricsCAD V25/local runtime execution or qualification is claimed by this remote batch.

## Preserved behavior / exclusions

- Same-`ProjectId` reload rebind remains valid; no stale mutable `ProjectState` reference was restored.
- `Commands.cs`, BQ native Table placement/refresh, quantity arithmetic/report builders, ED2 implementation, unit-resolution work, and detail viewport-reveal behavior were not modified by this lane.
- Existing source-DWG active-document guard, rollback, recalculation, locate, export, and current BQ row-freshness contracts remain in place.
- No concurrent ACTIVE claim was overwritten and no force-push was used.

## Completion condition

Satisfied: an already-open BQ modeless window can rebind after a same-project reload but cannot mutate or operate as current against a different semantic project loaded into the same DWG, with focused static regression evidence committed.