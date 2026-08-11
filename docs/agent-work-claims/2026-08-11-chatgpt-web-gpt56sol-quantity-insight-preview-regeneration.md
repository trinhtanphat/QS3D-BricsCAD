# Work claim — Quantity Insight detached preview regeneration parity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-preview-regeneration`
- Registered: `2026-08-11T21:03:30+07:00`
- Completed: `2026-08-11T21:09:00+07:00`
- Baseline main SHA: `56f4eac65f2730fc85e59e339701f0df9775c530`
- Priority: P1

## Reserved scope

- Make the docked `QuantityInsightPanel` compute its displayed totals/tree from a detached regenerated project snapshot, matching the already-established read-only `QS3DBQ` preview behavior.
- Ensure stale-row revalidation uses the same detached regenerated read path, so a dirty live project does not make every legitimate locate look stale merely because derived quantity state was preview-regenerated for display.
- Preserve the completed document/project affinity guards, selection highlighting, Handle-based native selection and `QS3DZOOMSELECTED` behavior.
- Update the existing affinity preflight only as needed so it guards the same stale-row/document contract after live grouped-row construction is routed through the new detached preview helper.

## Implemented

- `1c5513b403cef7fd1463960f4714018f2ac2e666` — `QuantityInsightPanel` now builds its tree/totals through `BuildPreviewRows(...)`: detached `ProjectStateSnapshot` copy -> semantic `RegenerationEngine.RegenerateDirty(previewProject)` -> grouped quantity rows from the detached preview only. The UI reports how many preview regeneration passes were applied without committing them to the live project.
- The same helper is now used by `ResolveCurrentRow(...)`, so locate compares the displayed row against the same regenerated read model before resolving current Handles.
- Existing document/ProjectId/drawing-fingerprint affinity, full row/provenance equality, CAD selection and `QS3DZOOMSELECTED` behavior remain intact.
- `cc38e41349bcb113367670feafbd17238220586c` updated the existing affinity guard to follow `BuildPreviewRows(...)` rather than requiring a direct live `Group(project)` call.
- `0d0874f6bb7fefde6464389b041097e9c34b096e` added `scripts/preflight-quantity-insight-preview-regeneration.py` to guard detached-copy -> regenerate -> group ordering for both refresh and locate revalidation and to forbid live-project create/mutation/regeneration/grouping.
- A concurrent follow-up preserved and strengthened the affinity preflight further on current `main`, explicitly requiring detached preview construction before stale-row matching.

## Source validation

- Re-fetched current `main` after implementation and concurrent integration. `QuantityInsightPanel.xaml.cs` still contains `ProjectStateSnapshot.CreateDetachedCopy(project)`, `RegenerateDirty(previewProject)`, `ProjectQuantityReportBuilder.Group(previewProject)`, refresh through `BuildPreviewRows(project, out var regenerated)`, and locate revalidation through `BuildPreviewRows(project, out _)`.
- Re-fetched both focused preflights on current `main`; both require the detached preview path and retain the cross-DWG/project/current-row fail-closed contract.
- No `ProjectContextCoordinator.GetOrCreate`, `ExistingProjectMutationContext.Require`, direct `RegenerateDirty(project)`, or direct `ProjectQuantityReportBuilder.Group(project)` path is present in the Quantity Insight read workflow.
- The implementation/test commits are ancestors of current `main`; concurrent commits were preserved and no force push was used.
- GitHub exposes no combined status checks for `0d0874f6bb7fefde6464389b041097e9c34b096e`; no GitHub Actions were dispatched in this lane.

## LOCAL_ONLY disposition

- Native BricsCAD V25 palette mouse interaction, implied-selection behavior and viewport zoom remain covered by the existing local interactive qualification queue. This source change adds no distinct new local-only scenario, so no duplicate inbox item was created.
- No remote native runtime PASS is claimed.

## Completion evidence

- Quantity Insight can now preview dirty semantic quantity state accurately without mutating the canonical live project.
- Stale-row locate revalidation uses the same detached regenerated read model as display, preventing false stale mismatches caused only by preview regeneration while retaining fail-closed behavior for real data/provenance changes.
- Implementation: `1c5513b403cef7fd1463960f4714018f2ac2e666`; focused preview guard: `0d0874f6bb7fefde6464389b041097e9c34b096e`.
