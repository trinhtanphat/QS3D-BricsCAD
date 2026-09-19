# QuantBIM standalone IFC-QTO

Issue #7271 establishes the first BricsCAD-independent boundary for the QuantBIM parity program.

`QS3D.QuantBIM.Standalone` targets `net8.0` and intentionally has no BricsCAD project/package reference. `IIfcDocumentReader` isolates IFC parsing; `IStandaloneModelViewer` isolates optional 3D rendering/navigation. The QTO core therefore remains usable in headless tests, services and a future Windows desktop shell.

The canonical evidence identity is `(SourceSha256, GlobalId, Method)`. Components retain property/spatial context and quantity values retain units. Filters and BOQ generation are ordinal and deterministically ordered so exports can be compared and traced back to IFC elements.

Next slices: production IFC reader + sample fixture; Windows desktop shell with 3D viewer/property tree/spatial and type filters; persisted selection sets; classification editor and BOQ review/correction; export through `qs3d.integration.v1`; packaging and Windows compatibility validation.

BricsCAD adapters are optional integration edges only and must not be referenced by this project.