# Project metadata Wall Snap preview key scope

Lane-Key: `issue-4546`

## Purpose

Wall Snap preview persistence intentionally batches six workflow-owned metadata keys so preview publication and cleanup consume bounded project revisions. The original atomicity fix widened that exemption with a `WallJunctionSnapPreview*` prefix test, which allowed unrelated public metadata such as `WallJunctionSnapPreviewCustomerData` to mutate without advancing `ProjectState.ChangeVersion`.

This contract keeps the workflow batch atomic while restoring semantic dirty tracking for public prefix-lookalikes.

## Contract

1. Exactly these six Wall Snap preview keys are exempt from per-key semantic dirty tracking:
   - `WallJunctionSnapPreviewPlanHash`
   - `WallJunctionSnapPreviewSourceFingerprint`
   - `WallJunctionSnapPreviewCount`
   - `WallJunctionSnapPreviewUtc`
   - `WallJunctionSnapPreviewProjectId`
   - `WallJunctionSnapPreviewChangeVersion`
2. Exact-key matching remains ordinal-ignore-case, consistent with the metadata dictionary's key comparer.
3. Public metadata that merely shares the `WallJunctionSnapPreview` prefix is ordinary semantic project state and must advance `ChangeVersion` on a real set, update, add, remove, or clear mutation.
4. The six workflow-owned keys must not advance `ChangeVersion` independently. Preview publication continues to use its audit revision plus one final `Touch()`, and cleanup/apply keeps its existing bounded revision behavior.
5. The existing `QS3D.ProjectBrowser.WorkspaceState` exemption, reserved metadata codecs, persistence replacement behavior, validation, and metadata entry limits are unchanged.

## Deterministic regression

`WallJunctionSnapPreviewRevisionSmoke` retains the original six-key publication and cleanup assertions and adds `WallJunctionSnapPreviewCustomerData` coverage for set, update, remove, `Add`, and `Clear`. The lookalike operations must each advance `ChangeVersion`, while the six production-owned preview keys preserve their batch-atomic revision contract.

The auto-discovered `preflight-project-metadata-wall-snap-preview-key-scope.py` pins the exact six-key allowlist, rejects a broad Wall Snap preview-prefix exemption, and requires the lookalike smoke coverage.

## Runtime boundary

This is deterministic Core metadata/revision correctness. BricsCAD licensed runtime is `NOT_APPLICABLE`; hosted CI must not be reported as `LOCAL_PASS`.

## Landing

Require exact-head Shared CI, latest-main collision-clean reconciliation if necessary, one canonical PR with `Lane-Key: issue-4546`, fresh protected current-candidate `preflight + core`, expected-head merge, and exact protected-main verification.