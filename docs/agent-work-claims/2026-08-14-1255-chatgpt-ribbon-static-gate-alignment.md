# Agent work claim — Ribbon static gate alignment

- Agent: `chatgpt-web-gpt56sol-ribbon-static-gate-alignment`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `9133fabbec2866001c9499bd613911f572417099`

## Goal

Reconcile three stale aggregate preflight consumers with the already-merged `QuickWorkflowRibbonAugmenter` grouped Author/Draw panel architecture. This is a static-gate correction only; production Ribbon behavior is read-only.

## Reserved paths

- `scripts/preflight-create-similar.py`
- `scripts/preflight-plan-to-3d-finish-workflow.py`
- `scripts/preflight-ribbon-augmenter-panel-targets.py`
- `docs/agent-work-claims/2026-08-14-1255-chatgpt-ribbon-static-gate-alignment.md`

Read-only evidence:

- `src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs`
- current Ribbon/bootstrap/coordinator sources and relevant workflow docs

## Evidence

The current augmenter intentionally uses `AuthorPanelSourceId`, shared `CreatePanel(...)`, `EnsureButtons(...)`, and `FindById(items as IEnumerable, spec.Id) ?? FindByText(...)` while also augmenting grouped Draw/IFC panels. Three older gates still require pre-refactor names such as `PanelSourceId`, `CreateQuickPanel`, and exact `FindById(items, spec.Id)` text. PR #1146 aggregate evidence identified these UI/Ribbon lanes as the only unrelated aggregate blockers at that checkpoint.

## Boundaries

- Do not edit production Ribbon/UI source, startup lifecycle, LOCAL runners, or GitHub Actions.
- Preserve stable Create Similar button ID/command, deterministic Author quick-panel identity/title, find-or-create reconciliation, click-time active-document dispatch, and Ribbon initialization ordering.
- Update only stale textual expectations; do not weaken semantic/idempotence checks.
- Refresh `main` before writes and stop on same-path collision.

## Validation

Read back all three corrected gates from live `main`. Local aggregate execution is `NOT_RUN` through this connector and must not be represented as PASS unless independent evidence lands.
