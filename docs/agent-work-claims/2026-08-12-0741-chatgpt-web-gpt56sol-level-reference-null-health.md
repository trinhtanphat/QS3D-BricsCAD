# Work claim — Level Reference health invalid-entry fail-visible

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-level-reference-null-health-20260812-0741`
- Registered: `2026-08-12T07:41:00+07:00`
- Completed: `2026-08-12T07:44:00+07:00`
- Baseline main SHA: `bb42dd7ac76880731ca89add594632d070be2f78`
- Source commit on implementation branch: `080f5316b84ffa0a855fa00f51e5f83efc151a5a`
- Smoke commit on implementation branch: `d9d826a6fcdded86265353c9737ae4a5dd39305f`
- Merged PR: `#624`
- Main squash SHA: `293754d0f020f3702e118fff80efc9de1210656b`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`LevelReferenceHealthService.Inspect(ProjectState)` silently skipped null Floor entries, blank Floor ids and null semantic elements. A malformed project could therefore be treated as clean by the specialized Level/vertical-placement provider even though `ComprehensiveModelHealthService` has an explicit fail-visible provider boundary that converts diagnostic data failures to Error-level `HEALTH_PROVIDER_FAILED`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/LevelReferenceHealthService.cs`
- `tests/QS3D.Core.SmokeTests/LevelReferenceNullHealthSmoke.cs`
- this claim file

## Completed contract

- direct Level Reference inspection rejects a null Floor entry instead of ignoring it;
- blank Floor/Level semantic ids fail closed instead of being silently excluded from the level index;
- direct Level Reference inspection rejects a null semantic element instead of ignoring it;
- composite health surfaces the Level Reference provider failure as Error-level `HEALTH_PROVIDER_FAILED` through existing wrapper behavior;
- existing valid Level/Bottom/Top reference diagnostic codes and vertical-placement semantics remain unchanged;
- focused module-initializer smoke coverage pins null Floor failure, null element failure/composite visibility, and the existing `TOP_LEVEL_REQUIRES_BOTTOM_LEVEL` path;
- no Floor/Level mutation service, CAD placement, WPF/native BricsCAD, persistence, release/update, or unrelated diagnostic provider changed.

## Validation evidence

- Re-fetched merged source from `main` after PR #624 and confirmed invalid entries now fail closed.
- Re-fetched merged smoke from `main` and confirmed malformed/direct/composite/valid-state coverage is present.
- Re-checked concurrent `main` movement immediately before merge; no concurrent commit touched the reserved source/test files.
- GitHub Actions were not manually dispatched.
- The committed smoke was not executed from this web session, and no BricsCAD V25 runtime PASS is claimed.
