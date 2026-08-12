# Work claim — Semantic Schedule health null-identity fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-semantic-schedule-null-health`
- Registered: `2026-08-12`
- Baseline main SHA: `d450d7f86fa6fe1f335ed42de315d2524367ce14`
- Priority: P1 — malformed semantic identity collections must not be silently normalized by health diagnostics.
- Task Key: `CORE-SEMANTIC-SCHEDULE-NULL-HEALTH`

## Confirmed defect

When persisted Semantic Schedule metadata is present, `SemanticScheduleHealthService` builds Floor, Zone, and Element identity counts through `BuildIdentityCounts(...)`. That helper still executes `if (value == null) continue;`, so malformed null identity entries can disappear from the provider's view and produce incomplete or false-clean schedule diagnostics. The canonical Semantic Schedule renderer already fails closed on null semantic Elements, and `ComprehensiveModelHealthService.AddSafely(...)` already converts `InvalidOperationException` from a provider into Error-level `HEALTH_PROVIDER_FAILED`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/SemanticScheduleHealthService.cs`
- `tests/QS3D.Core.SmokeTests/SemanticScheduleHealthSmoke.cs`
- this claim file

Do not modify `SemanticScheduleCatalog`, native schedule tables, BricsCAD commands, documentation placement, quantity/reporting, or unrelated health providers.

## Intended contract

- No Semantic Schedule metadata keeps the existing no-op/empty-health behavior.
- With Semantic Schedule metadata active, a null Floor, Zone, or Element identity entry fails visible with `InvalidOperationException` rather than being skipped.
- Composite health retains the existing safe-provider boundary and surfaces malformed provider input as `HEALTH_PROVIDER_FAILED`.
- Valid schedules, including valid zero-match schedules, remain healthy and read-only.
- Existing stale/ambiguous reference and invalid-template/catalog diagnostics remain unchanged.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Direct smoke coverage pins null Floor/Zone/Element rejection plus representative valid behavior; source and smoke are read back from merged `main`, then this claim is closed.
