# Cubicost-style MEP takeoff and clash adapter — BricsCAD V25

Updated: 2026-08-15 (UTC+7)
Issue: #1619
Upstream Core: #1611 / PR #1615

## Scope

This first native adapter wave connects the host-neutral MEP quantity and coordination contracts from #1611 to selected real BricsCAD V25 DWG entities.

Commands:

- `QS3DMEPTAKEOFF` — read-only selected-entity MEP quantity aggregation.
- `QS3DMEPCLASH` — read-only selected-entity hard/clearance clash inspection.

Neither command creates a QS3D project, writes a sidecar, changes semantic state, edits CAD entities, or creates generated geometry.

## Native data path

The adapter deliberately reuses existing production readers rather than creating a parallel CAD interpretation layer:

1. `EntitySnapshotReader.ReadCurrentSelection(document)` resolves PICKFIRST or interactive selection and captures stable Handle, native entity type, Layer, block name where available, and real native metrics.
2. Curve length is the existing `Curve.GetDistanceAtParameter(EndParam) - GetDistanceAtParameter(StartParam)` metric. Bounding-box diagonals are never treated as quantity length.
3. `CadUnitService.GetPolicy(document)` resolves the canonical drawing-unit policy without bootstrapping a project merely for inspection.
4. MEP snapshots are mapped to `QS3D.Core.Mep.MepElement` and aggregated by `MepQuantityService`.
5. For clash inspection only, live selected handles are re-resolved with `CadHandleService` and each entity is opened `ForRead` to obtain `GeometricExtents`.
6. Extents are converted to metres and sent to `QS3D.Core.Coordination.ClashDetectionService` as `AxisAlignedBox` envelopes.

## Fail-closed classification

The first wave intentionally refuses to guess unknown CAD content. MEP classification requires explicit layer/block tokens such as:

- `DUCT`
- `PIPE` / `PIPING`
- `CABLETRAY` / `TRAY`
- `CONDUIT`
- `CABLE` / `WIRE`
- `FITTING` / `ELBOW` / `REDUCER` / `COUPLING`
- `VALVE` / `DAMPER` / `ACCESSORY`
- `EQUIP` / `AHU` / `FCU` / `PUMP` / `FAN` / `CHILLER` / `BOILER`
- `FIXTURE` / common explicit electrical/sanitary fixture tokens

For coordination, explicit structural/architectural tokens are also recognized so MEP-vs-building clashes can be inspected. Unknown layers/blocks and entities without valid extents are skipped rather than coerced into a category.

This heuristic is an adapter convention, not a claim that all third-party drawings use the same naming scheme. Future recognition-rule configuration should replace hard-coded conventions where project standards require it.

## Quantity semantics

`QS3DMEPTAKEOFF` reports per Region/System/Specification/Kind:

- recognized entity count;
- item count;
- native curve length in metres when the entity exposes a real curve metric;
- native plan/surface area converted to square metres when available;
- native Solid3d volume converted to cubic metres when available.

A missing metric remains zero. The adapter never invents length, area or volume from unrelated extents.

## Clash semantics

`QS3DMEPCLASH` asks for a non-negative clearance in current drawing units. The value is converted through the canonical drawing-unit policy.

- `0` checks hard AABB intersections only.
- positive clearance additionally reports near-miss clearance pairs.
- output is restricted to pairs where at least one side is recognized as MEP.
- this first wave is broad-phase envelope coordination, not exact Solid3d interference/boolean analysis.

Exact-solid/narrow-phase clash, native highlight/zoom, clash issue persistence and modeless review UI are follow-up lanes.

## Safety boundary

The source contract requires:

- current active document only;
- selection before any quantity/clash work;
- inspection-only unit resolution;
- `StartOpenCloseTransaction()` + `OpenMode.ForRead` for extents;
- no `OpenMode.ForWrite`;
- no `ProjectContextCoordinator.GetOrCreate` / `SetCurrent`;
- no `ExistingProjectMutationContext`;
- no `ProjectStateSnapshot`;
- no `QsdbProjectStore`;
- no CAD append/erase/transform/boolean;
- no asynchronous/native DBObject use off the document thread.

## LOCAL_ONLY qualification handoff

Source/static implementation is remote-safe, but final runtime truth requires licensed BricsCAD V25.

Local scenario:

1. Build the exact stacked/integrated SHA against installed BricsCAD V25 references with zero warnings/errors.
2. On a disposable DWG with explicit supported INSUNITS and no QS3D sidecar, select representative LINE/POLYLINE/ARC MEP paths plus BlockReference/equipment and one or more building elements.
3. Run `QS3DMEPTAKEOFF`; verify selection/cancel behavior, recognized/skipped counts, true curve lengths versus native Properties, unit conversion in at least millimetre and metre drawings, and no sidecar/project/CAD/audit mutation.
4. Run `QS3DMEPCLASH` with `0` and with a positive clearance; verify hard and near-miss pairs against native extents, including a known non-clash control.
5. Include unknown-layer, erased/stale-handle, invalid-extents/proxy and unsupported-unit cases and verify fail-closed skip/refusal rather than guessed quantities.
6. Repeat with two DWGs and confirm no cross-document selection/state leakage.
7. Save/reopen only to prove the read-only commands themselves leave the disposable drawing bytes unchanged when no unrelated user save is performed.

Evidence required: exact SHA, BricsCAD V25 build, plugin ProductVersion/hash, selected fixture descriptions without private paths/raw Handle lists, native-vs-QS3D length/unit comparisons, clash/clearance matrix, no-project/no-sidecar/no-CAD-mutation evidence, multi-DWG result, process/fixture cleanup.

Status: `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`. Source/static review must not be reported as licensed runtime PASS.
