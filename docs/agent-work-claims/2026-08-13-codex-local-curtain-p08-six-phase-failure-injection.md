# Work claim — LOCAL-002 P08 Curtain seven-boundary failure injection

- Status: `COMPLETED`
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

## Mixed-source production correction expansion

- Expanded from exact main `d34832385e1a3643b8e18f1d5cebb5f7a98c7dc5` after two clean licensed diagnostics. The merged broad marker first reported `VERIFY_BASELINE / OWNER_METADATA_REJECTED`; a local field-only diagnostic then isolated `LINE_HOST_METADATA_REJECTED`.
- Root cause is deterministic in production source: `QS3DCURTAIN3D` passes the full mixed LINE/open-POLYLINE selection to `WallSolidBuilder.BuildSelectedLineWalls`, while that builder deliberately rejects mixed source batches before host mutation. The command catches the exception, so the next probe stage observes no LINE host metadata. This contradicts the command's documented mixed-source six-phase contract; it is not a fixture or tolerance issue.
- This claim now additionally reserves `src/QS3D.BricsCAD.V25/Cad/CurtainWallBuildSelectionGuard.cs` and a bounded `CurtainWallBuildCommands.cs` correction that partitions the already-validated canonical source ObjectIds by LINE/path before each corresponding host/frame/panel builder, restores the complete selection for live stamping/UI/failure paths, and leaves every builder's own fail-closed single-type rule unchanged.
- Related Curtain/P08 static gates and the probe/runner allowlist may be strengthened. No host/frame/panel builder, geometry planner, Health/Level/Locate/QSDB/UI implementation or other LOCAL-002 cell enters scope.
- The correction and diagnostic refinement must merge before a fresh exact-main P08 run. Only a full seven-phase rollback plus valid mixed replacement PASS can close P08.

## Source-preparation status

- The internal one-shot seam, seven post-phase orchestrator checks, mixed LINE/path probe, guarded runner, static/privacy gate and runbook handoff are source-complete. No builder, planner, Health, Level or UI implementation changed.
- Strict installed-reference V25 `Release|x64` build passes at zero warnings/errors. P01-P08/native/orchestration/runtime-health/Level gates, runner parser and `scripts/preflight.py` pass.
- The 719-gate aggregate passes 718 gates; only the unrelated moving-main customer-release version mismatch remains (`plugin 0.1.0-preview.4` versus `Core 0.1.0-preview.3`).
- Runtime status is bounded `LOCAL_PASS` for P08 at exact merged SHA `a025b9aa20a919ae585ddab2700055389e38eb1c`; P09-P12 and overall LOCAL-002 remain `PENDING_LOCAL`.
- The first exact-source run at `b4b72ae5df6cd2f22bd592ab809aa78b5c81fdfd` stopped cleanly at `VERIFY_BASELINE / STATE_REJECTED`; BricsCAD exited and script/drawing-lock/sidecar/backup cleanup passed. This is not evidence of a production defect. The probe/runner now use an allowlisted owner-state diagnostic code (stale, metadata, output, liveness, Health or overlap) without exposing identities, geometry or exception text; a fresh exact-SHA rerun is required before any product correction or P08 qualification.
- Follow-up diagnostics resolved that broad result to `LINE_HOST_METADATA_REJECTED`, and source inspection confirmed the mixed-selection contradiction described in the expansion above. The canonical selection guard/orchestrator correction merged through PR #972: each specialized builder now receives only its validated LINE/path partition, while the complete validated selection is restored for stamps/UI/failure paths. Builders and their fail-closed single-type contracts are unchanged.

## Completion evidence

- Claim PR #968 merged as `0a70a004414a83dc9014d80cbe74ee706c36fc83`; source-preparation PR #969 merged as `b4b72ae5df6cd2f22bd592ab809aa78b5c81fdfd`; privacy-safe diagnostic PR #970 merged as `5a2ac2eec479bc15afdd282dd3a4b0874e6a2a2e`; claim expansion PR #971 merged as `71bea31ab8195b9249ad2cef16552b5b043443d4`; mixed-source correction PR #972 merged as exact runtime candidate `a025b9aa20a919ae585ddab2700055389e38eb1c`; closeout PR #973 records the bounded evidence and releases the claim.
- Exact candidate V25 `Release|x64` build passed at zero warnings/errors. P01-P08/native/orchestration/runtime-health/Level gates, runner parser and `scripts/preflight.py` passed. The aggregate remained 718/719 only because of the unrelated customer-release plugin/Core version mismatch.
- BricsCAD `25.2.10` loaded adapter SHA-256 `E18A82EE66631D091EC78CD8E65E864DAD03BD6E8A4031C1BD572AAB268651A1`. All seven one-shot phases were consumed and verified; semantic/native/source preservation was exact, the 63-object baseline was fully replaced by 87 healthy generated objects, and panel Health returned zero issues.
- Disposable DWG SHA-256 stayed `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`. Launched-process exit, private-script deletion, `.dwl`/`.dwl2`, sidecar and backup cleanup all passed. No private/customer data or GitHub Actions were used.
- This closes only P08. P09-P12 and overall LOCAL-002 remain open and `PENDING_LOCAL`.
