# Interchange append + source-handle provenance

Status: `SOURCE_IMPLEMENTED` for the Core append-composition path. Native BricsCAD V25 import UX/runtime qualification remains separate.

`ProjectInterchangeAppendProvenanceImporter` composes two existing canonical operations as one rollback-protected project mutation:

1. `ProjectInterchangeAppendOnlyImporter` appends all-new portable semantic identities while discarding imported CAD ownership.
2. `ProjectInterchangeSourceHandleProvenance` stores the source snapshot's drawing-local handles as non-owning provenance.

This composition implements the append-only execution slice of `InterchangeSourceHandlePolicy.PreserveAsProvenanceOnly` without creating another provenance storage format.

## Ownership invariant

Imported handles never become target CAD ownership.

After combined import:

- the new target `ProjectElement.SourceHandles` collections remain empty;
- imported element `DrawingFingerprint` remains empty;
- no generated/native owner slot is reconstructed from the snapshot;
- source handles remain only under the canonical `Interchange.Provenance.Source.*` project metadata maintained by `ProjectInterchangeSourceHandleProvenance`;
- portable semantic re-export still excludes that project metadata, so the provenance records cannot become portable target-DWG handle authority.

The combined importer verifies the ownership invariant immediately after the semantic append and before provenance storage is accepted.

## Source drawing scope

The standalone provenance command remains useful for review/archive scenarios and keeps its existing compatibility behavior.

The **combined mutating append path is stricter**: if the source snapshot contains drawing-local handles, `ProjectInterchangeAppendProvenanceImporter` requires a non-empty source drawing fingerprint before mutation. A raw handle without a stable source drawing scope is not sufficient provenance for a semantic import operation.

A snapshot with zero source handles does not need a drawing fingerprint merely to use this combined path.

## Atomic project behavior

The combined importer captures an outer `ProjectStateSnapshot` before semantic mutation. The canonical append importer and provenance store retain their own internal validation/rollback boundaries, but the outer snapshot makes the user-visible composition one project-state operation.

Execution is:

1. plan canonical append-only semantics;
2. plan canonical source-handle provenance;
3. require source drawing fingerprint when source handles exist;
4. verify semantic/provenance source-handle counts agree;
5. capture outer project snapshot;
6. run canonical append-only semantic import;
7. verify imported elements still own no source CAD handles/fingerprint;
8. run canonical provenance storage;
9. verify execution counts still match the pre-mutation plan;
10. record combined import mode/counts/audit.

Any exception after the outer snapshot restores the pre-import project state. This is a project-state atomicity guarantee only; no native BricsCAD entities are created or modified by this Core path.

## Relationship to existing provenance command

`QS3DINTERCHANGEPROVENANCE` and `ProjectInterchangeSourceHandleProvenance` remain the canonical provenance implementation. The combined importer does not replace them and does not add a second raw-handle ledger.

The new class only adds an execution orchestration for the safe all-new append case, where source Element IDs and target Element IDs are identical by the append-only contract.

Import As New/remap and `UseSourceSemanticData` need different source→target identity/cleanup orchestration and are intentionally not routed through this append-only wrapper.

## Still separate

Still open:

- provenance retention for remapped Import As New identities;
- provenance retention with `UseSourceSemanticData` after native cleanup authorization;
- generic reviewed `QS3DINTERCHANGEIMPORT` UX;
- BricsCAD Undo/save-reopen/multi-DWG qualification;
- exact-SHA licensed V25 runtime proof.

This Core composition must not be presented as native CAD handle adoption or as V25-qualified generic round-trip import.
