# Work claim — Curtain property geometry freshness

- Status: `COMPLETED`
- Agent: `gpt-5.6-sol-chatgpt`
- Registered: `2026-08-12T10:14:30+07:00`
- Completed: `2026-08-12T10:21:30+07:00`
- Baseline main SHA: `80805f5178ce981f1ba5185cc5d68157c2b07f58`
- Claim commit: `c88d5898d382d64d659aac74f1372a7817513249`
- Implementation PR: `#746`
- Main merge commit: `21ca2d08427013f3ef8154708fef85fb2454ff8f`
- Priority: owner-requested continue-all source-safe bug fixing

## Confirmed defect

`ProjectElement.SetProperty()` delegates generated-geometry/output freshness to `ElementGeometryPolicy`. `WallRegenerator` uses GlassWall properties `CurtainMaxPanelWidthM`, `CurtainMaxPanelHeightM`, `CurtainPerimeterFrameWidthM`, `CurtainMullionWidthM`, and `CurtainTransomWidthM` to compute curtain panel/frame layout, but `ElementGeometryPolicy` did not classify those keys as geometry-affecting. Editing one therefore dirtied Properties/Quantity without Geometry and without marking existing generated curtain output stale, allowing native curtain geometry to remain out of date after the semantic layout input changed.

## Implemented contract

- Added category-specific GlassWall curtain geometry keys for all five layout dimensions.
- `AffectsGeneratedGeometry()` and `AffectsGeneratedOutput()` now share the category-aware geometry classification.
- Editing those keys through `ProjectElement.SetProperty()` dirties Geometry and marks existing generated curtain frame/panel output stale.
- The same curtain-only keys do not dirty unrelated generated categories.
- `CurtainFrameMaterial` remains generated-output-only and does not dirty Geometry.

## Regression source

`tests/QS3D.Core.SmokeTests/CurtainPropertyGeometryFreshnessSmoke.cs` is a module-initializer smoke covering all five GlassWall layout keys, an unrelated Beam control, and the existing output-only material behavior. The source and smoke were read back directly from `main` after merge.

## Validation limits

No GitHub Actions were dispatched. No local .NET build or BricsCAD V25/V26 runtime qualification was executed or claimed in this lane.
