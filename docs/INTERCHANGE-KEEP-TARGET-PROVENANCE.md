# KeepTarget + source-handle provenance

Status: `SOURCE_IMPLEMENTED` for the Core/project-state composition.

`ProjectInterchangeKeepTargetProvenanceImporter` combines the canonical KeepTarget semantic policy with canonical raw source-handle provenance and semantic target-lineage storage.

The central rule is that KeepTarget **does not create false lineage** for collisions.

## Collision semantics

For a source Element whose ID already exists in the target, canonical KeepTarget keeps the existing target Element and discards the source semantic Element for mutation purposes.

Therefore:

- raw source-handle provenance may still be retained for review of the source snapshot;
- the existing target Element remains unchanged and keeps its own target-DWG source handles/fingerprint;
- no source->target lineage record is created for that collided source Element;
- the target must not be described as having been imported from that source Element merely because their IDs collide.

The lineage rule is explicit: only actually appended source Elements receive source->target semantic lineage. Under the all-new KeepTarget append slice, their source and target Element IDs are identical.

## Execution

The combined operation:

1. plans canonical KeepTarget semantics;
2. plans canonical raw source-handle provenance;
3. requires a source drawing fingerprint whenever drawing-local source handles exist;
4. determines which source Elements are actually appended and which collide;
5. captures an outer `ProjectStateSnapshot`;
6. executes canonical KeepTarget import;
7. verifies every mapped appended target Element has empty imported `ProjectElement.SourceHandles` and imported drawing fingerprint;
8. stores raw source-handle provenance through the canonical provenance store;
9. stores target lineage only for actually appended source Elements;
10. records added-mapping, collision-without-lineage and provenance-handle counts.

Any Core failure after the outer snapshot restores the pre-operation project state.

## Provenance versus ownership

Raw source-handle provenance remains historical data only and is never target CAD ownership. A colliding source Element may have raw provenance records even though the target Element with the same ID is not mapped to it.

An actually appended source Element is mapped to its new target semantic Element, but that target still owns no imported CAD handle/fingerprint. Generated/native geometry must be created in the target drawing through normal target-DWG generation workflows.

The portable semantic re-export boundary excludes project provenance metadata, so neither raw handles nor lineage records become portable Element ownership.

## Why this differs from other policies

- Append-only: every source Element is appended, so source and target IDs are the same and no special collision exclusion is needed.
- Import As New: collisions are remapped, so every source Element gets explicit source->remapped-target lineage.
- UseSource: source semantic data replaces compatible target identities and may require native cleanup authorization, so source identity lineage is valid only after that guarded replacement.
- KeepTarget: collided source semantic data is not applied, so mapping the collision to the existing target would be false lineage.

## Remaining boundary

This Core composition does not create a generic BricsCAD import UI, modify native DWG entities, or provide V25 Undo/save-reopen/multi-DWG proof. Those remain separate runtime qualification work.
