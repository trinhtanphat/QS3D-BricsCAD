# QuantBIM standalone scene adapter

This P1 slice closes the renderer-boundary gap without coupling `QS3D.Core` to BricsCAD, WPF, WinUI, Helix, xbim, or a specific GPU stack.

## Contract

`QuantBimStandaloneSceneBuilder` consumes the canonical `IfcStandaloneDocument` plus an `IfcSelectionSet`. A host supplies `IIfcGeometryResolver`, which maps each existing `IfcStandaloneElement.GeometryReference` to a validated `IfcSceneMesh`.

The Core scene contract contains:

- deterministic scene-node ordering by IFC GUID;
- source `GeometryReference` retained on every node for evidence drill-through;
- triangle mesh payload as finite XYZ vertices and zero-based triangle indices;
- selection/highlight state projected from the workbench selection set;
- document revision/path identity retained on the scene;
- navigation intent reused from `QuantBimStandaloneWorkbench.DefaultNavigation`.

## Fail-closed rules

Scene construction rejects unknown selected GUIDs, duplicate IFC GUIDs, missing geometry references, null resolver results, incomplete triangles, out-of-range triangle indices, non-finite coordinates, and null mesh inputs. These conditions must not degrade silently to an apparently valid viewer scene because quantity/evidence drill-through depends on stable source identity.

## Windows host composition

A standalone Windows shell can now compose:

1. `IfcStepStandaloneSource` (or a richer IFC parser) for document/QTO/property ingestion;
2. an `IIfcGeometryResolver` backed by the chosen tessellation library;
3. `QuantBimStandaloneSceneBuilder` for deterministic scene/selection/navigation state;
4. a renderer adapter that converts `IfcSceneMesh` to native buffers/scene nodes;
5. the existing `QuantBimStandaloneWorkbench` for filters, property tree, selection sets, takeoff, BOQ and export.

BricsCAD remains optional. A BricsCAD adapter may implement the same resolver/selection boundary, but Core does not depend on the CAD SDK.

## Compatibility

No existing IFC-QTO, evidence, workbench, `IfcStandaloneElement`, `IfcViewCommand`, or STEP-ingestion contract is removed or changed. `GeometryReference` stays opaque to Core; only the host resolver interprets it.

## Acceptance

`QsQuantBimSceneSmoke` verifies deterministic node ordering, geometry resolution, selected/unselected projection, focus-selection navigation, and fail-closed handling for unknown selections, missing geometry and invalid triangle topology.
