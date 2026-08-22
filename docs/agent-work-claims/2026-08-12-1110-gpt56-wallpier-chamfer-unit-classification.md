# Work claim — WallPier chamfer unit classification

- Status: `ACTIVE`
- Agent: `gpt-5.6-sol-chatgpt`
- Registered: `2026-08-12T11:10:00+07:00`
- Baseline main SHA: `14cd3c225f4de985168b3b2cd21bf768f64e499b`
- Priority: owner-requested continue-all source-safe bug fixing

## Confirmed defect

`SemanticPropertyUnitClassifier.IsLinearMeterProperty()` is the shared boundary used by the V25 Workspace editor to decide both numeric editing and BLT-style millimeter presentation for semantic properties stored in SI meters. `WallPierChamferM` is a real WallPier geometry input consumed by the Core regenerator as meters, but the classifier's recognized linear suffixes omit `Chamfer`. The property is therefore not classified as a linear meter value, so Workspace editing/presentation can treat it as a generic text field instead of the same numeric mm UI used by other meter-backed geometry inputs.

## Evidence / non-overlap

- The classifier was introduced specifically to drive `WorkspaceViewModel.UsesMillimeterPresentation()` and numeric-property detection while preserving SI storage.
- `WallPierChamferM` is consumed as a positive finite meter value by WallPier profile regeneration and was recently classified as geometry-affecting, but no commit/claim was found for its unit-classifier suffix.
- Recent WallPier lanes cover profile geometry freshness and native/profile behavior, not Workspace unit classification.

## Reserved scope

- `src/QS3D.Core/Domain/SemanticPropertyUnitClassifier.cs`
- focused Core smoke source extending/covering chamfer classification
- this claim file

## Excluded scope

- WorkspaceViewModel implementation (consumer already delegates to the classifier)
- WallPier regeneration/profile algorithms
- `ElementGeometryPolicy.cs`
- BricsCAD runtime, packaging, signing, GitHub Actions

## Intended contract

- `WallPierChamferM` is recognized as a linear-meter semantic property.
- Existing recognized linear properties remain recognized.
- Arbitrary keys ending in `M` remain rejected unless their stem has an explicit linear suffix.
- `*Mm`, `*M2`, and `*M3` exclusions remain unchanged.

## Intended validation

Add focused auto-registered Core smoke coverage for `WallPierChamferM` plus negative controls. No Actions/build/BricsCAD runtime PASS will be claimed unless actually executed.
