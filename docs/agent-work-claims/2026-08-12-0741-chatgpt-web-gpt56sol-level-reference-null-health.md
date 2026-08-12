# Work claim — Level Reference health invalid-entry fail-visible

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-level-reference-null-health-20260812-0741`
- Registered: `2026-08-12T07:41:00+07:00`
- Baseline main SHA: `bb42dd7ac76880731ca89add594632d070be2f78`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`LevelReferenceHealthService.Inspect(ProjectState)` silently skips null Floor entries and null semantic elements. A malformed project can therefore be treated as clean by the specialized Level/vertical-placement provider even though `ComprehensiveModelHealthService` has an explicit fail-visible provider boundary that converts diagnostic data failures to Error-level `HEALTH_PROVIDER_FAILED`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/LevelReferenceHealthService.cs`
- isolated focused Core smoke regression for this provider
- this claim file for close-out

## Contract

- direct Level Reference inspection rejects a null Floor entry instead of ignoring it;
- direct Level Reference inspection rejects a null semantic element instead of ignoring it;
- composite health surfaces the Level Reference provider failure as Error-level `HEALTH_PROVIDER_FAILED` through existing wrapper behavior;
- existing valid Level/Bottom/Top reference diagnostic codes and vertical-placement semantics remain unchanged;
- no Floor/Level mutation service, CAD placement, WPF/native BricsCAD, persistence, release/update, or unrelated diagnostic provider changes.

## Validation plan

Add isolated module-initializer smoke coverage for null Floor fail-closed, null element fail-closed/composite visibility, and an existing valid `TOP_LEVEL_REQUIRES_BOTTOM_LEVEL` path. Re-fetch moving `main` before integration and do not overwrite concurrent work.

No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim from this web session.
