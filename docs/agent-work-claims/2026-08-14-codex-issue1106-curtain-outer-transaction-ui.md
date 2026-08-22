# Work claim — Curtain3D aggregate post-commit UI boundary

- Status: `ACTIVE`
- Agent: `/root/fix_curtain_method_gates`
- Registered: `2026-08-14T13:17:39+07:00`
- Baseline main SHA: `30afa77de4cf2db06af41e2685a637f4323fe350`
- Priority: GitHub issue `#1106` / LOCAL-002 P10 production blocker

## Exact diagnosis

The clean exact-SHA P10 run on `6b6cdb5ef4449c9de9930515304b08b3ca949180` timed out after 1200 seconds at `source_selection_prepared`, with no final marker and clean process/DWG/sidecar/UI restoration. Current source already uses `CommandFlags.UsePickSet`, validates and partitions the canonical selection, skips empty partitions, and suppresses interactive frame selection. No aggregate picker remains after canonical selection.

The first LINE host phase still calls `WallSolidBuilder.BuildSelectedLineWalls` while the aggregate command transaction is open. That builder commits its nested native transaction and synchronously calls `CadPostCommitUi.TryRegen`, whose `Editor.Regen()` can block without throwing before control returns to the aggregate command. The path host builder has the same boundary. These are the only synchronous Editor UI calls before the aggregate outer commit and therefore the bounded source explanation for the retained timeout; exact runtime confirmation remains a licensed P10 rerun.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs`: optional post-commit UI flag, defaulting to the existing standalone behavior, guarding only the final UI refresh.
- `src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs`: symmetric optional flag and guard.
- `src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs`: pass UI suppression only for the two aggregate host calls; retain aggregate `FinalizeUi` after outer commit.
- `scripts/preflight-curtain-aggregate-postcommit-ui.py`: focused static boundary contract.
- `scripts/preflight-curtain-empty-partition-orchestration.py` and `scripts/preflight-curtain-orchestration-boundary.py`: exact-call token updates required by the optional arguments.
- `scripts/preflight-native-builder-post-commit-ui.py`, `scripts/preflight-generated-replacement-atomicity.py`, and `scripts/preflight-geometry-completion.py`: signature-isolation/token updates only, after aggregate validation identified them as stale direct consumers of the same optional builder parameters.
- this claim and the parent issue-1106 claim for ownership/closeout only.

## Coordination and invariants

- Parent `/root` retains the P10/P11 runner, probe, local evidence and licensed rerun.
- The active issue-987 Curtain Undo claim retains Undo coordinator/marker/snapshot/transaction semantics; this lane changes only the two host call arguments inside the existing orchestration.
- The active Build3D single-native-touch claim is disjoint and its required standalone builder UI behavior is preserved by default `true` parameters.
- Preserve source selection, empty-partition guards, six-phase order, failure hooks, geometry, ownership, fingerprints, rollback/Undo behavior, outer transaction, and final aggregate UI refresh.

## Validation

- focused aggregate post-commit UI, empty-partition, orchestration-boundary and existing Curtain P08/P09/P10/P11/noninteractive gates;
- Core smoke and aggregate preflight, recording unrelated active failures;
- installed-reference BricsCAD V25 `Release|x64` build with zero warnings/errors;
- clean diff proving no runner/probe/docs implementation change and no production change beyond the three reserved source files.

## Exclusions

No P10 runner/probe/local-evidence edit or execution; no BricsCAD launch/private data/Actions; no frame/panel builder changes; no selection/UsePickSet change; no outer transaction, Undo, geometry, ownership, Health, Level, V26, release or signing work.

Completion means the bounded source/gate PR is merged, this claim is closed, and the exact merged-main SHA is returned to `/root` for the licensed P10 rerun. Static/build success is not P10 `LOCAL_PASS`.
