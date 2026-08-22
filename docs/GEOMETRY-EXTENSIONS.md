# QS3D Geometry Extensions — source contracts and runtime gates

This document describes the additive geometry/rebar workflows implemented after the original V25 foundation. It is a source contract, **not proof of BricsCAD V25 runtime success**.

## Commands

### Wall topology

- `QS3DWALLJUNCTIONS` — analyze selected plan-view/coplanar LINE/open-POLYLINE centerlines as End/Straight/L/T/X/Multi and print reviewable endpoint `SnapPlan` proposals.
- `QS3DWALLSNAPPREVIEW` — create/review the current guarded endpoint-snap plan.
- `QS3DWALLSNAPAPPLY` — apply only the previously previewed plan when plan/source fingerprints still match.

### Opening / Door

- `QS3DAUTOLINKHOSTS` — conservative automatic host matching; Floor/Zone/distance/ambiguity/elevation guards; never runs physical boolean automatically.
- `QS3DCUTOPENINGS` — physical cut path for generated compatible hosts backed by LINE or safe straight open POLYLINE centerlines.
- `QS3DCUTOPENINGSCURVED` — separate physical cut path for generated compatible hosts backed by **open plan-view bulged POLYLINE** centerlines.

### Generated rebar

- `QS3DREBAR3D` — rectangular-column longitudinal vertical bars.
- `QS3DREBARTIEQTY` — deterministic rectangular-column tie count/length/weight writeback.
- `QS3DREBARTIES3D` — rectangular-column tie Solid3d generation.
- `QS3DREBAR3DSHAPE` — BBS shape-path Solid3d generation for supported STRAIGHT/L/U/S paths.
- `QS3DREBARHEALTH` — longitudinal column generated-bar health.
- `QS3DREBARSHAPEHEALTH` — BBS shape generated-bar health.
- `QS3DREBARTIEHEALTH` — generated column-tie health.
- `QS3DREBARHEALTHALL` — combined generated-rebar ownership/liveness review.

### Extension UI

- `QS3DGEOMETRYEXT` — opens `GeometryExtensionsWindow`, an additive modeless panel exposing the workflows above without replacing the main Domain Hub.

---

## Wall topology contract

Core:

- `WallJunctionPlanner`
- `WallJunctionAdjustmentPlanner`

Inputs are finite, non-degenerate wall-axis segments with unique IDs. Current Core supports:

- endpoint clustering with tolerance;
- sweep/broad-phase intersection discovery;
- spatial candidate indexing with extreme-coordinate fallback;
- Straight / L / T / X / Multi classification;
- reviewable endpoint adjustments only when an endpoint is within the configured junction tolerance;
- collapse and equally-near ambiguity rejection.

Adapter analysis uses project metadata including:

- `WallJunctionToleranceM` — default `0.005` m;
- `WallJunctionPlanarityToleranceM` — defaults to junction tolerance;
- `WallArcSagittaM` — default `0.002` m for bulged centerline tessellation.

`QS3DWALLJUNCTIONS` is analysis/reporting. The Preview/Apply workflow is the mutation path. Apply must never bypass preview/source/plan fingerprint checks.

---

## Straight and curved physical opening cuts

Semantic host deduction and native physical subtraction are separate concepts.

### Safe straight host path

`QS3DCUTOPENINGS` uses the established LINE/straight-open-POLYLINE path and `OpeningCutPlanner` / `PolylineOpeningCutPlanner`.

### Curved host path

`QS3DCUTOPENINGSCURVED` is intentionally separate. Current supported host source:

- open plan-view `Polyline`;
- at least one non-zero bulge;
- compatible semantic host category (`ArchitecturalWall`, `GlassWall`, `WallPier`, `StructuralWall`);
- live generated host `Solid3d`.

Core `CurvedOpeningFootprintPlanner`:

1. consumes a tessellated metric host centerline;
2. projects opening source-center to the nearest centerline segment;
3. rejects openings beyond `MaximumCenterlineOffsetM`;
4. rejects ambiguous non-adjacent near branches using `AmbiguityMarginM`;
5. computes center station and station ± opening width/2;
6. rejects spans beyond host ends;
7. slices the centerline while retaining intermediate curve vertices;
8. creates a cutter footprint using `WallFootprintEngine` at `HostThicknessM + 2*ClearanceM`.

Vertical placement still uses `OpeningCutPlanner`, so opening height/sill/host height validation remains shared with the straight path.

Project metadata used by the adapter:

- `WallArcSagittaM` — curved host tessellation;
- `PhysicalOpeningMaximumOffsetM` — default `0.35` m;
- `PhysicalOpeningAmbiguityM` — default `0.01` m;
- `WallMiterLimit` — cutter-footprint join limit.

### Idempotence rule

The curved service prepares all opening plans and computes the complete host/opening fingerprint **before any `BoolSubtract`**.

For the same current generated host solid:

- same fingerprint → skip; do not subtract again;
- different fingerprint → reject and require rebuilding the generated host first;
- only a new/clean generated host state proceeds to cutter subtraction.

Physical-cut metadata remains:

- `PhysicalOpeningCutSolidHandle`;
- `PhysicalOpeningCutFingerprint`;
- `PhysicalOpeningCutCount`;
- `PhysicalOpeningCutMode` (`CurvedCenterlineFootprint` for this path).

