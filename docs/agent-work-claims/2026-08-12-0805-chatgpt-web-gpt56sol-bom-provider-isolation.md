# Work claim — BOM release health-provider isolation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bom-provider-isolation-20260812-0805`
- Registered: `2026-08-12T08:05:00+07:00`
- Completed: `2026-08-12T08:10:00+07:00`
- Baseline main SHA: `ccb9d4c0d992bfff487808ac6f5181df3b3e619a`
- Source commit on implementation branch: `65d3a83c9fe34adda4752d42c66bacea8e9e10c1`
- Smoke commit on implementation branch: `05d3e0a57084e7fabade5b953103e10a1b414a77`
- Merged PR: `#636`
- Main squash SHA: `cd94ed50289ce61e6cba23688d74634b16cf2032`
- Priority: release-guard regression repair during owner-requested `continue all`

## Confirmed defect

`BomReleaseGuardService.Inspect()` invoked `RoomFinishHealthService.Inspect(project)` and `GeneratedCurtainPanelHealthService.Inspect(project, ...)` before its own `BOM_NULL_ELEMENT` scan. Both specialized providers now fail closed with `InvalidOperationException` on a null semantic element. As a result, the existing `BomReleaseGuardSmoke.NullSemanticEntryBlocksReleaseWithoutCrashing()` contract was no longer satisfiable: BOM inspection threw before it could return the intended Error-level release blockers.

## Reserved scope

- `src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs`
- `tests/QS3D.Core.SmokeTests/BomReleaseGuardSmoke.cs`
- this claim file

## Completed contract

- BOM release diagnostics now isolate expected malformed-data `InvalidOperationException` failures from nested Room Finish and Curtain Panel health providers;
- provider failures become stable Error-level `BOM_ROOM_FINISH_HEALTH_FAILED` / `BOM_CURTAIN_PANEL_HEALTH_FAILED` diagnostics instead of escaping and aborting inspection;
- BOM's own `BOM_NULL_ELEMENT` blocker remains visible for malformed element collections;
- valid Room Finish and Curtain Panel diagnostics are still forwarded unchanged when providers succeed;
- deterministic BOM smoke pins exactly one of each nested-provider failure, their stable redacted messages, and Error severity alongside `BOM_NULL_ELEMENT`;
- no provider implementation, CAD mutation, quantity calculation, persistence, WPF/native BricsCAD, updater/release packaging, or unrelated health behavior changed.

## Validation evidence

- Compared claim-visible branch base to moving `main` before integration; concurrent changes did not touch the two reserved BOM source/test files.
- Re-fetched merged `BomReleaseGuardService.cs` from `main` and confirmed separate `InvalidOperationException` isolation around both nested providers.
- Re-fetched merged `BomReleaseGuardSmoke.cs` from `main` and confirmed no-crash/Error-level provider-failure assertions are present.
- GitHub Actions were not manually dispatched.
- The smoke source was not executed from this web session, and no BricsCAD V25 runtime PASS is claimed.
