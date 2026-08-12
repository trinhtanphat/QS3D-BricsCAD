# Work claim — Curtain property geometry freshness

- Status: `ACTIVE`
- Agent: `gpt-5.6-sol-chatgpt`
- Registered: `2026-08-12T10:14:30+07:00`
- Baseline main SHA: `80805f5178ce981f1ba5185cc5d68157c2b07f58`
- Priority: owner-requested continue-all source-safe bug fixing

## Confirmed defect

`ProjectElement.SetProperty()` delegates generated-geometry/output freshness to `ElementGeometryPolicy`. `WallRegenerator` uses GlassWall properties `CurtainMaxPanelWidthM`, `CurtainMaxPanelHeightM`, `CurtainPerimeterFrameWidthM`, `CurtainMullionWidthM`, and `CurtainTransomWidthM` to compute curtain panel/frame layout, but `ElementGeometryPolicy` does not classify those keys as geometry-affecting. Editing one therefore dirties Properties/Quantity without Geometry and without marking existing generated curtain output stale, allowing native curtain geometry to remain out of date after the semantic layout input changed.

## Non-overlap check

Recent curtain claims cover rectangle-area overflow, path/frame bounds, fingerprints, handles, health, schedule and native runner work. The active rectangle-area lane is reserved to `CurtainWallDetailPlanner.cs`; no current claim/commit was found for `ElementGeometryPolicy.cs` or curtain property freshness.

## Reserved scope

- `src/QS3D.Core/Domain/ElementGeometryPolicy.cs`
- focused Core smoke source for curtain property freshness
- this claim file

## Excluded scope

- `SemanticRegenerators.cs` and WallRegenerator implementation
- curtain layout geometry algorithms, native materialization, health, fingerprints, reporting
- WallPier property freshness (separate audit lane if needed)
- BricsCAD V25/V26 runtime, packaging, signing, private DWG, GitHub Actions

## Intended contract

- For `ElementCategory.GlassWall`, each curtain layout dimension property is geometry-affecting and generated-output-affecting.
- Editing those keys through `ProjectElement.SetProperty()` marks Geometry dirty and marks existing generated curtain frame/panel output stale.
- The same curtain-specific keys remain irrelevant to unrelated generated categories rather than globally dirtying their geometry.
- Existing generic geometry keys and output-only material keys retain current semantics.

## Intended validation

Add focused module-initializer smoke coverage for all five GlassWall layout keys plus a non-GlassWall control. No Actions/build/BricsCAD runtime PASS will be claimed unless explicitly executed.
