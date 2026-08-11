# Work claim — Quantity Insight detached preview regeneration parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-preview-regeneration`
- Registered: `2026-08-11T21:03:30+07:00`
- Baseline main SHA: `56f4eac65f2730fc85e59e339701f0df9775c530`
- Priority: P1

## Reserved scope

- Make the docked `QuantityInsightPanel` compute its displayed totals/tree from a detached regenerated project snapshot, matching the already-established read-only `QS3DBQ` preview behavior.
- Ensure stale-row revalidation uses the same detached regenerated read path, so a dirty live project does not make every legitimate locate look stale merely because derived quantity state was preview-regenerated for display.
- Preserve the completed document/project affinity guards, selection highlighting, Handle-based native selection and `QS3DZOOMSELECTED` behavior.
- Update the existing affinity preflight only as needed so it guards the same stale-row/document contract after live grouped-row construction is routed through the new detached preview helper.

## Expected files

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs`
- `scripts/preflight-quantity-insight-preview-regeneration.py`
- `scripts/preflight-quantity-insight-affinity.py` (compatibility update for the new helper path; no weakening of affinity checks)
- this claim file for close-out

## Excluded scope

- Core quantity formulas, quantity settings schema/rules, `QuantitySettingsWindow*`, Wall Takeoff UI, Ribbon/Start Center/Workspace presentation, persistence mutation, updater/release work.
- No native BricsCAD V25 runtime PASS claim from the remote connector environment.

## Functional contract

- `RefreshQuantityInsights()` must obtain the existing canonical project read-only, create a detached `ProjectStateSnapshot` copy, regenerate dirty semantics on that detached copy, and build grouped rows/totals from the detached copy only.
- `ResolveCurrentRow(...)` must rebuild current rows through the same detached regenerated pipeline before comparing semantic identity/value/provenance.
- The live canonical project must not be mutated merely by opening, refreshing, highlighting or locating from the Quantity Insight palette.
- Cross-DWG/project and stale-row fail-closed checks from the completed affinity lane must remain intact.
- Existing affinity regression coverage must continue to require DWG -> project -> live-row -> Handle -> native selection ordering, even though the live row is now produced by `BuildPreviewRows(...)` rather than a direct `ProjectQuantityReportBuilder.Group(project)` call.

## Validation plan

- Re-fetch current `main` immediately before source writes and preserve concurrent winners.
- Add an auto-discovered static preflight that requires detached-copy -> regenerate -> grouped-report ordering for both refresh and locate revalidation, while forbidding direct regeneration of the live project and any creating/mutating project bind.
- Keep the prior affinity preflight strict by replacing only its direct-group construction token with the detached-preview helper token/order.
- Re-fetch the implementation after commit and verify ancestry/status without dispatching GitHub Actions.

## Completion condition

- Dirty project state can be previewed accurately in Quantity Insight without mutating the live project, and locate revalidation compares against the same regenerated read model used for display.
- Both focused preflights are consistent with the new read path, and this claim is marked `COMPLETED` with exact implementation/test SHAs.
