# 2D Takeoff package per-sheet revisions

Autodesk-style takeoff packages can represent a package release independently from the revision identifiers carried by individual drawing sheets. QS3D keeps the existing `AutodeskTakeoffPackageCoordinator.Build` contract unchanged for callers that intentionally require every drawing revision to equal the package revision.

For mixed drawing revisions, use `AutodeskTakeoffPackagePerSheetRevisionCoordinator.Build`. The adapter validates each evidence row against its own source sheet revision and source reference, normalizes only the validation boundary to the package revision, then delegates to the existing package coordinator and `AutodeskTakeoffWorkflow`.

This preserves one canonical downstream pass:

`Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate`

The returned `TakeoffPackageBuildResult.Sources` restores each drawing's original revision so Package UX provenance remains faithful. BIM source semantics are unchanged.

## Compatibility

- Existing strict `AutodeskTakeoffPackageCoordinator.Build` behavior is unchanged.
- Existing callers do not need to migrate.
- Callers that previously split one package into several artificial package revisions solely to satisfy the strict sheet/package revision equality can instead use the per-sheet coordinator.
- Quantity grouping, formula evaluation, rate lookup, evidence counts, readiness, duplicate detection, orphan detection and source-reference checks continue to come from the canonical coordinator/workflow.

## Safety invariants

Evidence is admitted only when its revision matches the referenced drawing sheet revision. An evidence row that merely matches the package revision but not the drawing revision is still rejected as `PKG.STALE_EVIDENCE_REVISION`. Duplicate sheet identities remain blocked by the canonical coordinator, and no estimate is published for blocked packages.
