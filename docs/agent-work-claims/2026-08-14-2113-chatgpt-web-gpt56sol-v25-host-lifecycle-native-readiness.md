# Work claim — BricsCAD V25 host lifecycle and native dependency readiness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-v25-native-readiness`
- Registered: `2026-08-14T21:13:00+07:00`
- Baseline main SHA: `6826ef6616e4d818ee377d2e4e581a75af27bd2c`
- Implementation branch: `agent/chatgpt-web-gpt56sol-v25-native-readiness/v25-host-lifecycle-native-readiness-20260814`
- Integration batch: `integration/chatgpt-web-gpt56sol-v25-native-readiness-20260814`
- Priority: close remote-safe adapter/native-host safety gaps before treating the V25 adapter source lane as release-grade.

## Reserved scope

Harden the BricsCAD V25 plugin host lifecycle so an optional updater/bootstrap teardown failure cannot strand lifecycle/ribbon/palette cleanup, and make runtime readiness cover the native BREP dependency used by quantity geometry explanation rather than validating only BrxMgd/TD_Mgd.

The implementation must preserve the current V25 `net48/x64` hosted-plugin product boundary, preserve V26 source sharing, and fail closed on a mismatched/unavailable required V25 native dependency without manufacturing licensed runtime evidence.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/PluginEntry.cs` — contain top-level initialization/termination failure boundaries so partial startup is rolled back and teardown continues across independent services.
- `src/QS3D.BricsCAD.V25/Updates/UpdateBootstrapper.cs` — make start/stop state transitions rollback-safe/idempotent when update coordinator/event/window operations fail.
- `src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs` — include required V25 BREP assembly identity/readiness in `QS3DRUNTIMECHECK` while preserving the V26 shared-source build boundary.
- deterministic source/preflight guard(s) under `scripts/` for the lifecycle/readiness contracts above.
- this claim for implementation/integration close-out; update a canonical local qualification inbox scenario only if the source change creates a new exact V25 runtime check that local workers must execute.

## Exclusions / collision boundaries

- Do not modify V25 preview tag sequencing, release dispatcher/version scripts, or other surfaces reserved by the active release-preview-sequence claim.
- Do not modify Core persistence/project mutation surfaces reserved by concurrent persistence claims.
- Do not redesign the updater protocol, release manifest, signing policy, Ribbon feature set, Palette UI, native geometry builders, or semantic model.
- Do not claim real `NETLOAD`, licensed BricsCAD V25, private-DWG, native UI, installer, or Authenticode `LOCAL_PASS` from remote/static evidence.
- Keep V26 build compatibility: V25-only BREP readiness checks must not introduce an unconditional V26 compile-time dependency on `TD_MgdBrep.dll`.

## Evidence / reason

Current `PluginEntry.Terminate()` executes updater, ribbon, document, palette and augmenter cleanup sequentially without a top-level containment boundary. `UpdateBootstrapper.Start()` sets `_started = true` before event subscription/coordinator startup and has no rollback if a later step throws; `Stop()` can likewise stop before completing unsubscribe/coordinator/window cleanup. By contrast, `DocumentLifecycleCoordinator` already rolls back failed startup and performs best-effort independent teardown.

`QS3D.BricsCAD.V25.csproj` requires `BrxMgd.dll`, `TD_Mgd.dll` and `TD_MgdBrep.dll`, and quantity geometry explanation consumes `Teigha.BoundaryRepresentation`; current `QS3DRUNTIMECHECK` validates only the BrxMgd and TD_Mgd runtime majors. That leaves the required BREP native-managed dependency outside the adapter readiness verdict.

## Validation plan

- Add deterministic regression/source guards for rollback-safe updater bootstrap and independent top-level teardown.
- Add deterministic guard that V25 runtime readiness validates the BREP dependency while V26 shared-source compilation remains guarded from a V25-only BREP reference.
- Keep existing plugin source guards and Core smoke paths unchanged unless a demonstrated regression requires a minimal update.
- Re-read final branch diff, refresh current `main`, reconcile only non-overlapping concurrent changes, integrate once through the declared integration branch, and verify the final implementation is reachable from current `main`.
- Report source/static/CI evidence separately from licensed BricsCAD runtime evidence.

## Completion condition

V25 adapter startup/update lifecycle is rollback-safe, termination continues cleanup across independent host services, `QS3DRUNTIMECHECK` includes the required V25 BREP dependency without breaking the V26 shared-source boundary, deterministic regression coverage is present, the coherent implementation is integrated through the declared branch flow onto current `main`, and this claim is marked `COMPLETED` with exact implementation/integration/main SHAs. Licensed V25 runtime remains a separate LOCAL_ONLY qualification unless exact local evidence is supplied.
