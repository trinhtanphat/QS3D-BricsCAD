# Agent work claim — detailed quantity explainer

- Agent: `chatgpt-web-gpt56sol-quantity-detail-explainer-20260813-2249`
- Started: `2026-08-13T22:49:00+07:00`
- Status: `ACTIVE`
- Task ID / title: `Detailed per-component quantity explanation`
- Source / user driver: user supplied BLT3D reference screenshots and requested a detailed plan plus full implementation so QS3D can inspect quantity details per modeled component, then commit/push to `main`.
- Baseline main SHA: `484cd6248a167a6ff67a9ebace7e2504b5f8ecf1`

## Objective

Upgrade the existing QS3D Quantity Insight palette from project/group totals into a drill-down, read-only quantity explainer. Selecting a leaf quantity row must expose canonical per-element quantity fields and provenance (element identity, CAD handles and drawing fingerprint), with an element selector for grouped rows and a locate action that reuses the existing safe CAD-handle selection path.

## Implementation plan

1. Preserve the existing detached-snapshot regeneration path and `ProjectQuantityReportBuilder` as the single source of truth; do not add competing quantity formulas in the UI.
2. Add a responsive `CHI TIẾT CẤU KIỆN` area beneath the existing Floor/Family tree, including an empty-selection hint, per-element selector, metrics list, provenance/metadata, and locate-selected-component action.
3. Use `ProjectQuantityReportBuilder.Detail(previewProject, selectedRow.ElementIds)` to drill grouped report rows down to canonical element rows.
4. Surface concrete gross/deduction/net, formwork, length, perimeter and face-area components, density and mass with explicit units.
5. Keep selection/locate read-only and fail-closed: stale report keys or unresolvable source handles must not mutate project data or silently select unrelated CAD entities.
6. Add a focused source regression guard for the detail contract so future UI refactors cannot regress to aggregate-only Quantity Insight.
7. Verify the touched source on the final `main`; run the focused static guard where GitHub CI makes that executable, without claiming native BricsCAD runtime proof remotely.

## Expected path surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/ViewModels/QuantityInsightViewModel.cs`
- focused regression under `scripts/`
- this claim file for lifecycle close-out only

## Explicit exclusions

- Core QTO formulas and measurement-generation semantics
- persistence/schema migrations
- Build3D/PlanTo3D geometry generation
- updater/licensing/startup lanes
- V26 native-runtime qualification
- unrelated workspace, ribbon, rebar, family-editor or health lanes
- every other agent's claim file

## Dependencies / risks / merge constraints

- Repository `main` is highly concurrent; all writes must refresh and fast-forward without force-push.
- Existing Quantity Insight dark theme, stale-selection guards, detached preview regeneration, and textual-source/locate contracts must be preserved.
- This remote lane can verify source/static behavior but cannot truthfully claim BricsCAD V25 native UI/runtime acceptance without a V25 host run.

## Exact completion condition

The detailed component explainer, focused regression guard, and lifecycle close-out are present on current `main`; canonical detail rows drive all displayed quantity values; locate behavior remains guarded/read-only; final implementation/test SHAs are recorded here; any remaining native V25 acceptance requirement is stated explicitly rather than marked passed.