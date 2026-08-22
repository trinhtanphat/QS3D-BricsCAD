# Work claim — Curtain3D empty-partition prompt refusal

- Status: `ACTIVE`
- Agent: `codex-issue1106-curtain-empty-partition-20260814`
- Registered: `2026-08-14T09:23:00+07:00`
- Baseline main SHA: `77ebd673a9f81ca3628e75328319427fa298a33f`
- Priority: GitHub issue `#1106` / LOCAL-002 P10 production blocker reproduced on licensed BricsCAD V25

## Verified defect

The checkpointed exact-SHA P10 run reached `source_selection_prepared` but never reached `curtain_build_complete`. `QS3DCURTAIN3D` correctly prevalidates and partitions the canonical selection into LINE and open-POLYLINE source ids, yet it unconditionally invokes all six LINE/path host/frame/panel builders. With a LINE-only batch, the command applies an empty path selection and then `CurtainWallPathFrameSolidBuilder` falls back to interactive `Editor.GetSelection()`. The symmetric path-only batch can reach the LINE-frame fallback. A prevalidated one-partition command therefore waits for an unrelated second selection.

Older runtime scripts append `P` plus Enter after `QS3DCURTAIN3D`, masking this product defect. The P10 runner does not, and must not, use that workaround.

## Reserved scope

- `src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs`: add only per-partition non-empty guards around the existing six ordered builder calls.
- new focused static contract under `scripts/` proving each LINE/path host/frame/panel call is skipped when its already-validated partition is empty, without changing the six-phase order.
- `scripts/preflight-curtain-native-panels.py` only if the existing orchestration contract must be strengthened to consume the new focused gate.
- `docs/CURTAIN-NATIVE-PANELS.md`, `docs/LOCAL-AGENT-INBOX.md`, the active P10 claim, and this claim for exact-SHA diagnosis/handoff.
- exact-SHA V25 P10 rerun on a fresh repository-synthetic disposable copy after integration.

## Coordination with active issue #987

The ACTIVE Curtain Undo/Redo claim `2026-08-13-codex-issue987-curtain-native-undo.md` also names `CurtainWallBuildCommands.cs`, but its source implementation is already integrated and it remains active only for the guarded P11 runtime result. This claim owns a disjoint hunk: six conditional builder invocations between canonical partitioning and the existing failure-injection hooks. It will not edit `CurtainWallUndoCoordinator`, `BeginTransition`, `StageAfter`, `ConfirmCommitted`, snapshot capture/restore, transaction boundaries, lifecycle cleanup, or P11 automation. Both static contracts must pass before integration.

## Required invariants

- Preserve canonical source prevalidation before regeneration/native mutation.
- Preserve LINE host → path host → LINE frame → path frame → LINE panel → path panel order for non-empty partitions.
- Preserve each existing failure-injection hook immediately after its corresponding invoked/skipped phase and before outer commit.
- Preserve the single outer native transaction, semantic rollback snapshot, selected-owner Undo snapshots, final complete selection, live/config fingerprint stamping, post-commit warning behavior, and UI refresh boundary.
- Do not modify builder fallback behavior globally; other direct builder callers retain their current interaction contract.
- Do not add script tokens that answer an unexpected selection prompt.

## Validation plan

- Focused static gate for LINE-only, path-only and mixed orchestration tokens/order.
- Existing Curtain native-panel/orchestration/P08/P09/P11/P10 gates.
- installed-reference BricsCAD V25 `Release|x64` build with zero warnings/errors.
- Core smoke and aggregate preflight when current-main independent fixtures allow; record any unrelated blocker exactly.
- Clean exact-SHA P10 run requiring progress beyond `source_selection_prepared`, then the full sanitized Workspace/Health/Release marker and all process/script/private-state/DWG/UI-layout cleanup postconditions.

## Exclusions

No builder geometry/planner/ownership/Health/Level changes; no Direct Draw change; no P01-P09/P11/P12 runner edits; no Source Reconcile/LOCAL-004; no private/customer DWG; no V26/release/signing; no GitHub Actions; no overall LOCAL-002 promotion without complete bounded runtime evidence.

## Integrated source result — licensed rerun pending

- Claim PR `#1107` merged before production edits.
- Source commit `e03150ff3a1f87a3054377c36f771f417b3a50b6` merged through PR `#1109` at `50d074eb579a3e9503b623fd37fd50a4e82c5b9b` and closed issue `#1106`.
- `CurtainWallBuildCommands` now skips absent LINE/path host, frame and panel builder calls while retaining every phase label/failure hook, the outer transaction, rollback/Undo registration, final complete selection, fingerprint and UI ordering.
- The focused empty-partition, P10, native-panel, orchestration, P08, P09 and P11 gates passed; the installed-reference V25 Release|x64 build passed with zero warnings/errors. Aggregate preflight reached `783/783 PASS` after unrelated gate reconciliation.
- Exact current `main` `16d47ef46c4a955587f398d5597fb84ebce32c2e` builds adapter SHA-256 `A259BCF151D50B1BE9E6EC10EAAD469AC6E6E0FEFE6F01D9BD4A839E6E91E3B1`; P10/empty-partition focused gates pass on that checkout.

