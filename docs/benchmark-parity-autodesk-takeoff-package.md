# Autodesk Takeoff Package parity

Issue #7220 closes the managed P1 package-workflow integration gap without introducing a second measurement, revision, classification, formula, or estimate engine.

## Workflow authority map

`Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate`

- Drawing ingestion: `Qs2DSheetIngestor` admits PDF, PNG, and JPEG sheets and records source identity.
- Calibration and markup: `DrawingCalibration`, `TakeoffMarkup2D`, and `CalibratedTakeoffEngine2D` remain the authority for count, length, area, zone, layer, classification, and source-handle evidence.
- Package: `AutodeskTakeoffPackageWorkflow` validates deterministic current-sheet membership, preserves evidence, orders membership, and binds a generation token.
- Revision UX: `RevisionTakeoffPackage2D` and `DrawingRevisionOverlay2D` remain the authority for old/current revision compare and overlay semantics.
- Quantity/formula/inventory/estimate: `AutodeskTakeoffWorkflow` remains the authority; the package facade only supplies canonical current drawing evidence plus BIM quantities.

## Lifecycle rules

A package contains at most one current revision for each logical sheet id. Membership is ordered case-insensitively by sheet id and revision. A package snapshot carries an explicit generation token; callers must use `RequireCurrentGeneration` before publication or other freshness-sensitive handoff. A generation mismatch is refused rather than silently publishing stale quantities.

The package member constructor binds ingestion metadata to takeoff evidence by logical sheet id, revision, and exact source reference. This prevents evidence from one drawing source or revision from being attached to another package member.

## Compatibility and validation

The facade is additive and does not change existing public `Qs2D*` contracts. Managed smoke coverage proves zone/layer evidence retention, deterministic inventory ordering, formula/rate estimate handoff, revision comparison, stale-generation refusal, and rejection of two current revisions of the same logical sheet.

Hosted managed tests validate domain/workflow behavior only. Licensed BricsCAD-native UI/runtime behavior remains `NO_RESULT` unless exercised in the licensed runtime.
