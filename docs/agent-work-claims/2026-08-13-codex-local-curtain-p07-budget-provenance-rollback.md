# Work claim — LOCAL-002 P07 Curtain budget/provenance rollback

- Status: `COMPLETED`
- Agent: `codex-local-root` (`/root`, local Windows + licensed BricsCAD V25)
- Registered: `2026-08-13T11:36:12+07:00`
- Baseline main SHA: `77b559573864d7c5c35f1a0a6d1b93e6cdd75a72`
- Priority: `LOCAL-002 / P07 / P0` — qualify bounded panel-input and linked-opening provenance failures on a later selected GlassWall while proving whole-command rollback

## Reserved scope

Prepare and run one synthetic, exact-SHA BricsCAD V25 qualification for `docs/CURTAIN-NATIVE-PANELS.md` P07. Starting from production-created native Curtain output, exercise three independent two-owner batches whose first selected GlassWall is valid and whose later selected GlassWall is unsafe:

1. an authoritative LINE layout with more than the adapter's 4096 base-panel limit while its frame count remains below the frame limit;
2. a linked Door/WallOpening with missing or otherwise non-single live source provenance;
3. a linked Door/WallOpening whose live source is outside the permitted host-centerline proximity.

Each attempt must invoke production `QS3DCURTAIN3D`. The later unsafe owner must fail closed, and the command's outer native transaction plus semantic snapshot must preserve the exact pre-attempt native handles/bounds, generated ownership metadata, stale state, project version/audit and unrelated owner state for the whole selected batch. A final valid two-owner replacement must succeed and prove the command did not merely no-op.

## Expected surfaces

- new `src/QS3D.BricsCAD.V25/CurtainPanelBudgetProvenanceRuntimeProbeCommands.cs` automation-only seed/prepare/verify commands;
- new `scripts/test-bricscad-v25-curtain-panel-budget-provenance.ps1` guarded single-process runner;
- new `scripts/preflight-curtain-panel-budget-provenance-runtime-probe.py` static/privacy/order gate;
- `docs/CURTAIN-NATIVE-PANELS.md` P07 handoff and sanitized exact-SHA result only;
- this claim file.

## Excluded scope

- No edits to Curtain builders/planners, ownership/Health/Locate/Level services, `CurtainWallBuildCommands`, Direct Draw, QSDB persistence or product UI unless the licensed probe demonstrates a production defect and a claim expansion is published first.
- No P08-P12 failure injection, post-commit warning, Undo/save-reopen/multi-DWG qualification or overall `LOCAL-002` promotion.
- No private/customer DWG, GitHub Actions, installer, signing or release work.

## Validation plan

- Build the exact clean candidate `Release|x64` with installed BricsCAD V25 references and require zero warnings/errors.
- Run the focused P07 gate plus P01-P06/native/orchestration/runtime-health/Level gates, `scripts/preflight.py`, the aggregate gate and the PowerShell parser.
- Use only a fresh ordinary copy of the repository-generated synthetic sample with a guarded suffix, an empty artifact directory outside the repository, an initialized profile and no pre-existing BricsCAD process/sidecar/backup/drawing-lock.
- Accept aggregate sanitized evidence only: expected budget/provenance precondition, per-case refusal, exact whole-batch semantic/native preservation, final valid replacement, unchanged input DWG hash and verified process/script/drawing-lock/sidecar cleanup. Never publish raw Handles, semantic IDs, paths, profiles, drawing content or exception details.

## Coordination

The current ACTIVE/BLOCKED-claim and open-PR audit found no owner for P07, the proposed additive files or these runtime cases. The two current active claims are unrelated Room Finish XLSX and Preview Review XML lanes. The completed P06 claim explicitly excluded P07. Production Curtain surfaces remain read-only under this claim unless separately expanded from concrete local evidence.

## Completion condition

The additive probe/runner/gate is merged, an exact-main licensed run records either the complete sanitized P07 PASS contract or an allowlisted diagnostic FAIL without overclaiming, docs are updated truthfully and this claim is marked `COMPLETED`. P08-P12 and overall `LOCAL-002` remain `PENDING_LOCAL`.

## Source-preparation status

- Additive command, runner, focused gate and runbook handoff are source-complete on the implementation branch; production Curtain code remains unchanged.
- Strict installed-reference V25 `Release|x64` build passes with zero warnings/errors. P01-P07, native/orchestration/runtime-health/Level gates, the runner parser and `scripts/preflight.py` pass.
- The 718-gate aggregate passes 717 gates; only the unrelated moving-main customer-release version mismatch remains (`plugin 0.1.0-preview.4` versus `Core 0.1.0-preview.3`). This claim does not own release/version files.
- Runtime evidence remains `PENDING_LOCAL` until the source-preparation PR is merged and the exact clean merged SHA/DLL completes the licensed matrix.

## Completion evidence

- Claim-only PR `#965` merged as `367d658eacdaae5c2a37d1fd14b0e92001a4f390` (claim commit `17ef8571`).
- Additive source-preparation PR `#966` merged as exact runtime candidate `ef8656362aed5f5eb060bd62383d102f6302b125` (source commit `79261e7d`). Production Curtain builders, orchestration, Health and Level code remained unchanged.
- The exact candidate passed the installed-reference V25 `Release|x64` build with zero warnings/errors. Adapter SHA-256 was `5805F8E441D3EBE522E9E9ECC0A8480CD0F3B18E31F447D21B5F57CA4339770B`.
- Licensed BricsCAD `25.2.10` returned `QS3D_CURTAIN_PANEL_BUDGET_PROVENANCE_RUNTIME_V1 / PASS`: two GlassWalls, two Doors, 30 baseline panels, three of three later-element refusals, exact whole-batch native and semantic preservation, and a valid two-owner replacement producing 36 complete marked panels with zero panel Health issues.
- The repository-generated disposable DWG SHA-256 remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`. Process exit, private-script deletion, `.dwl`/`.dwl2` deletion and sidecar/backup absence were independently verified; marker/metadata were aggregate-only.
- Only P07 is promoted to bounded `LOCAL_PASS`. P08-P12 and overall `LOCAL-002` remain `PENDING_LOCAL`.
