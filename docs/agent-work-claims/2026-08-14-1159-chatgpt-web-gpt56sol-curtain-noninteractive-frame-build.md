# Agent work claim — Curtain3D non-interactive frame build

- Agent: `chatgpt-web-gpt56sol-curtain-noninteractive-frame-build-20260814-1159`
- Date: 2026-08-14
- Status: `COMPLETED / SOURCE_FIXED / PENDING_LOCAL`
- Issue: `#1106`
- Base observed before claim: `4dfa565add2ed14df22083b5e3300974e6173778`
- Claim commit: `8a5db72ab14c28282bbe282a9ffa32bcf710f59f`

## Goal

Eliminate the remaining interactive selection fallback inside the canonical-prevalidated `QS3DCURTAIN3D` build path. A production Curtain3D run that already validated and partitioned its source selection must never fall back to a second `Editor.GetSelection()` prompt inside LINE/path frame builders.

## Reserved paths

- `src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs`
- `scripts/preflight-curtain-noninteractive-frame-build.py`
- `docs/agent-work-claims/2026-08-14-1159-chatgpt-web-gpt56sol-curtain-noninteractive-frame-build.md`

## Evidence

The exact-SHA licensed P10 rerun recorded on issue #1106 still timed out after `source_selection_prepared` and never reached `curtain_build_complete` after the empty-partition fix. Remote audit of all six Curtain3D builder phases showed:

- LINE/path host builders consume implied selection only and do not call `GetSelection()`;
- LINE/path panel builders consume implied selection only and do not call `GetSelection()`;
- both LINE and path frame builders retained fallback from `SelectImplied()` to interactive `Editor.GetSelection()`.

That interactive fallback is valid for standalone frame commands but invalid after `QS3DCURTAIN3D` has already canonical-prevalidated the selection.

## Result

- `423fbc9b6c916d807c8fb0b3a7e591bfadc0f25e` — `fix(curtain): make LINE frame selection mode explicit`
  - LINE frame builder keeps `allowInteractiveSelection = true` by default for standalone use.
  - non-interactive callers fail closed before `Editor.GetSelection()`.
- `204f5cd28cbde3905e2a8bfedc766e90161e8fbf` — `fix(curtain): make path frame selection mode explicit`
  - path frame builder has the same explicit standalone/non-interactive selection contract.
- `cc8c866b5186aad96ec842e8921813f6448bcd0d` — `fix(curtain): keep aggregate frame build noninteractive`
  - `QS3DCURTAIN3D` passes `allowInteractiveSelection: false` to both LINE and path frame builders after applying the canonical partition selection.
  - existing six-phase order, empty-partition skips, failure injection, outer transaction, Undo registration, stamping and selection restoration remain unchanged.
- `e17a203d287f81d2ddae7b750a972cfd6d8ed53f` — `test(preflight): guard noninteractive Curtain3D frames`
  - static gate verifies aggregate non-interactive calls, all six partition guards, standalone interactive defaults, and fail-closed guards before `Editor.GetSelection()`.

## Validation

- Exact-source GitHub read-back: `PASS` for aggregate calls and both builder selection contracts.
- Aggregate discovery contract: `PASS` by source inspection; `scripts/preflight-all.py` auto-discovers the new `scripts/preflight-curtain-noninteractive-frame-build.py` gate.
- Focused preflight execution: `NOT_RUN` in this web-only connector lane; no local checkout/toolchain is available.
- GitHub Actions: `NOT_DISPATCHED` by this lane.
- Licensed BricsCAD V25 P10: `PENDING_LOCAL`; issue #1106 must remain open until the unchanged guarded runner reaches `curtain_build_complete` and the full P10 marker on an exact descendant SHA.

## Boundaries preserved

- No frame geometry/layout/ownership calculation was changed.
- No local runner behavior or acceptance criterion was weakened.
- No `LOCAL_PASS` or native/runtime PASS is claimed from source/static evidence.
