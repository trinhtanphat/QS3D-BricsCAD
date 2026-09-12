# QuantBIM standalone Windows viewport host

Issue: #6516  
Ownership-Key: `benchmark.quantbim.windows-viewport-host-v1`

## Purpose

`QuantBimStandaloneViewportHost` is the application-facing orchestration boundary between the existing standalone IFC/QTO workbench, the renderer-neutral scene contract merged by #6510, and a concrete Windows renderer adapter.

The host remains in `QS3D.Core` so it does not depend on BricsCAD, WPF, WinUI, Helix, xbim or a GPU API. A Windows desktop executable can implement `IQuantBimViewportRenderer` with the chosen UI/render stack while reusing the same document, filter, selection, QTO and evidence behavior.

## Workflow

`IFC source -> QuantBimStandaloneWorkbench -> QuantBimStandaloneSceneBuilder -> QuantBimStandaloneViewportHost -> IQuantBimViewportRenderer`

The viewport host provides:

- IFC open without BricsCAD through `IIfcStandaloneSource`;
- deterministic scene presentation and visible GUID ordering;
- spatial/entity/type/classification filtering via the canonical workbench filter;
- selection/highlight projection through the canonical `IfcSelectionSet` and scene builder;
- fit, focus-selection, orbit, pan and zoom command forwarding;
- standard-view intent (`Isometric`, `Top`, `Front`, `Right`) for the Windows renderer;
- property-tree, source revision/path, geometry-reference and quantity drill-through;
- selected-element quantity takeoff, BOQ aggregation and CSV export through existing workbench contracts.

## Selection and visibility rules

Selection fails closed when a requested GUID is not visible under the active filter. Changing filters deterministically removes selected GUIDs that become hidden before the scene is rebuilt, preventing invisible highlighted elements from surviving a filter transition.

Unknown inspection GUIDs fail closed. Scene construction retains the existing #6510 validation for duplicate IFC GUIDs, missing geometry references, invalid mesh topology and unknown selected elements.

## Evidence boundary

`QuantBimViewportInspection` retains the IFC document path and revision together with the selected `IfcStandaloneElement`, its canonical property tree, geometry reference and quantity records. The renderer never becomes quantity authority; it only visualizes state supplied by the existing standalone workbench and scene contracts.

## Windows packaging boundary

A Windows shell should reference `QS3D.Core`, construct `IfcStepStandaloneSource` (or a richer compatible IFC source), provide an `IIfcGeometryResolver`, and implement `IQuantBimViewportRenderer`. Renderer-native meshes, cameras, hit-testing and GPU resources stay outside Core.

BricsCAD adapters remain optional and are not referenced by this feature. Hosted CI validates deterministic host/view-model behavior with a fake renderer; it is not evidence of GPU driver fidelity, WPF/WinUI composition performance or a specific tessellation library.

## Compatibility

No existing `QuantBimStandaloneWorkbench`, `IfcStandaloneDocument`, `IfcSelectionSet`, `IfcViewCommand`, `QuantBimStandaloneSceneBuilder`, IFC STEP source, evidence or export contract is removed or changed. The host is additive.

## Acceptance coverage

`QsQuantBimViewportHostSmoke` runs through a module initializer and verifies open/present, filtering, deterministic visibility, selection/highlight, hidden-selection rejection, property/evidence inspection, selected QTO/BOQ/CSV export, navigation forwarding, standard-view forwarding and selection cleanup when filters change.
