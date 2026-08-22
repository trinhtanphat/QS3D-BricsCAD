# UseSourceSemanticData + source-handle provenance

Status: `SOURCE_IMPLEMENTED` for the Core/project-state composition. Native cleanup itself and BricsCAD V25 runtime qualification remain `LOCAL_ONLY`.

`ProjectInterchangeUseSourceProvenanceImporter` composes the canonical `UseSourceSemanticData` executor with the canonical source-handle provenance store and semantic target-lineage map.

The key rule is unchanged: provenance retention does not perform native cleanup and does not weaken cleanup authorization.

## Authorization boundary

`ProjectInterchangeUseSourceSemanticImporter.Plan` remains authoritative for affected target elements and its exact per-element `NativeCleanupRequirements`. `TargetElementIdsRequiringNativeCleanup` is a reporting/convenience view only.

The combined importer accepts the same `ProjectInterchangeNativeCleanupAuthorization` object and forwards it unchanged to the canonical UseSource executor. A successful native workflow should create that authorization with `ProjectInterchangeNativeCleanupAuthorization.ForPlan(plan.SemanticPlan)` after cleaning or transactionally staging the exact generated handles in the reviewed semantic plan.

The canonical executor re-plans immediately before mutation and rejects missing, element-ID-only, or stale handle-bound authorization. Therefore provenance composition cannot turn an authorization for old handle `H1` into permission for a newly observed handle `H2` on the same Element ID.

Provenance is never interpreted as permission to erase native CAD, and storing source handles does not satisfy the cleanup contract.

## Provenance records

After successful authorized semantic replacement/addition:

- raw source-handle provenance is stored through `ProjectInterchangeSourceHandleProvenance`;
- source-to-target semantic lineage is stored through `ProjectInterchangeProvenanceTargetMap`;
- UseSource keeps source and target Element IDs identical for source identities, so the lineage is explicit identity mapping;
- imported raw handles remain outside `ProjectElement.SourceHandles`;
- imported drawing fingerprints remain outside target Element ownership.

The combined mutating path requires a non-empty source drawing fingerprint whenever drawing-local source handles are present.

## Project-state atomicity

Execution is:

1. plan canonical UseSource replacement and exact generated-handle cleanup requirements;
2. plan canonical raw source-handle provenance;
3. preserve the canonical native-cleanup requirement;
4. build source ElementId -> target ElementId identity lineage;
5. reject unscoped source handles;
6. capture an outer `ProjectStateSnapshot`;
7. call canonical UseSource import with the caller's handle-bound cleanup authorization;
8. verify source Elements do not own imported CAD handles/fingerprint in the target project;
9. store canonical raw source-handle provenance;
10. store semantic target lineage;
11. verify planned/executed counts and record combined status/audit.

Any Core exception after the outer snapshot restores the pre-operation project state. This does not undo native cleanup already performed outside Core; that is why the adapter still needs a real whole-operation BricsCAD transaction or durable compensation/recovery workflow before a generic command can expose this path.

## Target-only affected elements

UseSource can invalidate target-only elements because they consume a replaced Family/Floor or depend on a replaced host/element. Those target-only elements are handled by the canonical UseSource cleanup/dirty contract.

They are not added to source provenance mapping because they did not come from the source snapshot.

## Ownership invariant

After successful UseSource + provenance:

- source snapshot handles are provenance only and never target CAD ownership;
- source snapshot drawing fingerprint is provenance only and never target Element ownership;
- generated/native ownership cleared by the canonical UseSource contract remains cleared and dirty for rebuild;
- `ProjectInterchangeProvenanceTargetMap` contains semantic lineage only, no CAD handles;
- portable semantic re-export excludes project provenance metadata.

## Runtime boundary

Still `LOCAL_ONLY` / separate:

- actual native generated-object cleanup;
- cleanup + semantic replacement + rebuild as one real BricsCAD operation;
- failure injection between native cleanup and Core mutation;
- Undo/save-reopen/multi-DWG;
- generic reviewed `QS3DINTERCHANGEIMPORT` UI;
- exact-SHA licensed BricsCAD V25 qualification.

This Core composition must not be described as proof that native cleanup occurred. The cleanup authorization is an exact generated-handle-set handoff from a guarded adapter/runtime workflow, not a bypass flag.
