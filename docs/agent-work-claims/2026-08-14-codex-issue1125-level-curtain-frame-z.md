# Work claim — Level-resolved Curtain frame Z placement

- Status: `ACTIVE`
- Agent: `codex-issue1125-level-curtain-frame-z-20260814` (`/root/fix_level_curtain_frame_z`, remote-safe source lane delegated by `/root`)
- Registered: `2026-08-14T12:17:22+07:00`
- Baseline main SHA: `8480fdfb8bfb26bb5195a07e179579f3c6dbff52`
- Priority: GitHub issue `#1125` / `LOCAL-003 P0` production defect reproduced on licensed BricsCAD V25

## Reserved scope

Correct the existing production LINE/open-POLYLINE Curtain frame native placement so Level-configured frame output consumes the same resolved host base Z and effective height already used by the GlassWall host and generated panels. Preserve the byte-for-byte legacy/no-Level placement path and the current fail-closed invalid-Level boundary.

The source correction is limited to the exact frame placement handoff. It does not redesign Curtain layout/topology, opening clipping, ownership, semantic snapshots, fingerprints, transactions, or Level resolution.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs`
- `scripts/preflight-level-curtain-placement.py` only when the existing source-contract gate must be strengthened for the corrected placement handoff
- focused deterministic smoke coverage only if the production audit exposes a CAD-independent seam that is not already exercised by the Level/Curtain gates
- this claim file for source-ready completion and exact merged rerun SHA

## Excluded scope

- No edits to `LevelZRuntimeProbeCommands.cs`, `scripts/test-bricscad-v25-level-z.ps1`, its runtime-probe gate, native backup cleanup, or any licensed-runtime automation/evidence surface.
- No edits to `CadElementVerticalPlacement`, `ElementVerticalPlacementService`, Level assignment UI, host/panel builders, Curtain planners, opening interruption, ownership/Health/fingerprints, P10/P11, Source Reconcile, or issue `#1106`.
- No BricsCAD execution, private/customer DWG, V26, installer, signing, release, GitHub Actions, or workflow operation.

## Validation plan

- Re-fetch current `main` and re-scan exact path/symbol claims immediately before every source/test write and PR merge.
- Add deterministic coverage proving both straight and path frame builders pass the resolved Level host base Z/effective height to native frame placement while retaining the legacy/no-Level arithmetic unchanged.
- Run focused Level/Curtain placement and frame gates, complete Core Release smoke, and the installed-reference BricsCAD V25 `Release|x64` compile without launching BricsCAD.
- Merge the smallest production correction and hand the exact merged SHA back to the existing LOCAL-003 owner for a fresh licensed run; do not claim `LOCAL_PASS` from source/static/build evidence.

## Coordination

The ACTIVE parent claim `2026-08-11-codex-local-019ff0c5-local003-level-z-chain.md` owns licensed LOCAL-003 qualification and the runtime probe. Its local owner explicitly delegated this CAD-independent source defect after exact SHA `945f26795725114c33251fc6eca031458e59fd1e` returned `LEVEL_Z_RUNTIME_CURTAIN_RANGE_FAILED`. This claim owns only the production frame-placement correction and its deterministic source contract; the parent claim retains the exact-SHA rerun and final runtime status.

The ACTIVE Curtain Undo/P10/empty-partition claims own different transaction, selection, and partition boundaries and exclude Level placement. The ACTIVE tiny-ratio claim owns only `CurtainWallLayoutPlanner.DivisionCount(...)`. No open PR or other ACTIVE/BLOCKED claim found at the baseline reserves the two frame placement builders or issue `#1125`.

## Completion condition

The bounded production fix and deterministic regressions are merged to current `main`; source/static/Core smoke and installed-reference V25 compile pass; issue `#1125` records the exact merged rerun SHA; this claim is `COMPLETED`; and licensed Level runtime evidence remains with the parent LOCAL-003 owner.
