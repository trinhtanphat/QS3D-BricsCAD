# Work claim — LOCAL-002 P06 Curtain panel ownership failures

- Status: `ACTIVE`
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

The additive source/runner/gate is merged, a fresh exact-main licensed V25 run either records the complete sanitized P06 PASS contract or records an allowlisted diagnostic FAIL without overclaiming, reusable docs are updated truthfully, and this claim is marked `COMPLETED`. P07-P12 and overall LOCAL-002 remain pending.
