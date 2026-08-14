# Work claim — Curtain frame method-signature gate reconciliation

- Status: `ACTIVE`
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

The three owned focused gates pass, the concurrent empty-partition winner is integrated, aggregate preflight and installed-reference V25 build pass, the bounded patch is merged to current `main`, this claim is marked `COMPLETED` with exact SHAs, and the exact merged main SHA is returned to `/root` for the licensed P10 run.
