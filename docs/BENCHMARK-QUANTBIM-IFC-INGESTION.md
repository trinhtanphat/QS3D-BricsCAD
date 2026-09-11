# QuantBIM standalone IFC STEP ingestion

Issue #6500 adds a host-neutral IFC STEP source to the canonical `QuantBimStandaloneWorkbench` introduced by the benchmark foundation. It does not create a second workbench and it does not add a BricsCAD dependency to `QS3D.Core`.

## Supported QS ingestion slice

`IfcStepStandaloneSource` implements `IIfcStandaloneSource`, so a Windows standalone host can call the existing workbench `Open(path)` API against an IFC file directly. The parser deterministically resolves the IFC subset needed for quantity review:

- product identity for wall/slab/beam/column/door/window/space;
- `IfcRelContainedInSpatialStructure` storey containment;
- `IfcRelDefinesByType` type names;
- `IfcPropertySet` / `IfcPropertySingleValue` values;
- `IfcElementQuantity` length/area/volume/count/weight values with SI default units;
- `IfcClassificationReference` + `IfcRelAssociatesClassification` classification codes;
- a content-derived `IFCSTEP-*` revision fingerprint;
- `ifc-step://#id` source evidence references that allow a later tessellator/renderer adapter to map the workbench row back to its STEP entity.

The output is the existing `IfcStandaloneDocument` / `IfcStandaloneElement` model, so current spatial/type/classification filters, property tree, selection sets, QTO, BOQ aggregation, CSV export and traceable evidence infrastructure continue to apply.

## Fail-closed behavior

The source rejects malformed STEP envelopes/statements, duplicate STEP ids, duplicate IFC GlobalIds, missing relationship targets, conflicting storey/type/classification assignments, unsupported quantity entities, non-finite quantities and explicit unit references that have not yet been resolved through an IFC unit assignment. Silent partial relationship reconstruction is intentionally avoided.

The included `tests/fixtures/quantbim/minimal-qto.ifc` fixture covers one classified external wall with storey containment, type relationship, a Pset boolean and area/volume base quantities. Smoke coverage also exercises real file-open through `QuantBimStandaloneWorkbench` and duplicate-id rejection.

## Compatibility and next product slices

Existing `IIfcStandaloneSource` implementations remain binary/source compatible; this is an additional adapter. BricsCAD-specific assemblies remain optional and outside the parser boundary.

The carrier has also been revalidated after protected `main` advanced through the Solibri QA executor, Live Workbook/API and Commercial QS modeless-affinity merges. Those sibling changes own disjoint paths; exact-head CI remains the merge authority for cross-cutting compatibility.

This carrier deliberately does **not** claim full IFC implementation or full QuantBIM desktop parity. Remaining product work includes schema-complete unit resolution and entity coverage, geometry tessellation, renderer/navigation UI, persisted named selections/workbench state, Windows shell/file-open UX, installer/signing and packaging validation. Those should be separate ownership slices and should reuse `IfcStandaloneDocument` rather than bypassing it.
