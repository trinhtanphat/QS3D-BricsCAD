# Work claim — BQ detail review and viewport reveal

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bq-detail-viewport-reveal`
- Registered: `2026-08-11T20:22:00+07:00`
- Baseline main SHA: `a551f331f640429e2f30f18ecb3d4b02c3dda76c`
- Priority: P1

## Reserved scope

- Upgrade the existing `QS3DBQ` modeless quantity summary with a BLT-style grouped/detail review switch.
- Reuse the existing Core `ProjectQuantityReportBuilder.Detail(...)` path for one-semantic-element-per-row quantity explanation; do not change quantity formulas, engineering arithmetic, intersection deductions or regeneration semantics.
- Make a user click/selection on a detail explanation row reveal the matching semantic source in the active BricsCAD 3D viewport through the existing safe Handle selection + zoom path.
- Add a concise selected-row explanation panel for concrete gross/deduction/net, formwork decomposition, length/perimeters, ElementId and CAD Handle provenance.
- Preserve current source-DWG binding, stale-row fail-closed checks, detached read-only recalculation and column preference lifecycle.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/Commands.cs` only around `QS3DBQ` callback wiring
- focused source/preflight coverage for the BQ detail/viewport contract
- `docs/LOCAL-AGENT-INBOX.md` only to extend the existing local V25 interactive qualification scenario for the new modeless click-to-3D behavior

## Excluded scope

- Core quantity formula changes, measured-solid arithmetic, deduction/intersection geometry or formwork generation algorithms.
- New element categories, persistence schema changes or project bootstrap behavior.
- Ribbon information architecture / Start Center / Create Similar lanes currently owned by other agents.
- GitHub Actions dispatch, release publication, BricsCAD binary/runtime claims.

## Validation plan

- Static/source validation that grouped rows use `ProjectQuantityReportBuilder.Group`, detail rows use `ProjectQuantityReportBuilder.Detail`, and detail selection routes through the existing locate callback.
- Preserve stale identity/fingerprint/ElementId checks before CAD selection.
- Add/update deterministic preflight coverage where practical.
- Record BricsCAD V25 modeless selection/highlight/zoom behavior as LOCAL_ONLY; do not claim runtime PASS remotely.

## Coordination

- Current active Core schedule-reporting identity work is excluded; this lane does not alter Core schedule builders or quantity identity arithmetic.
- Current ribbon/Start Center/Create Similar work is excluded; this lane does not touch RibbonBootstrapper or grouped ribbon augmenters.
- Re-fetch `main` and verify these target file blobs before every substantive write; preserve concurrent winners and never force-push.

## Completion condition

- `QS3DBQ` can switch between grouped quantity and per-element detailed explanation without mutating the project.
- Selecting a current detailed explanation row safely selects/reveals its CAD source in the active drawing and refuses stale/cross-DWG rows.
- Source/preflight checks are updated, LOCAL_ONLY runtime verification is handed off, and this claim is closed with exact implementation SHA evidence.
