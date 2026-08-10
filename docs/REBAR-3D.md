# QS3D generated rebar 3D contract

This document describes the current source-level reinforcement geometry paths and, more importantly, the ownership/rebuild rules that keep generated CAD solids from deleting or impersonating one another.

## Supported source paths

### Column longitudinal bars

- Command: `QS3DREBAR3D`.
- Semantic host: `Column`.
- Rectangular closed POLYLINE footprint.
- Layout source: `RebarNotation`, `RebarBarsAlongWidth`, `RebarBarsAlongDepth`, `RebarCoverM`.
- Generated ownership key: `GeneratedRebarHandles`.
- Mode: column longitudinal bars.

### Column ties

- Command: `QS3DREBARTIES3D`.
- Health: `QS3DREBARTIEHEALTH`.
- Semantic host: `Column`.
- Generated ownership key: `GeneratedTieRebarHandles`.
- The tie set is a different generated-output family from longitudinal bars; it must never reuse `GeneratedRebarHandles`.

### Beam longitudinal bars

- Command: `QS3DBEAMREBAR3D`.
- Semantic host: `Beam` with one selected LINE source.
- Planner: `BeamLongitudinalRebarPlanner`.
- Properties include `RebarBeamTopCount`, `RebarBeamBottomCount`, `RebarBeamEndCoverM` and optional `RebarBeamDiameterMm`.
- Hard caps are applied before mutation.
- Generated ownership key: `GeneratedRebarHandles` because this is the longitudinal-bar family shared with the column longitudinal workflow.

### Beam stirrups

- Command: `QS3DREBARSTIRRUP3D`.
- Health: `QS3DREBARSTIRRUPHEALTH`.
- Semantic host: nearly horizontal Beam LINE.
- Distribution notation: `RebarStirrupNotation`, for example spacing or count notation such as `D8@150` or `20D8`.
- Section properties: `WidthM`, `HeightM`, `RebarStirrupCoverM` (fallback `RebarCoverM`) and `RebarStirrupEndCoverM`.
- Planner: `BeamStirrupLayoutPlanner` + `LinearRebarLayoutPlanner`.
- Hard caps: 1,200 stirrups per element and 4,000 per batch.
- Generated ownership key: `GeneratedBeamStirrupHandles`.
- Current native geometry is a closed rectangular loop assembled from guarded overlapping cylinder segments. Hook detail/bend-radius authoring is intentionally not claimed yet.

### BBS shape bars

- Command: `QS3DREBAR3DSHAPE`.
- Health: `QS3DREBARSHAPEHEALTH`.
- Uses deterministic BBS-shape paths and cutting-length validation.
- Generated ownership key: `GeneratedShapeRebarHandles`.
- This output family is separate from host longitudinal/tie/stirrup output sets.

## Ownership rules

Before destructive replacement, generated rebar code must reserve all of these handle domains:

- semantic `SourceHandles`;
- `GeneratedSolidHandle`;
- `PhysicalOpeningCutSolidHandle`;
- `GeneratedRebarHandles`;
- `GeneratedShapeRebarHandles`;
- `GeneratedTieRebarHandles`;
- `GeneratedBeamStirrupHandles`.

`GeneratedRebarOwnershipGuard` is the common ownership index. A builder must prove that a handle belongs to the exact element/property key before erasing a live object. A live handle that resolves to a non-`Solid3d` object is a hard failure, not permission to erase it.

The tie-specific ownership guard must protect the same cross-set domains because column-tie replacement is independently destructive.

## Host rebuild invalidation

`GeneratedDependentGeometryInvalidator` invalidates dependent generated outputs when host geometry is rebuilt. It currently covers:

- longitudinal bars;
- BBS shape bars;
- column ties;
- beam stirrups;
- generated host solid and physical opening-cut metadata.

Metadata must only be cleared after the CAD transaction that erased/replaced the dependent solids has succeeded.

## Stale generated-output lifecycle

`ProjectElement` tracks stale state separately for:

- generated host solid;
- longitudinal rebar;
- BBS shape rebar;
- column ties;
- beam stirrups.

A Geometry/Properties/Relations semantic edit snapshots the current generated handles and marks those output kinds stale. Quantity-only dirty state does not make geometry stale.

When a builder successfully replaces an output with different handle metadata, stale state for that output kind auto-resolves because the current handle set no longer equals the stale snapshot. Builders may also explicitly clear the relevant stale kind after a successful replacement. Removing all dependent outputs through the invalidator clears the aggregate stale state.

Health should therefore use the generated stale API (`IsGeneratedTieRebarStale`, `IsGeneratedBeamStirrupStale`, etc.), not `element.Dirty != None`, because quantity-only dirtiness is not evidence that the CAD solid is stale.

## Health entry points

- `QS3DREBARHEALTH` — longitudinal + shape-level health path retained for compatibility.
- `QS3DREBARTIEHEALTH` — column ties.
- `QS3DREBARSTIRRUPHEALTH` — beam stirrups.
- `QS3DREBARHEALTHALL` — combined reinforcement health.
- `QS3DHEALTHALL` — full model health, generated stale state and all current generated-rebar families in one deduplicated review window.

## Current limits

Source implementation is not a claim of licensed BricsCAD V25 runtime verification. The current geometry still requires real V25/private-DWG regression for boolean robustness, visual correctness, layer/style expectations and larger models.

Broader product work includes slab/wall two-direction mesh authoring, richer beam/column hooks and bend radii, stirrup/tie shape families, laps/anchorage visualization and editing tools that preserve BBS semantics.
