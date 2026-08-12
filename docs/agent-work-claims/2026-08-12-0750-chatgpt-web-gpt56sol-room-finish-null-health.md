# Work claim — Room Finish health null-element fail-visible

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-room-finish-null-health-20260812-0750`
- Registered: `2026-08-12T07:50:00+07:00`
- Completed: `2026-08-12T07:54:00+07:00`
- Baseline main SHA: `8454f2779c02eed273ebfc83b09bdf7a159ad5ed`
- Source commit on implementation branch: `a41dc974c52765ba172ffa900be6ca65ba5fd286`
- Smoke commit on implementation branch: `e2fd6f0e34cafd34cc9f9f071131b77f8bd93512`
- Merged PR: `#629`
- Main squash SHA: `ef9ab854576c190cd1f3a46175527e84d79a3dc1`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`RoomFinishHealthService.Inspect(ProjectState)` started by filtering `project.Elements` with `Where(x => x != null)`. A malformed project containing a null semantic element was therefore silently reduced before Room/finish identity and provenance checks, allowing this specialized provider to return a false-clean result instead of participating in the fail-visible `ComprehensiveModelHealthService` provider boundary.

## Reserved scope

- `src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishNullHealthSmoke.cs`
- this claim file

## Completed contract

- direct Room Finish health inspection now snapshots `project.Elements` while rejecting null semantic elements before constructing its identity index;
- existing Room/finish identity, provenance, duplicate, stale and scope diagnostics remain unchanged for valid element collections;
- composite health surfaces Room Finish provider failure as Error-level `HEALTH_PROVIDER_FAILED` through existing wrapper behavior;
- focused module-initializer smoke coverage pins direct null failure, composite provider-failure visibility, and the existing `UNLINKED_ROOM_FINISH` warning path;
- no Room/finish mutation/lifecycle logic, quantity/schedule behavior, CAD geometry, persistence, WPF/native BricsCAD, release/update, or unrelated health-provider behavior changed.

## Validation evidence

- Re-fetched merged source from `main` after PR #629 and confirmed null entries now fail closed before identity indexing.
- Re-fetched merged smoke from `main` and confirmed malformed/composite/valid diagnostic coverage is present.
- Re-checked concurrent `main` movement before merge; no concurrent commit touched the reserved source/test files.
- GitHub Actions were not manually dispatched.
- The committed smoke was not executed from this web session, and no BricsCAD V25 runtime PASS is claimed.
