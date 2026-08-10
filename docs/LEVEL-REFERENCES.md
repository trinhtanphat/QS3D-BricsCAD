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

This batch establishes Core semantics, lifecycle guards, Health All / Release Check diagnostics, smoke coverage and static preflight only.

The Floor/Level UI intentionally does **not** expose Bottom/Top Level assignment yet. Native CAD builders currently share source-relative placement assumptions with physical openings, curtain frames and rebar. Exposing Level assignment before those paths consume the same placement resolver would create a misleading UI where semantic Level state and CAD geometry disagree.

The native integration batch must therefore update the vertical-placement chain coherently, including host solids and every dependent generated system that derives Z/effective height. Only after that integration is source-reviewed should Bottom/Top Level assignment be exposed in the Level Manager.

## Release behavior

`LevelReferenceHealthService` is included in `QS3DHEALTHALL` and `QS3DRELEASECHECK`. Invalid Level references are release-blocking errors even before native placement is enabled, so malformed or hand-edited metadata cannot silently enter a release.
