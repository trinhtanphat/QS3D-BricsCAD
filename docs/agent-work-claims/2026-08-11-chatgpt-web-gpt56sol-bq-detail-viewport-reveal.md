# Work claim — BQ detail review and viewport reveal

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bq-detail-viewport-reveal`
- Registered: `2026-08-11T20:22:00+07:00`
- Completed: `2026-08-11T20:44:00+07:00`
- Baseline main SHA: `a551f331f640429e2f30f18ecb3d4b02c3dda76c`
- Priority: P1

## Reserved scope

- Upgrade the existing `QS3DBQ` modeless quantity summary with a BLT-style grouped/detail review switch.
- Reuse the existing Core `ProjectQuantityReportBuilder.Detail(...)` path for one-semantic-element-per-row quantity explanation; do not change quantity formulas, engineering arithmetic, intersection deductions or regeneration semantics.
- Make a user click/selection on a detail explanation row reveal the matching semantic source in the active BricsCAD 3D viewport through the existing safe Handle selection + zoom path.
- Add a concise selected-row explanation panel for concrete gross/deduction/net, formwork decomposition, length/perimeters, ElementId and CAD Handle provenance.
- Preserve current source-DWG binding, stale-row fail-closed checks, detached read-only recalculation and column preference lifecycle.

## Implemented

- `2c367e4d8d40acf4ef4a6ee932ef2aeaef26d8ea` — upgraded `QuantitySummaryWindow.xaml` with grouped/detail mode, `Bám 3D`, selected-row explanation card and clearer BLT-style review hierarchy.
- `3675084a2347f90ca90dc2588658443ab71fc878` — wired detached `ProjectQuantityReportBuilder.Detail(...)` rows, click-to-locate for detailed rows, semantic row revalidation, cross-DWG/current-project guards and detail-mode XLSX export in `QuantitySummaryWindow.xaml.cs`.
- `b0ebaa6043cc933cc4bf017ee9aa5ca50b1d4e07` — added `scripts/preflight-quantity-summary-detail-viewport.py` guarding the detail/reveal contract.
- No `Commands.cs` edit was needed: the existing `QS3DBQ` locate callback already resolves current semantic source handles, performs native `CadHandleService.Select(...)`, then queues `QS3DZOOMSELECTED`; the window now reaches that existing safe callback after its own current-row revalidation.

## Source validation

- Re-fetched current `QuantitySummaryWindow.xaml(.cs)`, `Commands.cs`, Theme resources and both BQ preflight contracts after the implementation commits.
- Existing `preflight-quantity-summary-locate-affinity.py` tokens/order remain present: displayed rows are not passed directly to `_locate`; canonical semantic identity and full live row state are revalidated first.
- New preflight locks the detached-detail path, click -> `LocateCurrent` -> `ResolveCurrentRow` -> current-row callback order, native Handle selection/zoom wiring, source-DWG lifetime guard and non-creating read-only project behavior.
- No Core quantity formula, measured-solid arithmetic, intersection deduction or formwork generation algorithm was modified.
- No GitHub Actions workflow was dispatched by this lane.

## LOCAL_ONLY disposition

- BricsCAD V25 modeless mouse interaction, native implied-selection highlight and viewport zoom remain `LOCAL_ONLY`; this remote connector session does not claim V25 runtime PASS.
- No duplicate inbox item was created because existing `LOCAL-001` is still `IN_PROGRESS` and already explicitly retains the full interactive/private-DWG/modeless BQ matrix as pending local qualification. The new detail-click behavior is part of that same modeless BQ qualification surface.

## Coordination

- Concurrent Core schedule/reporting, ribbon/Start Center/Create Similar and right-side quantity-insight lanes were preserved; this lane did not edit their reserved files.
- A concurrent right-side quantity-insight agent has separately reserved `QuantityInsightPanel*`/`PaletteCoordinator` for the screenshot-inspired docked explanation tree; this completed lane remains limited to the full `QS3DBQ` review window.
- No force push, branch rewrite or GitHub Actions dispatch was used.

## Completion evidence

- `QS3DBQ` can switch between grouped quantity and per-element detailed explanation without mutating the canonical project.
- Selecting a current detailed explanation row with `Bám 3D` enabled routes through stale/cross-DWG fail-closed revalidation and the existing native CAD Handle selection + `QS3DZOOMSELECTED` path.
- Selected-row explanation exposes concrete gross/deduction/net, formwork decomposition, geometric measures, ElementId and Handle provenance.
- Implementation/test tip for this lane: `b0ebaa6043cc933cc4bf017ee9aa5ca50b1d4e07`; subsequent concurrent main commits were preserved.
