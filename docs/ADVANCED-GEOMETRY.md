# Advanced geometry and review workflows

These source paths complement Auto Room, wall-footprint generation, opening booleans, Tường KT variants and quantity/rebar workflows. Runtime-dependent behavior remains gated on licensed BricsCAD V25 validation.

## Wall junction analysis and review-gated cleanup

`QS3DWALLJUNCTIONS` reads selected compatible wall centerlines and classifies junction nodes such as L, T, X, Straight, End and Multi using deterministic Core planning.

This analysis is deliberately separate from mutation. Source-level centerline cleanup uses a two-step review gate:

1. `QS3DWALLSNAPPREVIEW` builds and stores a fingerprinted endpoint-move plan without modifying CAD.
2. `QS3DWALLSNAPAPPLY` revalidates the preview against the live selection/geometry/tolerances, then applies only a still-valid plan.

The cleanup path is restricted to tracked semantic wall LINE/open straight POLYLINE sources. Curved/bulged or unsupported geometry fails closed. Generated wall/opening/rebar/curtain geometry that becomes invalid after source mutation is invalidated with ownership-aware safeguards before later rebuild rather than being left silently stale.

This is not yet a complete L/T/X solid-join system: physical generated wall solids are not automatically unioned or reshaped into production-grade junction bodies.

## WallPier specialized profile geometry

WallPier is no longer only a generic wall category in the LINE path.

- `WallPierProfilePlanner` provides deterministic rectangular/chamfered profile quantities.
- Supported WallPier LINE sources are dispatched to the specialized native profile builder by the normal 3D workflow.
- `WallPierProfileMode` and `WallPierChamferM` control the current profile variants.
- Open POLYLINE WallPier remains on the generic guarded Tường KT footprint path.

This keeps source compatibility while making the common LINE Trụ Tường workflow closer to a dedicated BLT-style authoring object.

## Automatic Door/Opening host matching

`QS3DAUTOLINKHOSTS` automates the semantic host-link step but keeps physical cutting separate.

- Candidate walls must be compatible semantic wall categories.
- Matching uses plan/surface proximity rather than blindly choosing the nearest centerline.
- Floor/Zone scope and an independent elevation/vertical-overlap gate reduce false matches.
- Ambiguous candidates are rejected instead of guessed.
- Successful Auto Host only updates the semantic host relationship; physical subtraction remains an explicit command.

## Physical opening cuts

There are two guarded physical-cut paths:

- `QS3DCUTOPENINGS` supports compatible LINE wall hosts and straight/non-bulged open-POLYLINE segments where the opening projects safely onto one segment. `PolylineOpeningCutPlanner` rejects corner-crossing or excessive-offset cases.
- `QS3DCUTOPENINGSCURVED` handles supported bulged open-POLYLINE hosts by tessellating the centerline and using deterministic `CurvedOpeningFootprintPlanner` placement.

Both paths are fail-closed. The curved service specifically prepares **all** cutter footprints/vertical plans and the complete geometry fingerprint before any `BoolSubtract`. If the current generated host solid already carries the same fingerprint, the operation is idempotently skipped. If the same solid carries a different fingerprint, the command refuses to mutate it until the host is rebuilt.

Source replacement clears `PhysicalOpeningCutSolidHandle`, fingerprint, count and mode metadata, preventing an old straight/curved cut mode from surviving after the underlying host is replaced.

## Curtain Wall panel/grid, native frames and native panels

GlassWall keeps three intentionally separate native layers:

1. a single backing host `GeneratedSolidHandle`, used by the existing Door/Opening boolean lifecycle;
2. dedicated mullion/transom/perimeter-frame overlay solids stored under `GeneratedCurtainFrameHandles`;
3. panel-by-panel clear-glass solids stored under `GeneratedCurtainPanelHandles`.

`CurtainWallLayoutPlanner` and `CurtainWallDetailPlanner` deterministically calculate panel/grid rectangles. `QS3DCURTAINFRAMES3D` maps frames only. `QS3DCURTAIN3D` updates the backing host, frame overlays and panel solids for supported horizontal LINE and guarded open/bulged WCS-XY paths inside one outer native transaction.

Native frame controls currently include:

- `CurtainMaxPanelWidthM`
- `CurtainMaxPanelHeightM`
- `CurtainPerimeterFrameWidthM`
- `CurtainMullionWidthM`
- `CurtainTransomWidthM`
- `CurtainFrameDepthM`
- `CurtainFrameMaterial`

The adapter imposes lower native caps than the Core planners: at most 4,096 frame solids and 4,096 panel pieces per element, with independent 8,192-piece selected-batch caps. Panel base cells, opening-clipped pieces and path-mapped native fragments are bounded before destructive replacement.

Frame and panel ownership are independent from the backing host and rebar ownership. Source replacement erases a complete old generated set only after project-wide owner, canonical handle, live `Solid3d` and dedicated XData validation. Rebar/recognition/destructive guards treat both Curtain slots as generated foreign geometry.

`GeneratedCurtainFrameConfigFingerprint` is a deterministic SHA-256 snapshot of length, height, bottom offset, panel-size limits, perimeter/mullion/transom widths and frame depth. `GeneratedCurtainFrameHealthService` recomputes that fingerprint from current Family/Instance data, so panel-grid/frame-depth changes remain detectable even after semantic quantity regeneration has cleared ordinary dirty flags.

