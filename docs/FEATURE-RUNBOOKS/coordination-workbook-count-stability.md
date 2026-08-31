# Coordination workbook row Count stability

Lane-Key: `issue-4547`

## Scope

This runbook covers `CoordinationWorkbookExporter.Export` and its caller-owned `IReadOnlyList<CoordinationClashExportRow>` snapshot boundary. Runtime qualification is **NOT_APPLICABLE**: this is deterministic Core/XLSX integrity and requires no licensed BricsCAD or private DWG.

## Defect

`IReadOnlyList<T>` is not immutable. The historical exporter checked live `rows.Count` in `Export`, then `Snapshot` sized a result from another live Count read and traversed the caller-owned source with `foreach`. A hostile source could change cardinality between these observations, expose rows from another source generation, or drift after traversal. The snapshot had no exact admitted Count and no post-traversal Count rebound, so a mixed-generation row set could reach deterministic sorting/XML construction before the exporter noticed anything was wrong.

This package is intentionally distinct from Door/Opening XLSX snapshot work and BCF collection work. It owns only the Coordination clash workbook path.

## Production contract

1. Read `rows.Count` once as the admitted row Count.
2. Reject zero and Count above the existing Excel row ceiling before any caller-controlled row indexer access.
3. Pass the admitted Count into `Snapshot`; never reread live Count as a traversal bound.
4. Traverse exactly `0..admittedRowCount-1`.
5. Immediately before each `source[index]` read, require current Count to equal the admitted Count.
6. Revalidate Count after the admitted traversal and before deterministic sorting/XML generation.
7. Preserve canonical clash-ID verification, duplicate rejection, drawing-fingerprint consistency, deterministic sorting, trace identity, XLSX validation and atomic destination replacement.

Because snapshot/XML/package buffers remain local until `AtomicFileCommit.ReplaceWithoutBackup`, Count-integrity failure remains fail-closed with no destination publication.

## Deterministic regression

`CoordinationWorkbookCountStabilitySmoke` uses hostile `IReadOnlyList<CoordinationClashExportRow>` implementations and requires:

- growth after the first admitted row: reject before reading a row beyond admission;
- shrink after the first admitted row: reject before reading a missing/new-generation index;
- Count drift visible only at final rebound: reject after exactly the admitted reads;
- empty and Excel-limit-overflow Count: reject before any indexer read;
- stable caller-owned rows: read each admitted row exactly once and emit byte-for-byte deterministic XLSX output.

The auto-discovered `preflight-coordination-workbook-count-stability.py` pins admitted-Count publication, fixed-index snapshot traversal, pre-index stability checks, final rebound, and the hostile-list smoke registration. It rejects regression to caller-controlled `foreach`.

## Landing

Require focused guard, aggregate discovered guards, Core build/smoke and protected exact-head `preflight + core`. If protected main advances, collision-scan the four reserved paths, reconcile non-force, update the reservation to the successor SHA before publication, and obtain fresh checks. Merge only by expected head and verify the task head is in exact protected-main ancestry.
