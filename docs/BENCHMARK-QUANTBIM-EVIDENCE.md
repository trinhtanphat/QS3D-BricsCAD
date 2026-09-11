# QuantBIM standalone traceable takeoff evidence

This note defines the P1 evidence boundary added after the benchmark foundation in PR #6474.

## Canonical architecture

`QuantBimStandaloneWorkbench` remains the canonical host-neutral IFC-QTO workbench. `IIfcStandaloneSource` is still the adapter boundary for a Windows desktop host to supply parsed IFC elements and renderer geometry without taking a dependency on BricsCAD. This change does not add a second IFC parser or renderer.

`QuantBimTraceableTakeoffEngine` sits after document open/filter/selection and before BOQ/export. It turns selected `IfcStandaloneElement` quantities into deterministic evidence rows while retaining:

- IFC document path and revision;
- element GUID, entity, storey and classification;
- exact IFC quantity name, unit and value;
- geometry reference used by the standalone viewer/adapter.

The evidence CSV is intended for QS review, audit/interchange and downstream BOQ reconciliation. Aggregation is deterministic by classification and unit and counts distinct source GUIDs.

## Fail-closed rules

Traceable takeoff rejects the hand-off when any of the following is observed:

- duplicate IFC element GUIDs in the open document;
- a named selection references a GUID that is not in the open document;
- a selected element has no geometry evidence reference;
- a quantity row carries a GUID or IFC entity inconsistent with its owning element;
- non-empty storey or classification evidence conflicts with its owning element;
- no classification is available from either the quantity row or the element.

These checks intentionally strengthen evidence integrity without changing the existing permissive `QuantBimStandaloneWorkbench.Takeoff(...)` API, preserving backward compatibility for current callers. New standalone UI/export flows that require audit-grade evidence should use `QuantBimTraceableTakeoffEngine`.

## Remaining standalone product boundary

This carrier does **not** claim full QuantBIM desktop parity. A Windows shell still needs a production IFC STEP parser/tessellator, renderer, file-open UX, persistence for named selections/workbench state, packaging and signed distribution validation. Those adapters should continue targeting `IIfcStandaloneSource` and renderer-neutral geometry/view contracts so BricsCAD-specific assemblies remain optional.
