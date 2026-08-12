# Work claim — BOM release health-provider isolation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bom-provider-isolation-20260812-0805`
- Registered: `2026-08-12T08:05:00+07:00`
- Baseline main SHA: `ccb9d4c0d992bfff487808ac6f5181df3b3e619a`
- Priority: release-guard regression repair during owner-requested `continue all`

## Confirmed defect

`BomReleaseGuardService.Inspect()` invokes `RoomFinishHealthService.Inspect(project)` and `GeneratedCurtainPanelHealthService.Inspect(project, ...)` before its own `BOM_NULL_ELEMENT` scan. Both specialized providers now fail closed with `InvalidOperationException` on a null semantic element. As a result, the existing `BomReleaseGuardSmoke.NullSemanticEntryBlocksReleaseWithoutCrashing()` contract is no longer satisfiable: BOM inspection throws before it can return the intended Error-level release blockers.

## Reserved scope

- `src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs`
- `tests/QS3D.Core.SmokeTests/BomReleaseGuardSmoke.cs`
- this claim file for close-out

## Contract

- BOM release diagnostics isolate expected malformed-data `InvalidOperationException` failures from nested Room Finish and Curtain Panel health providers;
- provider failures become stable Error-level BOM diagnostics instead of escaping and aborting the release inspection;
- BOM's own `BOM_NULL_ELEMENT` blocker remains visible for malformed element collections;
- valid Room Finish and Curtain Panel diagnostics are still forwarded unchanged when providers succeed;
- no provider implementation, CAD mutation, quantity calculation, persistence, WPF/native BricsCAD, updater/release packaging, or unrelated health behavior changes.

## Validation plan

Keep the existing null-element no-crash smoke and strengthen it to assert provider-failure diagnostics are fail-visible. Preserve the existing provenance/conflict, generated-handle and redaction assertions.

No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim from this web session.
