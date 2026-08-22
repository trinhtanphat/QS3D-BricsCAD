# Level references

QS3D reuses the existing `ProjectFloor` catalog as its semantic Level model. It does not introduce a second Level entity or migrate legacy element placement implicitly.

## Semantic keys

Element-level opt-in references are:

- `BottomLevelId`
- `BottomLevelOffsetM`
- `TopLevelId`
- `TopLevelOffsetM`

`ProjectFloorService` owns assignment, clearing, reference counting, update invalidation and delete guards for these keys.

## Compatibility contract

`ElementVerticalPlacementService` defines the intended shared placement contract:

1. No Level references: preserve legacy placement exactly as `source base elevation + BottomOffsetM`, with legacy `HeightM`.
2. `BottomLevelId` only: absolute bottom is `bottom Level elevation + BottomLevelOffsetM`; legacy height remains in effect.
3. `BottomLevelId` + `TopLevelId`: bottom and top are resolved from Level elevations plus their explicit offsets; effective height is `top - bottom`.
4. `TopLevelId` without `BottomLevelId`, missing Level IDs, non-finite offsets, or `top <= bottom` are invalid and fail closed.
5. Legacy `FloorId` is not silently promoted to `BottomLevelId`.
6. Legacy `BottomOffsetM` is not added again after `BottomLevelId` is present.

## Current integration boundary

Core now carries the complete semantic placement contract, treats all four Level keys as geometry-driving, propagates Floor elevation changes through transitive dependents, and has prepared effective-span quantity paths for Wall/Opening plus Beam/Slab/Column/StructuralWall/Foundation. The first native host wave also routes ArchitecturalWall/GlassWall/WallPier and those structural host builders through the same CAD-unit adapter.

This preparation is deliberately dormant for Level-configured elements. `LevelReferenceNativeIntegrationPolicy.EnsureQualified(...)` blocks both native host mutation and production quantity regeneration while the policy qualifies no category. Legacy elements without Level metadata continue through the existing source-relative path. This fail-closed boundary prevents imported or hand-edited Level metadata from moving only a host or producing an ED2/BQ quantity that its opening, Curtain or rebar dependents cannot yet match.

The Floor/Level UI intentionally does **not** expose Bottom/Top Level assignment yet. Physical opening cutters, curtain frames/panels and generated reinforcement still need to consume the same placement resolver before any category can be enabled.

The native integration batch must therefore update the vertical-placement chain coherently, including host solids, semantic quantity regeneration and every dependent generated system that derives Z/effective height. Only after that integration is source-reviewed and qualified on BricsCAD V25 should Bottom/Top Level assignment be exposed in the Level Manager.

## Native integration qualification policy

`LevelReferenceNativeIntegrationPolicy` is the explicit source gate for categories whose complete native/dependent vertical-placement chain has been integrated and qualified. The current policy intentionally qualifies **no category**.

Do not add a category to that policy merely because one host builder has been changed. Qualification for a category requires, at minimum:

- its native host builder uses the shared Level placement contract;
- semantic quantities use the same effective bottom/top/height contract;
- hosted Door/Opening geometry remains vertically consistent;
- dependent Curtain/rebar/mesh/detail outputs that apply to the category use the same placement or are safely invalidated/rebuilt;
- source reconcile, rebuild, save/reopen and Floor elevation changes preserve the resolved placement;
- legacy elements with no Level references remain byte/behavior compatible at the semantic contract level;
- exact-SHA BricsCAD V25 runtime scenarios pass before the category is called production-qualified.

Until those conditions are met, keep the policy fail-closed.

## Release behavior

`LevelReferenceHealthService` is included in `QS3DHEALTH`, `QS3DHEALTHALL` and `QS3DRELEASECHECK`.

- malformed Level metadata produces its specific release-blocking semantic error;
- a semantically valid opt-in Level reference on an unqualified category produces `LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING` as a release-blocking Error;
- invalid references do not also receive the pending-integration error, so health output preserves the real root cause;
- elements with no Level reference continue on the legacy placement path and are not blocked by this gate.

This prevents hand-edited or future UI-created Level metadata from making a candidate look release-ready while native Solid3d/QTO/dependent geometry still follows legacy source-relative Z/height assumptions.