`CurtainWallOpeningPanelPlanner` clips clear panel cells against linked Door/Opening rectangles before native placement. LINE panel pieces are placed directly; open/bulged paths reuse `CurtainPathFramePlanner` station mapping and split pieces at tessellated path segments. Panel configuration/live fingerprints, independent stale state, generated ownership, Model Health and Release Readiness keep this output auditable.

Current limitations are explicit: closed/tilted/arbitrary freeform paths remain unsupported, bulged output is bounded piecewise-linear rather than an exact swept panel, physical Door/Opening subtraction still targets the backing host, and exact V25 nested-transaction/tolerance/Undo/save-reopen behavior remains LOCAL-002 `PENDING_LOCAL`.

## Shape-driven rebar 3D

`QS3DREBAR3DSHAPE` consumes deterministic BBS/shape metadata.

- Straight shape `00` can be built directly from BBS cutting length.
- L/`11`, U/`21` and Z/`31` require `RebarShapeLegsM`, so physical geometry is not guessed.
- Custom segmented shapes use `RebarShapeLegsM` plus `RebarShapeTurnsDeg`.
- Sum of shape legs must match BBS cutting length.
- `RebarCoverM` controls placement cover.
- Linear count/spacing distribution is bounded before geometry creation.
- The V25 adapter limits bars per element and per batch.
- Adjacent shape cylinders overlap slightly and are united to avoid disconnected segmented bars.
- Shape-generated handles use `GeneratedShapeRebarHandles`, separate from longitudinal-bar ownership.
- Ownership guards refuse destructive replacement when a handle is owned/protected by another semantic/generated role.

Exact rounded bend/hook radii and production fabrication shape rules remain V25/private-DWG validation/product work; source does not invent code-specific bend geometry without dimensions.

## Beam and column distribution geometry

- `QS3DBEAMREBAR3D` — supported Beam LINE longitudinal bars.
- `QS3DREBARSTIRRUP3D` — rectangular beam-stirrup loop distribution.
- `QS3DREBAR3D` — rectangular-column longitudinal bars.
- `QS3DREBARTIES3D` — rectangular column tie distribution.

Generated longitudinal, shape, tie and stirrup families use separate or explicitly shared ownership contracts and bounded batch limits. Current stirrup/tie loop geometry is segmented-cylinder rectangular geometry; fabrication hooks/bend radii are not inferred.

## Slab X/Y mesh geometry

`QS3DSLABREBAR3D` supports closed 4-vertex rectangular Slab POLYLINE footprints.

- X and Y directions use `RebarSlabXNotation` and `RebarSlabYNotation` independently.
- X/Y may use different diameters and count/spacing settings.
- Top/Bottom/Both faces are supported.
- `GeneratedSlabMeshHandles` is a dedicated ownership family, not `GeneratedRebarHandles`.
- Stored metadata includes independent X/Y diameter and actual spacing, cover, faces and mode.
- source replacement invalidates/erases slab mesh under ownership guard before metadata clear.
- `QS3DSLABREBARHEALTH` validates live handles, category, count, X/Y diameter/spacing, cover, faces and stale state.

## StructuralWall horizontal/vertical mesh geometry

`QS3DWALLREBAR3D` supports compatible near-horizontal StructuralWall LINE sources.

- horizontal and vertical notation are independent;
- Near/Far/Both faces are supported;
- horizontal/vertical diameters and actual spacing are stored separately;
- generated state uses `GeneratedWallMeshHandles` rather than the generic longitudinal slot;
- ownership/invalidation/health follow the same fail-safe pattern as slab mesh.

`QS3DWALLREBARHEALTH` checks count, live solids, category, independent diameter/spacing, cover, faces and stale state.

## Unified generated health

`QS3DREBARHEALTHALL` aggregates six generated-rebar families:

- longitudinal bars;
- BBS shape bars;
- column ties;
- beam stirrups;
- slab mesh;
- wall mesh.

It also includes cross-family ownership diagnostics. `QS3DHEALTHALL` adds core model/source/generated-solid health, generated snapshot stale checks, rebar mode semantics and curtain-frame health. Locate routes each issue back to the correct generated handle family rather than falling back to unrelated source geometry.

## Transient model review

- `QS3DHIGHLIGHT` — highlight current or prompted selection.
- `QS3DUNHIGHLIGHT` — remove QS3D transient highlight.
- `QS3DFOCUS` — highlight and zoom selected objects.
- `QS3DISOLATE` — isolate selected objects with BricsCAD object isolation.
- `QS3DUNISOLATE` — restore isolated objects.

Prompted selections are promoted to implied selection so chained BricsCAD/QS3D commands can continue on the same object set. The main Workspace exposes Focus/Isolate/restore alongside Locate/Top-view actions.

## Validation boundary

Static/source guards now include advanced geometry, wall-junction/snap, Auto Host, curved opening, dedicated slab/wall mesh, curtain native-frame/fingerprint, unified health and BLT-workspace preflights. They check command/source contracts, geometry caps, ownership safeguards, review gating, fingerprint lifecycle and key XAML wiring. They are not evidence of BricsCAD V25 compilation, NETLOAD, native boolean results or actual palette rendering.

`scripts/preflight-all.py` discovers the feature `preflight-*.py` files. GitHub Actions remain manual-only according to repository policy. Real V25 validation still requires the licensed Windows self-hosted runner/private representative drawings.
