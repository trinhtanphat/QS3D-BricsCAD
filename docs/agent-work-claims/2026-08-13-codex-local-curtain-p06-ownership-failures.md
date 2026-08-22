# Work claim — LOCAL-002 P06 Curtain panel ownership failures

- Status: `COMPLETED`
- Agent: `codex-local-root` (`/root`, local Windows + licensed BricsCAD V25)
- Registered: `2026-08-13T11:05:23+07:00`
- Baseline main SHA: `d676bd9970910938d19cfbc3de333d52bf9b6af1`
- Priority: `LOCAL-002 / P06 / P0` — qualify destructive-replacement refusal for malformed or conflicting native panel ownership before moving to lower-value Curtain cells

## Reserved scope

Prepare and run one bounded, synthetic, exact-SHA BricsCAD V25 qualification for the `docs/CURTAIN-NATIVE-PANELS.md` P06 matrix. Starting from production-created LINE GlassWall panel output, independently exercise:

1. one missing/erased expected old panel handle while other old panels survive;
2. duplicate canonical handle metadata, including a hexadecimal alias of the same native object;
3. metadata pointing at a live foreign or unmarked `Solid3d`;
4. a cross-owner semantic handle claim.

Every case must invoke the production `QS3DCURTAIN3D` replacement path and prove refusal before any destructive erase or append. The exact pre-attempt semantic panel metadata, surviving owned panel set, unrelated owner set and native object counts must remain unchanged. A final uncorrupted control must prove ordinary exact replacement still succeeds.

## Expected surfaces

- new `src/QS3D.BricsCAD.V25/CurtainPanelOwnershipFailureRuntimeProbeCommands.cs` automation-only seed/corrupt/verify commands;
- new `scripts/test-bricscad-v25-curtain-panel-ownership-failures.ps1` guarded single-process runner;
- new `scripts/preflight-curtain-panel-ownership-failure-runtime-probe.py` static/privacy/order gate;
- `docs/CURTAIN-NATIVE-PANELS.md` P06 handoff and sanitized exact-SHA result only;
- this claim file.

## Excluded scope

- No edits to Curtain builders, planners, ownership guards, native marker services, Health, Locate, Level placement, Direct Draw, QSDB persistence or product UI unless the licensed probe first demonstrates a production defect and this claim is expanded and published before that edit.
- No P07-P12 qualification, broad failure injection, Undo/save-reopen/multi-DWG claim or overall `LOCAL-002` promotion.
- No overlap with the active Curtain tiny-ratio Core claim; this probe uses ordinary bounded dimensions and treats current production planning as read-only.
- No private/customer DWG, GitHub Actions, installer, signing or release work.

## Validation plan

- Build the exact clean candidate with installed BricsCAD V25 references and require zero warnings/errors.
- Run focused P06 plus existing P01-P05/native/orchestration/runtime-health gates and aggregate preflight.
- Use a fresh ordinary copy of the repository-generated sample with a guarded suffix, an empty artifact directory outside the repository, an initialized profile and one hidden BricsCAD process.
- Require sanitized aggregate markers only: per-case refusal, no append/erase, surviving-old-set and metadata preservation, unrelated-owner preservation, foreign-object survival, final valid replacement, unchanged input DWG hash and verified process/script/sidecar cleanup. Never emit raw Handles, semantic IDs, paths, exception details, drawing content or profile names.

## Coordination

Current ACTIVE/BLOCKED claim audit found no reservation for the P06 runtime scenario or the expected new files. `2026-08-12-1144-chatgpt-gpt56sol-curtain-division-tiny-ratio.md` owns only `CurtainWallLayoutPlanner.DivisionCount(...)` plus its focused Core smoke. The broad LOCAL-003 claim explicitly excludes Curtain panel topology, clipping and ownership P01-P12 work. Production Curtain ownership surfaces remain read-only unless a separately published expansion is required by concrete local evidence.

## Completion condition

The additive source/runner/gate is merged, a fresh exact-main licensed V25 run either records the complete sanitized P06 PASS contract or records an allowlisted diagnostic FAIL without overclaiming, reusable docs are updated truthfully, and this claim is marked `COMPLETED`. P07-P12 and overall LOCAL-002 remain `PENDING_LOCAL`.

## Completion evidence

- Claim-only PR `#960` merged as `f1c90f045f11d5024b29cad08752723f923ec7e0` (claim commit `5d7e4a337225c7822c462dce490533851b5566f0`).
- Additive probe/runner/gate preparation PR `#961` merged as `5189463ae85c399745d640d2081ff1aee0a44a2d` (source commit `40f2c341`).
- Probe-isolation correction PR `#962` merged as `db8961a55b1dca51921546e23161ebd51884863c` (source commit `99094acc`); it restored the clean semantic baseline after each already-proven independent refusal so the final whole-project regeneration control did not invalidate prior per-case samples.
- Drawing-lock cleanup hardening PR `#963` merged as exact runtime candidate `7c41ff875813a7773868240e8f79d22060eca196` (source commit `de4e4cde`).
- The exact candidate built `Release|x64` against installed BricsCAD V25 references with zero warnings/errors. Adapter SHA-256 was `309E934D12581DCDD97D104BA46F13F2F5A03595B2C005DBE3B22FF5FB0E6DB6`.
- Licensed BricsCAD `25.2.10` returned `QS3D_CURTAIN_PANEL_OWNERSHIP_RUNTIME_V1 / PASS`: 90 baseline panels; four of four unsafe cases refused; no erase/append; semantic metadata, surviving old sets, unrelated owners and foreign object preserved; final valid replacement produced 21 complete marked panels and zero panel Health issues.
- The repository-generated disposable DWG SHA-256 remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`. Launched-process exit, private-script deletion, `.dwl`/`.dwl2` deletion and sidecar/backup absence were verified. Markers contained aggregate sanitized fields only.
- Closeout validation passed the strict V25 `Release|x64` build with zero warnings/errors, the P01-P06/native/orchestration/runtime-health/Level focused gates, the runner parser and `scripts/preflight.py`. The 717-gate aggregate had one unrelated moving-main release-version failure (`plugin 0.1.0-preview.4` versus `Core 0.1.0-preview.3`); no release/version file is in this claim.
- Earlier diagnostic FAIL/PASS attempts were used only to harden probe isolation and drawing-lock cleanup; they are not qualification evidence.
- Only P06 is promoted to bounded `LOCAL_PASS`. P07-P12 and overall `LOCAL-002` remain `PENDING_LOCAL`.
