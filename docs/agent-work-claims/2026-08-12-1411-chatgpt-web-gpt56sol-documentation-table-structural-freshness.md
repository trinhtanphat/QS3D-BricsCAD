# Work claim — Documentation table structural freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-documentation-table-structural-freshness`
- Registered: `2026-08-12T14:11:00+07:00`
- Baseline main SHA observed: `1d5cfdef6892d3ccfdfc13d3f858a1419fc605b8`
- Priority: P1 — reject caller-enumeration project drift before returning a semantic documentation table.

## Confirmed defect

`SemanticDocumentationTableBuilder.Build(...)` materializes caller-controlled `orderedElementIds` and `columns` before constructing `SemanticTagRenderContext`. `ProjectState.Elements` is publicly mutable, so a lazy input can remove or replace an element directly without calling `Touch()`. The current builder can then render from the structurally changed project and return an apparently valid table even though the project changed during planning.

Concrete counterexample: the project initially contains `E1`; lazy row-id enumeration replaces that list entry with a different `ProjectElement` instance also named `E1` without advancing `ChangeVersion`, then yields `E1`. Current code resolves and renders the replacement instead of rejecting stale input.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationTableBuilder.cs` — project revision/element-ownership freshness around caller enumeration only
- focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Snapshot `ChangeVersion` and the ordered element instance sequence before enumerating caller row IDs/columns.
- After external enumeration, reject revision drift or direct list count/order/instance replacement drift before tag resolution/rendering.
- Re-check the same freshness immediately before returning the table.
- Preserve title/id/column validation order, row/column bounds, existing duplicate/missing/tag semantics, valid output ordering, and read-only result snapshots.
- Do not introduce new global duplicate-ID policy for malformed projects.

## Excluded scope

- `SemanticTagRenderer`, `SemanticTagRenderContext`, Semantic Schedule persistence/placement, native CAD tables, UI, BricsCAD runtime, GitHub Actions, release/package work.
