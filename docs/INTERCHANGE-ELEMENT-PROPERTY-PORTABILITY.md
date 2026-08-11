# Interchange ProjectElement property portability

Status: `SOURCE_IMPLEMENTED` on `main` by PR #389 / squash commit `746fd721397e01bd70e7f829709ccfe9afa67ce0`.

This document records the semantic/native boundary for `ProjectElement.Properties` so later import/export work does not reintroduce drawing-local CAD ownership through an otherwise semantic snapshot.

## Canonical rule

A `ProjectElement` property is portable semantic interchange data only when `ProjectInterchangeElementPropertyPolicy.IsPortable(...)` accepts the key.

The policy rejects:

- canonical generated-owner slots recognized by `GeneratedHandleOwnershipPolicy`;
- `Generated*` and `QS3D.Generated*` runtime metadata;
- `PhysicalOpeningCut*` and `QS3D.PhysicalOpeningCut*` runtime metadata;
- any other element property whose key contains `Handle` case-insensitively.

This rule applies to `ProjectElement` properties only. Family properties keep their existing semantic contract; for example a legitimate Family property such as `HandleHeight` is not removed merely because its name contains `Handle`.

## Export boundary

`ProjectInterchangeJsonExporter` filters element properties through the shared portability policy before writing `QS3D.SemanticSnapshot` JSON.

The explicit top-level element fields `sourceHandles`, `drawingFingerprint` and `sourceRefScope` remain separate interchange/provenance fields. They are not target CAD ownership merely because they appear in a snapshot.

## Legacy snapshot read boundary

`ProjectInterchangeJsonValidator` remains compatible with a legacy otherwise-valid snapshot containing an arbitrary element property such as `CadHandle`. Generated/native ownership properties already rejected by the validator remain errors.

After validation, `ProjectInterchangeValidatedSnapshotReader` filters element properties through the same portability policy before creating immutable `InterchangeElementSnapshot` objects. Therefore canonical planners/importers never receive legacy arbitrary handle-bearing element properties as portable semantic data.

This protects AppendOnly, KeepTarget, ImportAsNew/remap, UseSource, field-level merge and provenance compositions through their common typed-read boundary instead of relying on each mutation path to remember a separate filter.

## Regression contract

`ProjectInterchangeElementPropertyPortabilitySmoke` proves that:

- exporter omits an element `CadHandle`;
- exporter preserves a Family `HandleHeight`;
- a legacy snapshot with `CadHandle` can remain validator-compatible but the typed reader does not materialize that property;
- AppendOnly and KeepTarget do not rebind the legacy source handle property;
- field-level merge does not surface `properties.CadHandle` as a reviewable source decision and does not adopt the source value.

`scripts/preflight-interchange-element-property-portability.py` is auto-discovered by `scripts/preflight-all.py` and statically locks the shared policy, exporter boundary, typed-reader boundary, semantic-reference validation retention and smoke coverage.

## Do not weaken this boundary

Do not make arbitrary source `*Handle*` element properties portable in order to support target-DWG rebinding. Native/source-handle adoption is a separate product policy and licensed V25 runtime problem under issue #84.

If a future feature needs a semantically meaningful element attribute whose name contains `Handle`, define and review an explicit portable semantic key/contract instead of globally weakening the fail-closed rule.

This source contract is not a BricsCAD V25 runtime qualification claim. Native cleanup, ownership adoption/rebinding, Undo, save/reopen and multi-DWG behavior remain within the existing local V25 qualification boundary.
