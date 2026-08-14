# Work claim — Level-resolved Curtain frame Z placement

- Status: `COMPLETED`
- Phase: `REBAR_SOURCE_COMPLETE / PENDING_LOCAL`
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

## Implementation candidate — 2026-08-14

On synchronized baseline `d321660c632b2c66cf0cefe78c9c0ecea93bb198`, both native frame builders now preserve the historical legacy/no-Level box-origin translation but omit the duplicate `-height/2` Z translation when `CadElementVerticalPlacement.UsesBottomLevel` is true. BricsCAD V25 `Solid3d.CreateBox` is centered, so the Level branch now places each frame piece around the already-computed `resolved bottom + piece Z + piece height/2` center instead of lowering it by another half piece height. LINE and path placement use the same bounded correction.

The focused Level/Curtain placement, opening, path, noninteractive, ownership, health and native-panel gates pass. Core Release builds with zero warnings/errors, the complete Core smoke reports `ALL PASS`, and the installed-reference BricsCAD V25 `Release|x64` adapter builds with zero warnings/errors. Aggregate discovery ran all 787 gates and found four pre-existing frame/orchestration isolation failures whose start tokens still require the old two-argument public builder signatures; current `origin/main` already exposes the newer optional `allowInteractiveSelection` parameter from issue `#1106`. This lane did not edit those unrelated gates or weaken their contracts.

At candidate time the claim remained `ACTIVE / SOURCE_READY / PENDING_MERGE`. No BricsCAD runtime or GitHub Actions were run before merge; the completion record below is authoritative.

## Completion record

- Claim-only PR `#1128` merged as `734af3dd1f5e9c1459416e14c128a4fe2483f002` before implementation began.
- Implementation source commit `e3d201c958480a1d830eb39106d29344e74658f7` merged through PR `#1133` as exact production SHA `8676b6a8430062931356be7dca3bace268ca233d`.
- Focused Level/Curtain placement, opening, path, noninteractive, ownership, Health and native-panel gates passed; Core Release built with zero warnings/errors; Core smoke returned `ALL PASS`; installed-reference BricsCAD V25 `Release|x64` compiled with zero warnings/errors.
- Aggregate preflight discovered 787 gates and retained four pre-existing frame/orchestration isolation failures caused by stale two-argument builder start tokens after issue `#1106`; this correction did not edit those unrelated gates.
- GitHub Actions and BricsCAD runtime were not run. Issue `#1125` remains `OPEN / PENDING_LOCAL`, and the parent LOCAL-003 owner must rerun the guarded Level matrix on exact SHA `8676b6a8430062931356be7dca3bace268ca233d` before any `LOCAL_PASS` or issue closure.

## Reactivation — Level-driven rebar placement

- Reactivated: `2026-08-14T12:37:46+07:00`
- Successor baseline main SHA: `97ba91295b1c5f4c61888b876502aaa878a3e536`
- Trigger: the licensed LOCAL-003 rerun on exact source SHA `8676b6a8430062931356be7dca3bace268ca233d` verified legacy `1.2 m .. 3.7 m`, GlassWall host `3 m .. 7 m`, Curtain frames `3 m .. 7 m`, and Curtain panels `3.05 m .. 6.95 m`, then advanced to sanitized `LEVEL_Z_RUNTIME_REBAR_FAILED` with complete cleanup.

### Reserved successor scope

Diagnose and correct only the production generated-rebar/native-mesh vertical placement that diverges from the already-resolved Level host. Preserve legacy/no-Level placement, existing rebar/fabrication topology, counts, ownership, semantic snapshots, transactions and fail-closed invalid-Level behavior.

Expected implementation surfaces, narrowed to the actually implicated builder(s) after post-claim source/probe audit:

- `src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/ColumnTieSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/StructuralWallMeshSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs`
- `scripts/preflight-level-rebar-placement.py` and focused CAD-independent smoke only if the existing gate cannot lock the corrected production handoff
- this claim file for source completion and exact merged rerun SHA

### Successor exclusions and validation

Do not edit the Level runtime probe/runner/gate, LOCAL-003 execution docs/inbox, Curtain builders, host placement, Level resolver/UI, private data, GitHub Actions, V26, release or packaging. Run focused Level/Rebar gates, Core Release smoke and installed-reference V25 `Release|x64` compile without launching BricsCAD. Merge the smallest correction, record the exact production SHA on issue `#1125`, then return the licensed rerun to the parent LOCAL-003 owner. Source evidence cannot claim `LOCAL_PASS`.

### Coordination

Parent task `/root` explicitly delegated this CAD-independent successor to `/root/fix_level_curtain_frame_z`. The parent LOCAL-003 claim retains the runner/probe, exact-SHA licensed execution, cleanup and final status, and will not concurrently edit the reserved rebar placement surfaces while this reactivated claim is `ACTIVE`.

## Rebar successor completion record

- Claim-only PR `#1141` merged as `2b9e7371b3886d23636b0ab5b1a247f3a5faaa53` before the rebar implementation began.
- The post-claim audit narrowed the failing runtime path to `BeamRebarSolidBuilder.BuildSelected(...)` followed by `BeamStirrupSolidBuilder.BuildSelected(...)`. Longitudinal bar Z centers already consume `CadElementVerticalPlacement.CenterDrawing` and stay inside the host envelope; no longitudinal or non-beam builder was changed.
- BricsCAD V25 `Solid3d.CreateFrustum` is centered at the world origin, but the beam-stirrup cylinder helper translated each centered native segment to its validated start endpoint. Vertical legs therefore extended about half a leg beyond the intended section. The correction translates each frustum to `start + unit * length / 2`, preserving the planner path, overlap, topology, ownership, transactions, metadata and vertical snapshots.
- Implementation source commit `31e31e42fdd3e5f1a43a591278f2d0fe84c4d940` merged through PR `#1144` as exact production SHA `5972a5bfda3a20df549e75364b40b0824286f162`.
- Focused Level/Rebar placement, Beam stirrup/rebar lifecycle, bend/hook and single-bind gates passed. Core Release built with zero warnings/errors; complete Core smoke returned `ALL PASS`; installed-reference BricsCAD V25 `Release|x64` compiled with zero warnings/errors.
- GitHub Actions and BricsCAD runtime were not run. Issue `#1125` remains `OPEN / PENDING_LOCAL`, and the parent LOCAL-003 owner must rerun the guarded Level matrix on exact SHA `5972a5bfda3a20df549e75364b40b0824286f162` before any `LOCAL_PASS` or issue closure.
