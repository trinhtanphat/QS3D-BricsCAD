# QuantBIM IFC STEP ingestion

Issue #6500 introduced the host-neutral IFC STEP source consumed by the canonical `QuantBimStandaloneWorkbench`. Issue #6560 extends that same source with quantity-specific and project/global SI-unit resolution; it does not create another workbench and does not add a BricsCAD dependency to `QS3D.Core`.

## Supported QS ingestion slice

`IfcStepStandaloneSource` implements `IIfcStandaloneSource` and deterministically resolves the IFC subset needed for quantity review:

- product identity for wall/slab/beam/column/door/window/space;
- `IfcRelContainedInSpatialStructure` storey containment;
- `IfcRelDefinesByType` type names;
- `IfcPropertySet` / `IfcPropertySingleValue` values;
- `IfcElementQuantity` length/area/volume/count/weight values;
- quantity-specific `IfcSIUnit` references when `IfcPhysicalSimpleQuantity.Unit` is present;
- project `IfcUnitAssignment` through `IfcProject.UnitsInContext` when the quantity-specific unit is omitted;
- SI prefix normalization, including dimensional scaling for area/volume and IFC's gram-based `MASSUNIT` convention;
- `IfcClassificationReference` + `IfcRelAssociatesClassification` classification codes;
- a content-derived `IFCSTEP-*` revision fingerprint;
- `ifc-step://#id` source evidence locators.

The parser normalizes supported quantities into the existing QS3D canonical downstream units: `m`, `m2`, `m3`, `kg`, and `count`. This keeps current QTO, BOQ, CSV, traceability and aggregation contracts source-compatible even when an IFC exchange file uses prefixed SI units such as millimetres or grams.

## Unit precedence and compatibility

IFC quantity semantics are applied in this order:

1. when a simple quantity has an explicit `Unit` reference, that unit is authoritative for the quantity;
2. otherwise, a relevant SI unit from the `IfcProject.UnitsInContext` `IfcUnitAssignment` is used;
3. when neither is present, the parser retains the pre-#6560 compatibility behavior and treats length/area/volume/weight values as already expressed in QS3D canonical SI units.

Count quantities remain canonical `count`; explicit count-unit extensions are outside this bounded slice.

The representative fixture now exercises:

- an explicit prefixed area unit overriding the project's area unit;
- global millimetre length normalization to metres;
- global decimetre-based volume normalization to cubic metres;
- IFC gram mass normalization to kilograms;
- real file-open through `QuantBimStandaloneWorkbench` after normalization;
- legacy files with no project unit assignment.

## Fail-closed behavior

The source rejects malformed STEP envelopes/statements, duplicate STEP ids, duplicate IFC GlobalIds, missing relationship targets, conflicting storey/type/classification assignments, unsupported quantity entities and non-finite quantities. Unit handling also fails closed on dangling explicit unit references, quantity/unit-type mismatches, ambiguous project unit assignments, conflicting relevant global units, malformed SI units, unsupported SI prefixes, and relevant conversion/context-dependent units that are not yet implemented.

Conversion-based units such as foot/inch and their area/volume forms remain a separate bounded extension. They must not be silently treated as SI or relabeled as canonical units without a validated conversion chain.

## Product boundary

The host-neutral IFC/QTO contracts in `QS3D.Core` are reusable shared logic for the QS3D product family. This repository still ships the BricsCAD V25/V26 plugin as defined by `docs/PRODUCT-BOUNDARY.md`; a standalone desktop executable/shell/renderer belongs to sibling `QS3D-CAD` and is not created by this ingestion slice.

Geometry tessellation also remains outside this parser boundary. `GeometryReference` is evidence/provenance identity, not geometric quantity evidence.

## Remaining bounded IFC work

This slice does not claim schema-complete IFC support. Legitimate follow-up work includes conversion-based units, broader entity/schema coverage, richer property/value types and production tessellation adapters, each behind a separate ownership/evidence carrier. Existing `IIfcStandaloneSource`, `IfcStandaloneDocument`, QTO and downstream commercial contracts remain unchanged.
