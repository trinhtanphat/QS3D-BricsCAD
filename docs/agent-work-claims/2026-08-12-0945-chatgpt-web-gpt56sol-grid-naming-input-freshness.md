# Work claim — Grid naming input freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-naming-input-freshness`
- Registered: `2026-08-12T09:45:00+07:00`
- Completed: `2026-08-12`
- Baseline main SHA: `4647f0ec44e3af8f3e685e2c93a1430b7dd11dfd`
- Priority: P1 — fail-closed Core mutation freshness at a caller-controlled enumeration boundary.

## Confirmed defect

`GridNamingService.Renumber(ProjectState, IEnumerable<string>, GridNamingOptions?)` enumerated caller-controlled `orderedGridElementIds` before resolving targets and mutating Grid naming metadata without verifying that the project stayed at the same `ChangeVersion`. A lazy enumerable could mutate/touch the same `ProjectState` while yielding otherwise-valid Grid IDs; renumbering then continued and could write labels against stale assumptions.

## Reserved scope

- `src/QS3D.Core/Domain/GridNamingService.cs`
- focused Core smoke regression and registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-grid-naming-input-freshness.md`
- this claim file

## Implemented contract

- Capture `project.ChangeVersion` immediately before enumerating `orderedGridElementIds`.
- Keep the existing 2,000-item bounded enumeration and ID normalization unchanged.
- Immediately after enumeration, fail closed with `InvalidOperationException` if the version changed.
- Perform freshness rejection before empty/duplicate/options validation, project element resolution, planning, and all renumber mutations.
- Preserve stable-input behavior, label rules, collision checks, ordering, and no-op semantics.

## Evidence

- Claim: `3f0915076869f92244a0b5b384bf157d2ef097ee`
- Plan: `a3db7f9eba6664e2b15bfa3efcb1f86b97cc9405`
- Source fix: `6102b216cd65e13ea72bf4a8f7a47584534dee55`
- Deterministic smoke regression: `6e1d446f8eec489134b67d47040722f4017e064a`
- Smoke registration: `8f1ed34bb4d92e957a33cef49cd9fc51dcae4d5b`
- Static preflight: `e8eab1273986057c9b25f2ec091ed534ffa9ab63`

## Validation evidence

- Readback on current `main` confirmed version capture → caller enumeration → freshness rejection → empty-input validation ordering remains present.
- Static preflight committed to lock that source ordering plus stable/mutating/mutating-empty smoke coverage and ModuleInitializer registration.
- This remote connector session did not execute the full Core smoke executable, the Python preflight, GitHub Actions, or BricsCAD V25 runtime; no PASS claim is made for those environments.

## Excluded scope

- Grid annotation health/owner/built-label canonicality.
- BricsCAD Grid command lifecycle/native annotation behavior.
- Grid naming health diagnostics and unrelated naming semantics.
- No GitHub Actions dispatch or BricsCAD V25 runtime qualification.

## Completion condition

`COMPLETED`: the caller-enumeration freshness hole is fixed on `main`, focused deterministic regression/preflight coverage is committed, exact integration SHAs are recorded, and remote validation limitations are explicit.
