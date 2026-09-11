# Cubicost + QuantBIM benchmark lane

This lane is additive to the existing benchmark foundation. It deliberately keeps quantity/review/workbench contracts in `QS3D.Core` so BricsCAD adapters are optional rather than architectural dependencies.

## Cubicost-style Concrete / Formwork workflow

`CubicostConcreteFormworkWorkflow` consumes `RecognizedQsComponent` records from drawing recognition, IFC model adapters, or manual reconstruction. Every component carries immutable `QuantityEvidence` with source identity/reference, revision, recognition method, and confidence.

The review workflow is explicit:

- `Proposed`: recognized but not yet reviewed;
- `Accepted`: QS reviewer confirmed the reconstruction;
- `Corrected`: reviewer supplied authoritative dimensions;
- `Rejected`: false-positive recognition and excluded from quantities.

The quantity workflow applies review corrections before calculation and produces separate `.CONCRETE` (`m3`) and `.FORMWORK` (`m2`) inventory lines. Existing `ConcreteFormworkCalculator` remains available and is reused rather than replaced.

### Adapter expectations

Drawing recognition adapters should map calibrated source geometry into `RecognizedQsComponent` and preserve the drawing markup/source handle in `QuantityEvidence.SourceReference`. IFC adapters should use IFC GUID/revision evidence. Recognition engines must not silently overwrite reviewer corrections.

This core contract is suitable for connection to existing rebar, MEP, estimate, tender, procurement, workbook, and REST layers through the common inventory/classification types.

## QuantBIM-style standalone IFC QTO workbench

`QuantBimStandaloneWorkbench` is BricsCAD-independent. Actual IFC parsing/geometry tessellation is injected through `IIfcStandaloneSource`, allowing a Windows desktop host to use its chosen IFC parser/rendering stack without coupling Core to CAD SDKs.

The workbench core now supports:

- opening an IFC document through an injectable standalone source;
- IFC entity/storey/type/classification filtering;
- persistent named selection-set semantics through `IfcSelectionSet`;
- property-tree projection for identity, spatial, type, classification, geometry reference, and IFC property values;
- selection-scoped quantity takeoff;
- classification/unit BOQ aggregation;
- CSV interchange/export;
- viewer-navigation commands for fit, focus-selection, orbit, pan, and zoom.

`IfcStandaloneElement.GeometryReference` is intentionally renderer-neutral. A Windows viewer host can resolve it into its native mesh/scene handle. `IfcViewCommand` similarly provides navigation intent without taking a dependency on WPF, WinUI, Helix, xbim, BricsCAD, or another renderer.

## Windows host boundary

A standalone Windows product should compose four adapters around the Core workbench:

1. `IIfcStandaloneSource`: parse IFC, property sets, spatial/type relationships, quantities, and geometry references;
2. scene adapter: map `GeometryReference` to renderable meshes and execute `IfcViewCommand` navigation;
3. desktop UX: model tree, property pane, filter controls, selection sets, BOQ/takeoff grid and evidence drill-down;
4. interchange adapter: persist/export BOQ and pass inventory into QS3D estimate/tender/procurement or Integration API contracts.

None of these adapters require BricsCAD to be installed. BricsCAD integration can remain a separate optional source/selection adapter.

## Compatibility

No existing `ConcreteFormworkCalculator`, `IfcQtoItem`, or `IfcQtoWorkbench` API is removed. The new workflow builds on them. Existing callers therefore require no migration.

## Smoke acceptance

`QsCubicostQuantBimSmoke` verifies:

- accepted/corrected/rejected reconstruction behavior;
- reviewer corrections applied before concrete/formwork calculation;
- drawing/model quantity evidence preserved into takeoff output;
- dedicated concrete/formwork inventory lines;
- standalone IFC open/filter/property tree/selection/takeoff/BOQ/export workflow;
- renderer-neutral viewer focus command for selected IFC elements.
