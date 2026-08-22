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

The current source candidate carries the complete semantic placement contract and routes CAD consumers through one branch-lazy adapter, `CadElementVerticalPlacement`. The adapter deliberately avoids reading legacy source Z, height or `BottomOffsetM` on Level-derived branches, while the no-Level branch preserves the established source-relative calculation exactly.

The source-integrated chain now includes:

- ArchitecturalWall, GlassWall, WallPier, StructuralWall, Beam, Slab, Column, Foundation, Stair and Railing native placement;
- Door/WallOpening host-relative containment, straight/curved physical cutting, live fingerprints and Auto Host matching;
- Curtain LINE/path frames, panels, live-state fingerprints and generated vertical snapshots;
- Beam/Column longitudinal and tie/stirrup output plus Slab/Foundation/StructuralWall mesh and Shape rebar placement;
- effective-span semantic quantities and Level-edit stale propagation through hosts and generated dependents;
- guarded Floor/Level modeless actions for assigning Bottom Level, assigning Top Level and clearing vertical Level references.

`LevelReferenceNativeIntegrationPolicy` therefore source-enables the categories above plus Door and WallOpening. Unsupported categories, including Earthwork, remain fail-closed when Level metadata is configured. This is a **source enablement boundary**, not evidence that the customer-release runtime matrix has passed.

`QS3DLEVELZPROBE` and `scripts/test-bricscad-v25-level-z.ps1` provide a focused exact-SHA automated V25 probe for representative legacy, Bottom-only, Bottom+Top, Top-only refusal, opening, Curtain, Beam rebar/stirrup, quantity, snapshot and Level-edit invalidation paths. The wider mm/m, full-category, Undo, save/reopen, multi-DWG and representative private-DWG matrix remains `LOCAL-003 / PENDING_LOCAL` until it is executed against the same exact SHA and DLL.

## Source enablement and runtime qualification policy

`LevelReferenceNativeIntegrationPolicy` is the explicit source gate for categories whose complete native/dependent vertical-placement chain is present. In this class, `IsQualified(...)` means **qualified to leave the source fail-closed gate**; it does not mean `LOCAL_PASS`, customer-release qualification or completion of the full native matrix.

Do not add a category to that policy merely because one host builder has been changed. Qualification for a category requires, at minimum:

- its native host builder uses the shared Level placement contract;
- semantic quantities use the same effective bottom/top/height contract;
- hosted Door/Opening geometry remains vertically consistent;
- dependent Curtain/rebar/mesh/detail outputs that apply to the category use the same placement or are safely invalidated/rebuilt;
- source reconcile, rebuild, save/reopen and Floor elevation changes preserve the resolved placement;
- legacy elements with no Level references remain byte/behavior compatible at the semantic contract level;
- the focused exact-SHA BricsCAD V25 probe passes before the integrated candidate is pushed as runtime-proven;
- the complete interactive matrix passes before a category is called production-qualified or customer-release ready.

If a future category lacks any applicable dependent link, keep it fail-closed until that entire source chain and its focused local scenario are added together.

## Release behavior

`LevelReferenceHealthService` is included in `QS3DHEALTH`, `QS3DHEALTHALL` and `QS3DRELEASECHECK`.

- malformed Level metadata produces its specific release-blocking semantic error;
- a semantically valid opt-in Level reference on a source-unqualified category produces `LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING` as a release-blocking Error;
- invalid references do not also receive the pending-integration error, so health output preserves the real root cause;
- elements with no Level reference continue on the legacy placement path and are not blocked by this gate.

This prevents hand-edited or UI-created Level metadata on unsupported categories from making a candidate look release-ready while native Solid3d/QTO/dependent geometry still follows legacy source-relative Z/height assumptions. For source-enabled categories, the separate `LOCAL-003` exact-SHA evidence remains mandatory before release qualification.
