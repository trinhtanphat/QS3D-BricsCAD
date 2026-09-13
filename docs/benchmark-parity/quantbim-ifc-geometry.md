# QuantBIM standalone IFC geometry boundary

## Scope

`IfcStepGeometryResolver` provides a host-neutral geometry resolver for a deliberately bounded IFC4 STEP subset used by the standalone QuantBIM scene pipeline. The supported path is product → `IfcProductDefinitionShape` → one `Body` `IfcShapeRepresentation` → one `IfcExtrudedAreaSolid` with `IfcRectangleProfileDef`, explicit Cartesian origin, and a vertical extrusion direction.

The resolver produces deterministic triangle meshes for navigation, selection and scene presentation. It validates references, dimensions, direction vectors and supported placement semantics, and fails closed for ambiguous or unsupported representations rather than fabricating geometry.

## Quantity evidence boundary

QTO remains authoritative. Scene geometry is visualization/navigation evidence only and must never replace IFC quantity sets, classification data, or the detached quantity evidence used by takeoff/BOQ publication. Proxy or reconstructed mesh dimensions are therefore not promoted into measured quantities.

## Architecture boundary

The resolver lives in `QS3D.Core` and has no dependency on BricsCAD, AutoCAD, WinForms, DirectX, OpenGL, or any other host/view technology. BricsCAD-specific adapters remain optional consumers. Standalone Windows hosts may use the same resolver through the existing renderer-neutral scene/viewport contracts.

## Supported initial subset

- `ifc-step://#<product-id>` identity.
- Exactly one product representation and one `Body` shape representation.
- Exactly one `IfcExtrudedAreaSolid` item.
- `IfcRectangleProfileDef` with finite positive X/Y dimensions.
- `IfcAxis2Placement3D` with a three-coordinate Cartesian location and no rotated profile-plane axis/reference direction in this initial subset.
- Finite non-zero three-component extrusion direction normalized internally; the initial subset requires vertical extrusion.
- Finite positive extrusion depth.
- Deterministic eight-vertex/twelve-triangle closed box topology.

## Failure policy

Missing/dangling STEP references, duplicate entity ids, malformed aggregates, non-finite dimensions, zero/negative depth, multiple/ambiguous Body representations, rotated placement, unsupported non-vertical extrusion, and unsupported geometry-reference schemes are rejected with `InvalidDataException`.

## Compatibility and validation

The implementation remains compatible with the repository's `netstandard2.0` Core boundary and is covered by module-initialized deterministic smoke tests plus `scripts/preflight-quantbim-ifc-geometry.py`. Hosted CI can validate source, Core compatibility and deterministic behavior; licensed/native BricsCAD runtime evidence is not required for this host-neutral Core resolver.
