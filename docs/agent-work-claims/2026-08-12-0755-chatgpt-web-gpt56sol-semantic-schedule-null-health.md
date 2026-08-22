# Work claim — Semantic Schedule health null-identity fail-visible

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-semantic-schedule-null-health`
- Registered: `2026-08-12`
- Baseline main SHA: `d450d7f86fa6fe1f335ed42de315d2524367ce14`
- Priority: P1 — malformed semantic identity collections must not be silently normalized by health diagnostics.
- Task Key: `CORE-SEMANTIC-SCHEDULE-NULL-HEALTH`

## Confirmed defect

When persisted Semantic Schedule metadata is present, `SemanticScheduleHealthService` built Floor, Zone, and Element identity counts through `BuildIdentityCounts(...)`, which silently skipped null identity entries. Malformed semantic identity collections could therefore disappear from the provider's view and produce incomplete or false-clean schedule diagnostics.

## Reserved scope

- `src/QS3D.Core/Diagnostics/SemanticScheduleHealthService.cs`
- `tests/QS3D.Core.SmokeTests/SemanticScheduleHealthSmoke.cs`
- this claim file

## Completed contract

- No Semantic Schedule metadata retains the existing no-op/empty-health behavior.
- With Semantic Schedule metadata active, a null Floor, Zone, or Element identity entry now throws `InvalidOperationException` instead of being skipped.
- `ComprehensiveModelHealthService` retains its existing safe-provider boundary and smoke coverage pins Error-level `HEALTH_PROVIDER_FAILED` for the Semantic Schedule provider.
- Valid schedules, including valid zero-match schedules, retain their existing healthy/read-only behavior.
- Existing stale/ambiguous reference and invalid-template/catalog diagnostics remain in place.

## Commits

- Claim: `5a6021cb42025bd5012c537bc4fee83f0f6e1758`
- Source fix: `0dde780ee13d787aa8b0869eb743cc142c720693`
- Smoke regression: `9071c380d8cfe76bc43b02dc10c084d28fd0283c`

## Verification

Readback from `main` after concurrent commits confirmed the null-identity throw remains in `SemanticScheduleHealthService` and the Floor/Zone/Element smoke cases plus aggregate provider-failure assertion remain present. The executable Core smoke suite was not run by this GitHub connector session, so no executable test/build PASS is claimed. No GitHub Actions/build/release was dispatched and no BricsCAD runtime PASS is claimed.