The exact licensed completion run is still pending the runner's mandatory zero-preexisting-BricsCAD-process boundary. A refused launch did not build a disposable copy, start another host or create private artifacts, and no existing process was terminated. This claim remains `ACTIVE` until a fresh exact-SHA P10 run advances beyond `source_selection_prepared` and produces the full sanitized Workspace/Health/Release result with all cleanup postconditions. Neither the source merge nor the static/build evidence is a P10 or overall LOCAL-002 `LOCAL_PASS`.

## Exact post-fix rerun — issue reopened

The zero-process precondition later cleared and a fresh clean run used exact SHA `82901db277eba9e685ebc356e97375138d0d538d`, BricsCAD `25.2.10` x64 and adapter SHA-256 `1DB2A88E7769BF53398652811CC1EFBBB54A975A286A374B095E03BB6C971DA1`. V25 Release|x64 built with zero warnings/errors, Core smoke reported `ALL PASS`, and the focused P10/empty-partition/native-panel gates passed. The unchanged guarded runner still timed out after 900 seconds at `source_selection_prepared`, with a null final marker and without reaching `curtain_build_complete`.

All failure cleanup postconditions passed: zero remaining BricsCAD processes, script/private-state/UI-layout restoration, zero launcher handoffs, zero sidecars and disposable-DWG restoration to SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`. The local worker did not interact with the retained command or edit production source. Issue `#1106` is reopened for a remote claim-first audit of the current LINE-only `QS3DCURTAIN3D` path, including nested selection fallbacks or UI boundaries after canonical selection. This claim remains `ACTIVE`; source/static success is not P10 `LOCAL_PASS`.

## 2026-08-14 stale-gate reconciliation split

Parent task `/root` delegated the stale aggregate-preflight consumers to `/root/fix_curtain_method_gates` under claim `2026-08-14-codex-issue1106-curtain-method-gates.md`. A newer concurrent ACTIVE claim already owns `preflight-curtain-empty-partition-orchestration.py`, so this delegated lane was narrowed before publication to `preflight-curtain-frame-atomicity.py`, `preflight-curtain-frame-transaction-boundary.py`, and `preflight-curtain-orchestration-boundary.py`. It will align only those three exact method/call tokens with the already-merged optional `allowInteractiveSelection` signatures without changing production source or runtime automation. This claim retains issue `#1106`, LOCAL-002 P10 and the exact licensed rerun.

The three-gate lane completed through PR `#1146`, merged as `1d8e82f382e74b03f6b9c39fd86e14f7ea8c7f47`. Exact descendant `ef279421599d30ebc2d156542dd22e71d2741138` passes those three gates, the independently reconciled empty-partition gate, the noninteractive-frame gate and the installed-reference V25 `Release|x64` build with zero warnings/errors. Aggregate execution recorded three unrelated active UI/Ribbon gate failures; parent `/root` accepted those as out of scope. This claim still retains the licensed P10/P11 rerun and remains `ACTIVE / PENDING_LOCAL`.

## 2026-08-14 aggregate post-commit UI split

After exact candidate `6b6cdb5ef4449c9de9930515304b08b3ca949180` still timed out for 1200 seconds at `source_selection_prepared`, parent `/root` delegated a bounded remote-safe source audit to `/root/fix_curtain_method_gates` under claim `2026-08-14-codex-issue1106-curtain-outer-transaction-ui.md`. That lane owns only optional default-preserving post-commit UI flags in the LINE/path host builders, the two aggregate host-call arguments, and their focused/static gate tokens. This parent claim retains all P10/P11 automation, private/local evidence and licensed runtime execution; no runner/probe/docs implementation is delegated.

The bounded source lane completed through PR `#1163`, merged at `03bd7e014ddf0cfeee69824b20a649f1a2e3140e`. `QS3DCURTAIN3D` now suppresses the LINE/path host builders' synchronous post-commit Regen while its outer transaction is open and retains the aggregate final UI refresh after commit; standalone builder calls keep their previous default UI behavior. Focused static gates and the installed-reference V25 build passed. This parent claim remains `ACTIVE / PENDING_LOCAL` for the exact licensed P10/P11 rerun; the source merge is not runtime qualification.
