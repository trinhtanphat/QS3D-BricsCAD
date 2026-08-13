# Work claim — LOCAL-002 P08 Curtain seven-boundary failure injection

- Status: `ACTIVE`
- Agent: `codex-local-root` (`/root`, local Windows + licensed BricsCAD V25)
- Registered: `2026-08-13T11:51:45+07:00`
- Baseline main SHA: `ead773ba3c0e7cecbff07c46ccba77d93bdd6377`
- Priority: `LOCAL-002 / P08 / P0` — qualify semantic rollback plus all six LINE/path host/frame/panel native phase boundaries

## Reserved scope

Add one bounded automation-only, one-shot failure-injection seam to the production Curtain orchestrator and qualify it on a mixed LINE/open-POLYLINE GlassWall batch. Inject immediately after each completed pre-commit boundary:

1. semantic regeneration;
2. LINE host replacement;
3. open-POLYLINE host replacement;
4. LINE frame replacement;
5. path frame replacement;
6. LINE panel replacement;
7. path panel replacement.

For every phase, production `QS3DCURTAIN3D` must consume the exact armed one-shot ticket, report its normal atomic-failure path, abort the outer native transaction and restore the command semantic snapshot. The exact pre-attempt semantic state, Current Space Handle/type/extents digest and complete source/host/frame/panel ownership sets must remain unchanged. A final unarmed mixed-source replacement must succeed.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs` — calls to a bounded internal injection seam only, after each completed phase and before outer commit;
- new `src/QS3D.BricsCAD.V25/Cad/CurtainWallBuildFailureInjection.cs` — thread-safe in-memory allowlisted one-shot arm/consume/verify service, no direct environment-variable activation;
- new `src/QS3D.BricsCAD.V25/CurtainPanelAtomicFailureRuntimeProbeCommands.cs` automation-only seed/arm/verify commands;
- new `scripts/test-bricscad-v25-curtain-panel-atomic-failures.ps1` guarded runner;
- new `scripts/preflight-curtain-panel-atomic-failure-runtime-probe.py` static/privacy/order gate;
- `docs/CURTAIN-NATIVE-PANELS.md` P08 handoff/evidence and this claim.

## Excluded scope

- No edits inside host/frame/panel builders, planners, ownership/Health/Locate/Level services, QSDB persistence or product UI.
- No P09-P12 post-commit, Undo/save-reopen/multi-DWG qualification or overall LOCAL-002 promotion.
- No externally configurable production fault switch: the seam is internal, one-shot and armable only through the authenticated automation probe command in the same assembly.
- No private/customer DWG, GitHub Actions, installer, signing or release work.

## Validation plan

- Claim-only PR must be merged before source edits.
- Build exact candidate `Release|x64` with installed V25 references at zero warnings/errors; run focused P08 plus P01-P07/native/orchestration/runtime-health/Level gates, runner parser, `scripts/preflight.py` and aggregate.
- Use a fresh repository-generated synthetic disposable copy, empty outside-repository artifacts, initialized profile and zero pre-existing BricsCAD/sidecar/backup/drawing-lock state.
- Publish aggregate evidence only: seven consumed phases, semantic/native/ownership preservation, final valid replacement, unchanged DWG hash and verified process/script/drawing-lock/sidecar cleanup. Never emit raw Handles, IDs, paths, profiles, drawing content or exception details.

## Coordination

No current ACTIVE/BLOCKED claim or open PR owns P08, `CurtainWallBuildCommands.cs` or the proposed new files. P06/P07 are completed and explicitly left P08 pending. The two other active claims are unrelated Room Finish XLSX and Preview Review XML lanes.

## Completion condition

The seam/probe/runner/gate is merged, a clean exact-main licensed run either records the full sanitized seven-phase PASS contract or a bounded diagnostic FAIL, docs remain truthful and this claim is `COMPLETED`. P09-P12 and overall `LOCAL-002` remain `PENDING_LOCAL`.

## Source-preparation status

- The internal one-shot seam, seven post-phase orchestrator checks, mixed LINE/path probe, guarded runner, static/privacy gate and runbook handoff are source-complete. No builder, planner, Health, Level or UI implementation changed.
- Strict installed-reference V25 `Release|x64` build passes at zero warnings/errors. P01-P08/native/orchestration/runtime-health/Level gates, runner parser and `scripts/preflight.py` pass.
- The 719-gate aggregate passes 718 gates; only the unrelated moving-main customer-release version mismatch remains (`plugin 0.1.0-preview.4` versus `Core 0.1.0-preview.3`).
- Runtime status remains `PENDING_LOCAL` until the source-preparation PR is merged and a clean exact merged SHA/DLL completes all seven licensed injections plus the valid control.
