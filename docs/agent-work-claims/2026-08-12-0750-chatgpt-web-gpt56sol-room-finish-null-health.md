# Work claim — Room Finish health null-element fail-visible

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish-null-health-20260812-0750`
- Registered: `2026-08-12T07:50:00+07:00`
- Baseline main SHA: `8454f2779c02eed273ebfc83b09bdf7a159ad5ed`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`RoomFinishHealthService.Inspect(ProjectState)` starts by filtering `project.Elements` with `Where(x => x != null)`. A malformed project containing a null semantic element is therefore silently reduced before Room/finish identity and provenance checks, allowing this specialized provider to return a false-clean result instead of participating in the fail-visible `ComprehensiveModelHealthService` provider boundary.

## Reserved scope

- `src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs`
- isolated focused Core smoke regression for this provider
- this claim file for close-out

## Contract

- direct Room Finish health inspection rejects null semantic elements before constructing its identity index;
- existing Room/finish identity, provenance, duplicate, stale and scope diagnostics remain unchanged for valid element collections;
- composite health surfaces Room Finish provider failure as Error-level `HEALTH_PROVIDER_FAILED` through existing wrapper behavior;
- no Room/finish mutation/lifecycle logic, quantity/schedule behavior, CAD geometry, persistence, WPF/native BricsCAD, release/update, or unrelated health-provider changes.

## Validation plan

Add isolated module-initializer smoke coverage for direct null fail-closed, composite provider-failure visibility, and an existing valid `UNLINKED_ROOM_FINISH` warning path. Re-fetch moving `main` before integration and do not overwrite concurrent work.

No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim from this web session.
