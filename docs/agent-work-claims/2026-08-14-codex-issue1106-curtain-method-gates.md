# Work claim — Curtain frame method-signature gate reconciliation

- Status: `COMPLETED`
- Agent: `/root/fix_curtain_method_gates`
- Registered: `2026-08-14T12:41:10+07:00`
- Baseline main SHA: `56d85ae606d93b282d28986a04632f87aed504e9`
- Priority: issue `#1106` / LOCAL-002 P10 source-ready prerequisite; aggregate preflight is blocked by stale static method/call tokens after the noninteractive frame-builder correction

## Reserved scope

Reconcile exactly three Curtain static gates with the intentional optional `allowInteractiveSelection` builder signatures and the aggregate `QS3DCURTAIN3D` calls that explicitly disable interactive selection. Preserve every existing atomicity, transaction, phase-order, selection, Undo, ownership and post-commit assertion.

## Expected surfaces

- `scripts/preflight-curtain-frame-atomicity.py`
- `scripts/preflight-curtain-frame-transaction-boundary.py`
- `scripts/preflight-curtain-orchestration-boundary.py`
- this claim file and the parent issue `#1106` claim for coordination/close-out only

## Excluded scope

- No production C# edits, builder behavior changes, geometry/layout/ownership/Health/Level changes, or new runtime contract.
- No P10 probe/runner/local evidence, BricsCAD launch, private/customer data, V26, packaging, release, signing, or GitHub Actions operation.
- No edits to neighboring Curtain gates beyond the three exact scripts above.
- No edit to `scripts/preflight-curtain-empty-partition-orchestration.py`, which a newer concurrent ACTIVE claim reserved before this claim was published.

## Validation plan

- Run each of the three owned focused static gates after the bounded token reconciliation; consume but do not overwrite the concurrent winning empty-partition gate when it lands.
- Run aggregate `scripts/preflight-all.py` and preserve all unrelated gate contracts.
- Run the installed-reference BricsCAD V25 `Release|x64` build without launching BricsCAD, when the local SDK references are available.
- Run `git diff --check`; do not claim licensed runtime evidence.

## Coordination

Parent task `/root` explicitly delegated this remote-safe stale-gate lane. The ACTIVE issue `#1106` and LOCAL-002 P10 claims retain production/runtime ownership and the exact licensed P10 rerun. The completed noninteractive frame-builder claim owns the intentional production contract. Concurrent claim `2026-08-14-1238-gpt56sol-curtain-empty-partition-preflight-reconcile.md` owns the empty-partition gate; this claim was narrowed before publication to the three disjoint stale consumers listed above. The ACTIVE Level/rebar and Curtain Undo claims own disjoint production/runtime surfaces.

## Completion condition

The three owned focused gates pass, the concurrent empty-partition winner is integrated, aggregate preflight records the bounded Curtain result and any unrelated active-lane failures, the installed-reference V25 build passes, the bounded patch is merged to current `main`, and the exact validated descendant SHA is returned to `/root` for the licensed P10 run.

## Completion record — 2026-08-14

- Claim PR `#1143` merged as `e6230682ee8ef0dea0abe44ffd35a5a0cfec9087` before implementation.
- Concurrent empty-partition gate correction `181c81517` was consumed without overlap; its claim closed on `main` as `6f47dec11`.
- Implementation commit `1042a074b8e837446cb25c1f9740f8c9645bcab0` merged through PR `#1146` as `1d8e82f382e74b03f6b9c39fd86e14f7ea8c7f47`.
- Exact descendant `ef279421599d30ebc2d156542dd22e71d2741138` passed all three owned gates, the concurrent empty-partition gate, the existing noninteractive-frame gate, and the installed-reference BricsCAD V25 `Release|x64` build with zero warnings and zero errors.
- Aggregate `scripts/preflight-all.py` executed all 788 discovered gates on the bounded implementation patch. All four issue `#1106` Curtain gates passed; the overall aggregate remained blocked only by three independent active UI/Ribbon lanes: `preflight-create-similar.py`, `preflight-plan-to-3d-finish-workflow.py`, and `preflight-ribbon-augmenter-panel-targets.py`. Parent `/root` explicitly accepted recording those unrelated failures instead of waiting or expanding scope.
- No production C#, P10 probe/runner, local evidence, private data, BricsCAD runtime, GitHub Actions, release, packaging or signing surface was changed or operated.
- Licensed P10/P11 execution remains with `/root`; source/static/build evidence is not `LOCAL_PASS`.
