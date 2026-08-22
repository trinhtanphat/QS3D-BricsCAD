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
