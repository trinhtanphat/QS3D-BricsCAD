# Agent work claim — Curtain3D non-interactive frame build

- Agent: `chatgpt-web-gpt56sol-curtain-noninteractive-frame-build-20260814-1159`
- Date: 2026-08-14
- Status: `ACTIVE`
- Issue: `#1106`
- Base observed before claim: `4dfa565add2ed14df22083b5e3300974e6173778`

## Goal

Eliminate the remaining interactive selection fallback inside the canonical-prevalidated `QS3DCURTAIN3D` build path. A production Curtain3D run that already validated and partitioned its source selection must never fall back to a second `Editor.GetSelection()` prompt inside LINE/path frame builders.

## Reserved paths

- `src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs`
- `scripts/preflight-curtain-noninteractive-frame-build.py`
- `docs/agent-work-claims/2026-08-14-1159-chatgpt-web-gpt56sol-curtain-noninteractive-frame-build.md`

## Evidence

The exact-SHA licensed P10 rerun recorded on issue #1106 still timed out after `source_selection_prepared` and never reached `curtain_build_complete` after the empty-partition fix. Current remote audit of all six Curtain3D builder phases shows:

- LINE/path host builders consume implied selection only and do not call `GetSelection()`;
- LINE/path panel builders consume implied selection only and do not call `GetSelection()`;
- both LINE and path frame builders still fall back from `SelectImplied()` to interactive `Editor.GetSelection()`.

That interactive fallback is valid for standalone frame commands but is invalid after `QS3DCURTAIN3D` has already canonical-prevalidated the selection.

## Planned fix

- keep the existing interactive fallback as the default for standalone frame commands;
- add an explicit non-interactive mode to both frame builders;
- when non-interactive mode cannot read the implied selection, fail closed instead of opening a prompt;
- call both frame builders from `QS3DCURTAIN3D` with non-interactive mode;
- add a focused static regression guard that proves the production aggregate command cannot route through interactive frame fallback;
- do not claim licensed P10 PASS until the unchanged exact-SHA runner is rerun locally.

## Boundaries

- Preserve empty-partition skips, six-phase ordering, outer transaction, failure injection, rollback, Undo integration, post-commit stamping and selection restoration.
- Do not modify frame geometry/layout/ownership calculations.
- Do not alter local runner behavior or weaken P10/LOCAL-002 acceptance.
- Do not dispatch GitHub Actions from this lane.
