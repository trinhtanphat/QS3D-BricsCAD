# Import As New — source-handle provenance and target lineage

Status: `SOURCE_IMPLEMENTED` for the Core/project-state composition. Native BricsCAD V25 import UX/runtime qualification remains `LOCAL_ONLY`.

`ProjectInterchangeRemapProvenanceImporter` combines Import As New/remap semantics with provenance retention without turning source DWG handles into target ownership.

Because Import As New may change Element IDs, provenance needs **two complementary records**:

1. `ProjectInterchangeSourceHandleProvenance` remains the canonical raw source handles record, indexed by source project/source Element identity.
2. `ProjectInterchangeProvenanceTargetMap` stores source-to-target semantic lineage: source ElementId -> imported target ElementId.

The target-map record contains no raw CAD handles. The raw provenance record contains no authority to claim target native entities. Together they allow later review of where imported semantics came from while preserving the ownership boundary.

## Planning

The combined plan uses the existing canonical `ProjectInterchangeRemapAppendImporter.Plan` and `ProjectInterchangeSourceHandleProvenance.Plan`.

For every source Element, the target semantic ID comes only from `ProjectInterchangeRemapPlan.MapId`. The composition refuses duplicate source mappings or duplicate target IDs. It does not invent a second remap algorithm.

When drawing-local source handles exist, the mutating combined path requires a non-empty source drawing fingerprint so the provenance is scoped to the source drawing.

## Execution

Execution is rollback-protected at the outer project-state boundary:

1. re-plan canonical Import As New;
2. plan canonical source-handle provenance;
3. build one-to-one source ElementId -> planned target ElementId lineage;
4. reject unscoped source handles;
5. capture `ProjectStateSnapshot`;
6. execute canonical `ProjectInterchangeRemapAppendImporter`;
7. verify every mapped target Element exists and still has empty imported `SourceHandles`/drawing fingerprint;
8. store raw source-handle provenance through the canonical provenance store;
9. store source-to-target semantic lineage through `ProjectInterchangeProvenanceTargetMap`;
10. verify execution counts against the pre-mutation plan;
11. record combined mode/counts/audit.

If any later step fails, the outer snapshot restores the project to its pre-import state. This is project-state atomicity; the Core path does not touch native DWG entities.

## Target-map storage boundary

`ProjectInterchangeProvenanceTargetMap` persists bounded, encoded records under `Interchange.Provenance.TargetMap.*`.

The map is deliberately semantic-only:

- source ProjectId;
- source drawing fingerprint;
- source ElementId;
- target ElementId.

It rejects mappings whose target Element does not exist, duplicate source IDs, or duplicate target IDs. Records for the same source project replace that source project's previous target-map records so stale lineage does not accumulate.

The map never contains a source CAD handle, native ObjectId, generated owner slot or physical-cut ownership state.

## Ownership invariant

After Import As New with provenance:

- imported target Elements have empty `ProjectElement.SourceHandles`;
- imported target Elements have no imported drawing fingerprint;
- generated/native ownership remains absent and requires explicit target-DWG generation;
- raw source handles remain provenance only and are never target CAD ownership;
- source-to-target semantic lineage remains metadata only;
- neither record can be treated as target CAD ownership.

The portable semantic re-export boundary excludes project metadata, so neither raw provenance records nor the target mapping are promoted to portable Element ownership.

## Why the mapping is separate

Append-only import keeps source and target Element IDs identical, so a separate target map is not necessary there. Import As New deliberately remaps collisions, making the source->target relationship explicit and persistent.

`UseSourceSemanticData` is different again: replacement can require native generated-object cleanup and affected-target invalidation. Provenance composition for UseSource must be designed around that cleanup authorization and must not simply reuse this Import As New wrapper.

## Qualification boundary

Still `LOCAL_ONLY` / separate product work:

- generic reviewed `QS3DINTERCHANGEIMPORT` UI;
- BricsCAD Undo/save-reopen/multi-DWG behavior;
- native generated-object rebuild after import;
- source-file navigation workflows that intentionally open/resolve the original source drawing;
- exact-SHA licensed BricsCAD V25 qualification.

This source implementation is not native CAD-handle adoption and must not be described as V25-qualified generic round-trip interoperability.
