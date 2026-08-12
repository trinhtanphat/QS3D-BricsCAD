# Work claim — WallPier property geometry freshness

- Status: `ACTIVE`
- Agent: `gpt-5.6-sol-chatgpt`
- Registered: `2026-08-12T10:40:00+07:00`
- Baseline main SHA: `bc9ac1c59a8909a3a32bc643a0fb7e3fb10cbcf7`
- Priority: owner-requested continue-all source-safe bug fixing

## Confirmed defect

`WallRegenerator` resolves `WallPierProfileMode` and `WallPierChamferM` from instance/Family semantic properties and uses them to select/parameterize the generated WallPier profile. `ProjectFamilyService.SetProperty()` propagates inherited Family changes through `ProjectElement.SetProperty()`, whose generated-geometry freshness is delegated to `ElementGeometryPolicy`. The policy currently classifies generic dimensions plus GlassWall curtain layout keys, but not these WallPier profile keys. Editing either key can therefore update the semantic model without dirtying WallPier Geometry or marking an existing generated solid stale, allowing the native WallPier profile to remain out of date.

## Non-overlap check

Recent WallPier commits cover authoring, path-profile atomicity, numerical stability and native builder behavior. The recently completed curtain property freshness lane changes the same policy for GlassWall only. No current claim/commit was found for WallPier profile property freshness or `WallPierProfileMode` / `WallPierChamferM` classification.

## Reserved scope

- `src/QS3D.Core/Domain/ElementGeometryPolicy.cs`
- focused Core smoke source for WallPier property freshness
- this claim file

## Excluded scope

- `SemanticRegenerators.cs` implementation and WallPier profile algorithms
- native V25/V26 builders, reporting, health, persistence, UI
- GlassWall curtain freshness (already completed)
- GitHub Actions, packaging, signing, private DWG/runtime qualification

## Intended contract

- For `ElementCategory.WallPier`, `WallPierProfileMode` and `WallPierChamferM` are geometry/output-affecting.
- Editing either through `ProjectElement.SetProperty()` dirties Geometry and marks existing generated solid output stale.
- The same WallPier-specific keys do not dirty unrelated generated categories.
- Existing generic geometry, curtain-only and output-only material semantics remain unchanged.
- Family property propagation inherits the same freshness behavior through the existing `ProjectFamilyService.SetProperty()` path.

## Intended validation

Add focused auto-registered Core smoke coverage for instance edits, Family propagation and unrelated-category controls. No Actions/build/BricsCAD runtime PASS will be claimed unless explicitly executed.