Do not expand this command to arbitrary self-near freeform centerlines without preserving the ambiguity guard.

---

## Rectangular column ties

### Semantic inputs

Tie configuration can be Family or instance data; instance values win:

- `WidthM`
- `DepthM`
- `HeightM`
- `RebarCoverM` — default `0.04`
- `RebarTieDiameterMm` — default `8`
- `RebarTieSpacingMm` — default `150`
- `RebarTieBottomClearanceM` — default `0`
- `RebarTieTopClearanceM` — default `0`
- `RebarTieHookAllowanceM` — default `0`
- `BottomOffsetM` — used by native placement.

When `QS3DREBARTIES3D` is run and the selected Column is missing explicit tie diameter/spacing, the command may seed them from the first spacing group in `RebarNotation` (for example `D8@150`). Existing explicit tie properties are never overwritten by this seeding step.

### Core layout

`ColumnTieLayoutPlanner`:

- uses bar-center path at `cover + tie radius` from the concrete faces;
- rejects impossible cover/envelope;
- treats `SpacingMm` as a **maximum** spacing;
- computes `ceil(usableHeight/requestedSpacing)` intervals so actual spacing never exceeds the requested maximum;
- produces a closed rectangular path and deterministic tie elevations;
- bounds tie count.

`ColumnTieQuantityCalculator` produces:

- count;
- cutting length per tie = path perimeter + optional hook allowance;
- total length;
- theoretical `kg/m = d² / 162`;
- total weight.

`ColumnTieProjectQuantityService` resolves instance → Family → defaults and feeds the same layout/quantity calculators.

`QS3DREBARTIEQTY` writes:

- `TieRebarCount`
- `TieRebarCutLengthM`
- `TieRebarTotalLengthM`
- `TieRebarKgPerM`
- `TieRebarWeightKg`

The writeback is snapshot/rollback guarded across a selected batch.

### Native tie geometry

`ColumnTieSolidBuilder` currently requires:

- `Column` semantic element;
- selected live source;
- closed four-vertex rectangular POLYLINE;
- XY-planar/no-bulge rectangle;
- one semantic owner per selected source.

Each tie is represented by four horizontal cylindrical segments united into one `Solid3d`. Source code currently bounds generated solids per element/batch. This is a first native path, not fabrication-complete bend-radius geometry.

Generated metadata:

- `GeneratedTieRebarHandles`
- `GeneratedTieRebarCount`
- `GeneratedTieRebarDiameterMm`
- `GeneratedTieRebarActualSpacingM`
- `GeneratedTieRebarCoverM`
- `GeneratedTieRebarMode = ColumnRectangularTies`

---

## Generated rebar ownership model

Three generated-rebar handle families currently exist:

- `GeneratedRebarHandles` — rectangular-column longitudinal bars;
- `GeneratedShapeRebarHandles` — BBS shape-path solids;
- `GeneratedTieRebarHandles` — rectangular-column tie solids.

`GeneratedRebarOwnershipGuard` indexes all three before destructive replacement. A handle may not silently migrate between elements or handle families.

Core health additionally detects cross-key ownership conflicts through `GeneratedRebarOwnershipHealthService`.

A generated handle must not be mixed into semantic `SourceHandles`.

---

## Deterministic smoke/preflight coverage

Relevant Core smoke modules include:

- `GeometryCompletionSmoke`
- `WallJunctionRegressionSmoke`
- `WallJunctionAdjustmentSmoke`
- `TopologyScaleSmoke`
- `CurvedOpeningFootprintSmoke`
- `ColumnTieLayoutSmoke`
- `ColumnTieQuantitySmoke`
- `ColumnTieProjectQuantitySmoke`
- `GeneratedTieHealthSmoke`
- `GeneratedRebarOwnershipHealthSmoke`

Relevant source guards include:

- `scripts/preflight-geometry-completion.py`
- `scripts/preflight-wall-junctions.py`
- `scripts/preflight-wall-snap-review.py`
- `scripts/preflight-curved-opening-cut.py`
- `scripts/preflight-column-ties.py`
- `scripts/preflight-geometry-extension-ui.py`

Manual-only helper workflows:

- `.github/workflows/geometry-extensions.yml`
- `.github/workflows/curved-opening.yml`

They are `workflow_dispatch` only and must not be run unless the repository owner explicitly requests it.

---

## Required BricsCAD V25 runtime validation

Before calling these extensions production-ready on an exact source SHA:

1. build adapter Release/x64 against the installed V25 managed assemblies;
2. DemandLoad/NETLOAD;
3. verify `QS3DGEOMETRYEXT` WPF modeless behavior;
4. wall-junction Preview/Apply transaction + UNDO;
5. curved wall generation then curved opening subtraction; repeat command to prove no double-subtract;
6. rebuild host, change opening position/width and verify stale-fingerprint rejection;
7. test self-near curved wall branch ambiguity;
8. column longitudinal + ties + BBS-shape generated ownership interactions;
9. run dedicated and unified rebar health commands after erasing/tampering a generated solid;
10. test large selected batches and transaction rollback;
11. private-DWG regression;
12. visual/DPI/Unicode checks.

Until that gate passes, use the phrase **source-implemented** rather than **V25 runtime-verified**.
