# Cubicost-style exact Solid3d MEP clash — BricsCAD V25

Updated: 2026-08-15 (UTC+7)
Issue: #1641
Upstream: #1638 / #1636

## Scope

`QS3DMEPEXACTCLASH` is the read-only hard-clash narrow phase that follows the broad-phase envelope coordination workflow.

The command intentionally lives in a new adapter file so it does not overlap the active recognition/Locate lane.

Data path:

1. read PICKFIRST or interactive source selection through `EntitySnapshotReader`;
2. resolve current live handles through `CadHandleService.Resolve`;
3. accept native `Solid3d` entities only;
4. classify each solid with the host-neutral `MepRecognitionProfiles.CreateDefault()` profile;
5. skip `Unmatched` and `Ambiguous` recognition results;
6. read native `GeometricExtents` and use them only as a conservative broad-phase rejection test;
7. for remaining pairs with at least one MEP participant, call native `Solid3d.CheckInterference` as the exact hard-clash predicate;
8. report the deterministic Handle pairs that interfere.

The broad-phase extents are never used as the final exact-clash answer.

## Bounded execution

Exact pair testing is intentionally bounded for interactive safety:

- at most 500 recognized selected `Solid3d` candidates;
- at most 100,000 extents-overlapping broad-phase pairs;
- exceeding either limit fails explicitly and asks the operator to narrow selection.

Pairs where neither participant is MEP are ignored before exact testing.

## Read-only safety boundary

The implementation requires:

- `StartOpenCloseTransaction()`;
- `OpenMode.ForRead` only;
- all live `Solid3d` references stay inside the document-thread transaction;
- no `BooleanOperation`;
- no clone/copy/append/erase/transform path;
- no project bootstrap, QSDB/sidecar write or semantic state mutation;
- no background task/parallel access to native DBObjects.

`CheckInterference` is used only as a predicate. The command does not request or create an interference solid.

## Relationship to broad-phase clash

- `QS3DMEPCLASH`: fast AABB hard/clearance coordination across recognized entities, including non-solids.
- `QS3DMEPCLASHLOCATE`: select a reviewed broad-phase pair.
- `QS3DMEPEXACTCLASH`: selected native `Solid3d` hard-clash confirmation using `CheckInterference`.

AABB clearance remains a coordination/near-miss signal. This lane does not claim an exact offset/clearance solid engine.

## LOCAL_ONLY qualification

Final runtime truth requires licensed BricsCAD V25. On the exact integrated SHA, use disposable solids with explicit naming rules and validate:

1. two overlapping MEP/Structure solids: broad-phase candidate and exact hard clash;
2. two solids whose AABBs overlap but geometry does not: broad-phase candidate but no exact clash;
3. touching-only control if supported by the modeler: record native `CheckInterference` result without forcing an expectation not guaranteed by source review;
4. MEP-vs-MEP exact hard clash;
5. Structure-vs-Architecture pair is excluded when neither side is MEP;
6. unknown/ambiguous classification is skipped;
7. non-Solid3d selection is skipped;
8. dense selection limit fails safely;
9. two DWGs prove document affinity and no cross-document leakage;
10. no project, sidecar or DWG mutation attributable to the command.

Record exact SHA, BricsCAD V25 build, TD_Mgd version, plugin hash/ProductVersion, fixture descriptions, expected/actual pair matrix and cleanup evidence.

Status: `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`. Static/source inspection is not a licensed V25 runtime PASS.
