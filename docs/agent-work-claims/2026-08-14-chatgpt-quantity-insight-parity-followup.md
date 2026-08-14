# Work claim — Quantity Insight BLT parity follow-up

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-quantity-insight-followup-20260814`
- Registered: `2026-08-14T16:13:00+07:00`
- Completed: `2026-08-14T17:00:00+07:00`
- Baseline main SHA: `5124e588275ac33f01c722218c9466ce76f03d12`
- Context: follow-up to the completed Quantity Insight BLT parity/click-to-3D lane after user requested `continue all`.

## Claimed scope

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Geometry.cs` for stale/dirty detail consistency when locating exact BREP deduction rows.
- One focused source regression/preflight guard for Quantity Insight BLT-parity wiring if an existing guard does not already cover it.
- This claim document for closeout evidence.

## Explicit exclusions

- No changes to Core quantity arithmetic, intersection/deduction rules, quantity mapping/category policy, or BREP computation contracts.
- No changes to Quantity Summary, Wall Quantity, Project Browser/Grid work, releases/signing, or GitHub Actions policy.
- No native-runtime PASS claim without a licensed BricsCAD V25 host.

## Proven gap and resolution

The displayed component detail is generated from a detached preview that regenerates dirty semantic state, while the BREP exact explainer can also derive from live Solid3d. The deduction-click path previously re-queried `ProjectQuantityReportBuilder.Detail` directly against the potentially dirty live semantic project before resolving CAD handles. This could make the visible exact deduction row fresh while its click-to-3D validation was stale.

The locator now creates the same detached semantic preview used by the detail workflow, regenerates dirty state, resolves a unique canonical detail row, and rejects row/provenance drift with the existing `SameRow` contract. Only after that validation succeeds does it resolve handles from the live project and select/zoom the live BricsCAD objects.

## Implementation evidence

- `9e35e9c6e58fef8231f1c972388fb893225f7680` — `fix(quantity): validate deduction locate against regenerated preview`
  - switched deduction validation from live dirty `Detail(project, ...)` to detached preview + `RegenerateDirty(preview)` + `Detail(preview, ids)`;
  - requires unique canonical identity and `SameRow` provenance equality;
  - keeps `SourceHandleResolver.Resolve(project, semanticIds)` on the live project and direct `ViewportCommands.TryZoomSelection(document)`.
- `431f2454eadc2ffcaafabb6bfb764081e7f6db80` — `ci: guard regenerated deduction locate semantics`
  - extends `scripts/preflight-quantity-geometry-explainer.py` with required detached-preview/regeneration/provenance/live-handle markers;
  - explicitly rejects regression back to `ProjectQuantityReportBuilder.Detail(project, option.Row.ElementIds)`.

## Validation

- Source/commit read-back: PASS — implementation and guard are present on `main` at `431f2454eadc2ffcaafabb6bfb764081e7f6db80` before this closeout write.
- The focused preflight contract is source-backed; GitHub Actions were not dispatched because `CI_POLICY.md` keeps workflows manual-only and this request did not separately authorize a CI run.
- Native BricsCAD V25 click-to-3D acceptance remains `LOCAL_ONLY`: load the DLL on a licensed V25 host, open a project with intersections, display an exact `Trừ giao` row, click it, and confirm the current target + cause are selected and synchronously zoomed. A stale row after semantic changes must refuse locate and request `Làm mới`.

## Result

`COMPLETED` for the remote-safe source lane. No Core quantity arithmetic or BREP calculation policy was changed.