# QuantBIM IFC STEP ingestion

Issue #6500 introduced the host-neutral IFC STEP source consumed by the canonical `QuantBimStandaloneWorkbench`. Issue #6560 added quantity-specific and project/global SI-unit resolution. Issue #6565 extends that same resolver with bounded `IfcConversionBasedUnit` support; no second workbench or BricsCAD dependency is introduced into `QS3D.Core`.

## Supported QS ingestion slice

`IfcStepStandaloneSource` implements `IIfcStandaloneSource` and deterministically resolves the IFC subset needed for quantity review:

- product identity for wall/slab/beam/column/door/window/space;
- `IfcRelContainedInSpatialStructure` storey containment;
- `IfcRelDefinesByType` type names;
- `IfcPropertySet` / `IfcPropertySingleValue` values;
- `IfcElementQuantity` length/area/volume/count/weight values;
- quantity-specific unit references when `IfcPhysicalSimpleQuantity.Unit` is present;
- project `IfcUnitAssignment` through `IfcProject.UnitsInContext` when the quantity-specific unit is omitted;
- `IfcSIUnit` prefix normalization, including dimensional area/volume scaling and IFC's gram-based `MASSUNIT` convention;
- bounded `IfcConversionBasedUnit -> IfcMeasureWithUnit` chains for length, area, volume and mass when the component unit ultimately resolves to a supported SI or conversion-based unit of the same dimensional type;
- `IfcClassificationReference` + `IfcRelAssociatesClassification` classification codes;
- content-derived `IFCSTEP-*` revision fingerprints and `ifc-step://#id` evidence locators.

Supported quantities continue to normalize into the existing downstream units `m`, `m2`, `m3`, `kg`, and `count`. Conversion-unit names are not trusted as conversion authority: scale comes from the IFC conversion-factor graph and dimensional metadata.

## Unit precedence and compatibility

Quantity unit precedence remains:

1. explicit simple-quantity `Unit` reference;
2. relevant unit from the project's `IfcUnitAssignment`;
3. legacy compatibility default treating omitted length/area/volume/weight units as already canonical SI when no relevant project unit is declared.

Count remains canonical `count`; explicit count-unit extensions are outside this bounded slice. Existing prefixed-SI behavior from #6560/#6562 remains source-compatible.

## Conversion-based unit resolution

For a supported `IfcConversionBasedUnit`, the resolver now requires:

- an `IfcDimensionalExponents` reference matching the declared unit type;
- a valid `IfcMeasureWithUnit` conversion-factor record;
- a positive finite typed measure (`IfcLengthMeasure`, `IfcAreaMeasure`, `IfcVolumeMeasure`, or `IfcMassMeasure` as appropriate);
- a component unit whose resolved dimensional type matches the conversion-based unit;
- an acyclic reference chain.

This supports data-driven units such as foot, square foot, cubic foot and nested inch-through-foot chains without hard-coding those names. Quantity-specific conversion units still override global project units.

The representative `tests/fixtures/quantbim/conversion-qto.ifc` fixture and `QsQuantBimConversionUnitSmoke` exercise global foot length, explicit square-foot area, global cubic-foot volume, SI gram compatibility, a nested inch conversion chain, and deterministic fail-closed cases.

## Fail-closed behavior

The source rejects malformed STEP envelopes/statements, duplicate STEP ids/GlobalIds, missing relationship targets, conflicting storey/type/classification assignments, unsupported quantity entities and non-finite quantity values. Unit handling rejects dangling unit/factor references, unit-type mismatches, ambiguous/conflicting global units, malformed SI units/prefixes, dimensional-exponent mismatches, wrong conversion-measure types, non-positive/non-finite conversion factors, cyclic conversion chains, offset conversion units, and context-dependent units.

`IfcConversionBasedUnitWithOffset` remains intentionally unsupported because offset units are not valid bounded quantity-normalization semantics for this QS slice. Unsupported or malformed units are never silently relabeled as canonical SI.

## Product boundary

The host-neutral IFC/QTO contracts in `QS3D.Core` are reusable shared logic for the QS3D product family. This repository still ships the BricsCAD V25/V26 plugin as defined by `docs/PRODUCT-BOUNDARY.md`; standalone desktop executable/shell/renderer work belongs to sibling `QS3D-CAD`.

Geometry tessellation remains outside this parser boundary. `GeometryReference` is evidence/provenance identity, not geometric quantity evidence.

## Remaining bounded IFC work

This slice does not claim schema-complete IFC support. Legitimate follow-up work includes broader entity/schema coverage, richer property/value types, additional standards-backed unit forms where commercially relevant, and production tessellation adapters. Each requires a separate ownership/evidence carrier and must reuse `IfcStandaloneDocument`, QTO and downstream commercial contracts rather than fork them.
