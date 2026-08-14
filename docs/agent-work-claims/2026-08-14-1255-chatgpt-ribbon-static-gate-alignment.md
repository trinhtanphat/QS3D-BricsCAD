# Agent work claim — Ribbon static gate alignment

- Agent: `chatgpt-web-gpt56sol-ribbon-static-gate-alignment`
- Date: 2026-08-14
- Status: `COMPLETED`
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
- `src/QS3D.BricsCAD.V25/Ribbon/ReferenceWallRibbonAugmenter.cs`
- `src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs`
- current Ribbon/bootstrap/coordinator sources and relevant workflow docs

## Evidence

The current augmenter intentionally uses `AuthorPanelSourceId`, shared `CreatePanel(...)`, `EnsureButtons(...)`, and `FindById(items as IEnumerable, spec.Id) ?? FindByText(...)` while also augmenting grouped Draw/IFC panels. Three older gates still required pre-refactor names such as `PanelSourceId`, `CreateQuickPanel`, exact `FindById(items, spec.Id)` text, and the old `foreach (var spec in Buttons)` loop. PR #1146 aggregate evidence identified these UI/Ribbon lanes as the only unrelated aggregate blockers at that checkpoint.

## Boundaries

- No production Ribbon/UI source, startup lifecycle, LOCAL runner, or GitHub Actions behavior was changed.
- Stable Create Similar button ID/command, deterministic Author quick-panel identity/title, find-or-create reconciliation, click-time active-document dispatch, and Ribbon initialization ordering remain guarded.
- Only stale textual expectations were updated; semantic/idempotence checks remain explicit.

## Result

- `2b5bcfd37fce5c84190d986fb13fafaa3cfbbf8a` — align `preflight-create-similar.py` with the grouped Author quick panel, shared panel helper, text fallback and `specs` loop.
- `d077f68e157a6bec4e7dcb2f9dff6592049dda04` — align `preflight-plan-to-3d-finish-workflow.py` with `AuthorTabId` and current idempotent button reconciliation while retaining all Window/Plan-to-3D/local-runtime checks.
- `0987ee49a0773dad9aff7c44fc5cd5e832a63ba5` — align `preflight-ribbon-augmenter-panel-targets.py` with the grouped Author quick-panel/shared-helper contract while preserving Reference Wall, Project Tools and coordinator checks.

## Validation

Remote read-back confirmed all three corrected gates on live `main` and cross-checked their unchanged Reference/Project expectations against the current production augmenters. `scripts/preflight-all.py` auto-discovers the three files. Local focused/aggregate execution and GitHub Actions are `NOT_RUN`; no executable PASS is fabricated. A later independent run is still required to prove the aggregate result on an exact SHA.
