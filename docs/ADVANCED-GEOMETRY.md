# Advanced geometry and review workflows

These source paths complement Auto Room, wall-footprint generation, opening booleans, Tường KT variants and quantity/rebar workflows. Runtime-dependent behavior remains gated on licensed BricsCAD V25 validation.

## Wall junction analysis and review-gated cleanup

`QS3DWALLJUNCTIONS` reads selected compatible wall centerlines and classifies junction nodes such as L, T, X, Straight, End and Multi using deterministic Core planning.

This analysis is deliberately separate from mutation. Source-level centerline cleanup uses a two-step review gate:

1. `QS3DWALLSNAPPREVIEW` builds and stores a fingerprinted endpoint-move plan without modifying CAD.
2. `QS3DWALLSNAPAPPLY` revalidates the preview against the live selection/geometry/tolerances, then applies only a still-valid plan.

The cleanup path is restricted to tracked semantic wall LINE/open straight POLYLINE sources. Curved/bulged or unsupported geometry fails closed. Generated wall/opening/rebar geometry that becomes invalid after source mutation is invalidated with ownership-aware safeguards before later rebuild rather than being left silently stale.

This is not yet a complete L/T/X solid-join system: physical generated wall solids are not automatically unioned or reshaped into production-grade junction bodies.

## Automatic Door/Opening host matching

`QS3DAUTOLINKHOSTS` automates the semantic host-link step but keeps physical cutting separate.

- Candidate walls must be compatible semantic wall categories.
- Matching uses plan/surface proximity rather than blindly choosing the nearest centerline.
- Floor/Zone scope and an independent elevation/vertical-overlap gate reduce false matches.
- Ambiguous candidates are rejected instead of guessed.
- Successful Auto Host only updates the semantic host relationship; the user still runs `QS3DCUTOPENINGS` explicitly for physical subtraction.

## Physical opening cuts

`QS3DCUTOPENINGS` supports compatible LINE wall hosts and guarded straight/non-bulged POLYLINE segments where the opening projects safely onto one segment. `PolylineOpeningCutPlanner` rejects corner-crossing or excessive-offset cases. Curved/bulged polyline-host cuts remain unsupported in the current source path rather than being approximated silently.

Cut fingerprints include live host/opening placement and relevant dimensions, so moving/editing geometry on an already-cut generated solid requires rebuilding the host before applying a new cut.

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
- Shape-generated handles use `GeneratedShapeRebarHandles`, separate from the rectangular-column `GeneratedRebarHandles` path.
- ownership guards refuse destructive replacement when a handle is owned/protected by another semantic/generated role.

Exact rounded bend/hook radii and production fabrication shape rules remain V25/private-DWG validation/product work; source does not invent code-specific bend geometry without dimensions.

## Transient model review

- `QS3DHIGHLIGHT` — highlight current or prompted selection.
- `QS3DUNHIGHLIGHT` — remove QS3D transient highlight.
- `QS3DFOCUS` — highlight and zoom selected objects.
- `QS3DISOLATE` — isolate selected objects with BricsCAD object isolation.
- `QS3DUNISOLATE` — restore isolated objects.

Prompted selections are promoted to implied selection so chained BricsCAD/QS3D commands can continue on the same object set. The main Workspace exposes Focus/Isolate/restore alongside Locate/Top-view actions.

## Validation boundary

Static/source guards currently include the advanced-geometry, wall-junction/snap, Auto Host and BLT-workspace preflights. They check command/source contracts, geometry caps, ownership safeguards, review gating and key XAML wiring. They are not evidence of BricsCAD V25 compilation, NETLOAD, native boolean results or actual palette rendering.

GitHub Actions remain manual-only according to repository policy. Real V25 validation still requires the licensed Windows self-hosted runner/private representative drawings.
