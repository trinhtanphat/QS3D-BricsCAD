# Work claim — Grid Naming health null-element fail-visible

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-naming-null-health-20260812-0746`
- Registered: `2026-08-12T07:46:00+07:00`
- Baseline main SHA: `ffb61455c8dcc7c5497b105bef71dab7015dbe8f`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`GridNamingHealthService.Inspect(ProjectState)` combines the null check with the category filter: `if (element == null || element.Category != ElementCategory.Grid) continue;`. A malformed project containing a null semantic element can therefore be reported clean by this specialized provider instead of participating in the fail-visible `ComprehensiveModelHealthService` provider boundary.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GridNamingHealthService.cs`
- isolated focused Core smoke regression for this provider
- this claim file for close-out

## Contract

- direct Grid Naming health inspection rejects null semantic elements instead of silently skipping them;
- non-Grid elements continue to be ignored by this specialized provider;
- composite health surfaces Grid Naming provider failure as Error-level `HEALTH_PROVIDER_FAILED` through existing wrapper behavior;
- existing Grid label/sequence warning and error codes remain unchanged;
- no Grid mutation/naming service, CAD annotations, persistence, WPF/native BricsCAD, release/update, or unrelated health-provider behavior changes.

## Validation plan

Add isolated module-initializer smoke coverage for direct null fail-closed, composite provider-failure visibility, and an existing valid `GRID_SEQUENCE_INVALID` diagnostic. Re-fetch moving `main` before integration and do not overwrite concurrent work.

No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim from this web session.
