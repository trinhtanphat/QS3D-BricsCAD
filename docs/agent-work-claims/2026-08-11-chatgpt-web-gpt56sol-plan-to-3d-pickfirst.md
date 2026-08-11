# Agent work claim — Plan-to-3D PICKFIRST

- Agent: `chatgpt-web-gpt56sol-plan-to-3d-pickfirst`
- Registered: `2026-08-11T21:53:00+07:00`
- Status: `ACTIVE`
- Baseline main SHA: `b33459e74ce1229e4b42cfe78190c5d3e6063ef7`

## Scope

Reduce redundant selection in the 2D-plan -> 3D wall workflow by making the existing implied-selection-first acquisition contract explicit at the BricsCAD command boundary.

Reserved implementation paths:
- `src/QS3D.BricsCAD.V25/PlanTo3DCommands.cs`
- `scripts/preflight-plan-to-3d-pickfirst.py`
- `docs/DIRECT-DRAW-PLAN-TO-3D-PICKFIRST-2026-08-11.md`
- this claim file for close-out

Target commands:
- `QS3DCONVERT2D`
- `QS3DPLAN2WALLS`
- `QS3DCONVERT2DADV`

Planned source-safe contract:
- add `CommandFlags.UsePickSet` to the three commands already implemented around `AcquireSelection(...)`;
- preserve the existing `SelectImplied()` -> explicit `GetSelection()` fallback without altering source freshness, project preview/resolve ordering, semantic capture, scoped regeneration, native ownership or rollback;
- do not infer sources from geometry and do not broaden supported source types beyond LINE/open POLYLINE;
- add a focused static guard and local V25 qualification notes.

## Boundaries

- No DrawJig/transient/repeated-mode implementation.
- No changes to source geometry freshness logic from the completed `plan-to-3d-source-freshness` claim.
- No GitHub Actions dispatch.
- Exact BricsCAD V25 PICKFIRST/editor/document-switch behavior remains `LOCAL_ONLY` under existing local qualification gates; source/static completion must not be reported as runtime PASS.
