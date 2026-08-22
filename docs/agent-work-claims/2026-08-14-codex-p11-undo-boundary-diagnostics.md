# Work claim — P11 native Undo boundary diagnostics

- Status: `ACTIVE`
- Agent: `codex-p11-undo-boundary-diagnostics-20260814` (`/root/fix_curtain_method_gates`)
- Registered: `2026-08-14T16:51:22+07:00`
- Baseline main SHA: `87d9ef2e35d0baa2b99d4d19c820115e339c939a`
- Priority: `LOCAL-002 / P11 / issue #987` automation-only diagnostic correction

## Diagnosis

The licensed exact-SHA P11 result at `af910adb05f66f22198dd38c38397312723fa755` reports only the aggregate `native_undo / SEMANTIC_NATIVE_DIVERGENCE`. Source audit shows that `QS3DCURTAIN3D` queues `QS3DVIEW3D`, which in turn queues native `VPOINT` and `ZOOM E`, while the current runner requests only `UNDO 1`. A queued view command can therefore consume the single native Undo step before the Curtain build boundary is reached.

The current probe folds semantic-before equality, generated Handle-list equality, native absence of the after-generation, and source/sentinel preservation into one boolean, then assigns the semantic-divergence code to every false result. It cannot distinguish a view-only Undo from a native Curtain removal whose semantic snapshot failed to restore. No further production defect is proven by that aggregate marker.

## Reserved scope

- `scripts/test-bricscad-v25-curtain-panel-undo-reopen.ps1`: place an explicit native Undo mark after P11 prepare/source selection and use Undo Back after the Curtain baseline so queued post-build view commands cannot steal the intended boundary; retain the existing Redo proof.
- `src/QS3D.BricsCAD.V25/CurtainPanelUndoReopenRuntimeProbeCommands.cs`: publish only bounded sanitized Undo-branch booleans/codes separating after-generation still present, native after-generation removed but semantic before-state not restored, and source/sentinel drift.
- `scripts/preflight-curtain-panel-undo-reopen-runtime-probe.py`: lock the exact mark/build/back/check ordering, diagnostic allowlist and no-raw-data contract.
- this claim for remote-safe source/build handoff only. The licensed rerun and private local evidence remain with `/root`.

## Exclusions

- No edit to `CurtainWallUndoCoordinator`, `CurtainWallBuildCommands`, `ViewportCommands`, any production Curtain/Undo/Workspace/Health/Release service, Core, native geometry, ownership or persistence behavior.
- No P10, P12, LOCAL-003/004, private/customer data, BricsCAD launch or interaction, V26, release/signing/installer or GitHub Actions.
- No claim that the coordinator is defective unless the corrected licensed runner proves native after-output is absent while the semantic before-state remains unrestored.

## Collision audit

The prior local P11 probe/runner claim is `COMPLETED` and releases these three automation surfaces. The still-`ACTIVE` canonical issue `#987` claim owns production coordinator/build/lifecycle behavior and explicitly excludes the P11 automation files. Parent `/root` approved this disjoint successor after the exact tuple audit. Current open PR inventory contains no P11 or Curtain Undo automation work.

## Validation and completion

- PowerShell AST parser;
- focused P11, Curtain Undo/native-panel/orchestration/runtime-health gates;
- installed-reference BricsCAD V25 `Release|x64` build with zero warnings/errors;
- normal PR merge and exact merged-main SHA handoff to `/root` for the guarded licensed P11 rerun.

This claim remains `ACTIVE / PENDING_LOCAL` after remote integration until the corrected licensed P11 run records a truthful bounded PASS or a newly isolated source-blocking branch. Static/build evidence is not P11 `LOCAL_PASS`.
