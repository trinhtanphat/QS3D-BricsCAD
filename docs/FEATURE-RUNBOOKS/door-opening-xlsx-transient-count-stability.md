# Door/Opening XLSX transient known-Count stability

Lane-Key: `issue-4705`

## Scope

`DoorOpeningXlsxExporter.Export(...)` accepts a caller-owned `IReadOnlyList<DoorOpeningScheduleRow>`. The exporter must bind every supported deterministic Count surface at admission and must not read semantic row content while any admitted Count surface is transiently inconsistent with that admission.

This contract extends the historical Door/Opening XLSX count and snapshot protections without changing workbook schema, schedule grouping, provenance semantics, Excel limits, or atomic publication.

## Invariant

For the top-level exported row source:

1. discover and bind every supported Count surface (`IReadOnlyCollection<T>`, `ICollection<T>`, and non-generic `ICollection` when present);
2. reject negative, conflicting, or over-limit admission before any caller row indexer read or filesystem mutation;
3. immediately before each caller-controlled row indexer read, re-read every admitted Count surface and require the same source set and exact admitted value;
4. immediately after the indexer read, revalidate again before any semantic row snapshot work;
5. after the row snapshot, revalidate again so a transient mutation during semantic extraction cannot be restored silently before publication;
6. retain final exact Count revalidation after traversal;
7. keep each stable row index read exactly once.

A transient Count mismatch is a data-integrity failure even if the source later restores the original value.

## Deterministic regression

`DoorOpeningXlsxTransientCountStabilitySmoke` uses a public `IReadOnlyList<DoorOpeningScheduleRow>` that also implements `ICollection<DoorOpeningScheduleRow>` with independently scripted Count reads. It proves:

- a transient generic Count growth is rejected before the first row indexer read;
- drift observed immediately after an indexer read fails before semantic snapshot/publication;
- an existing destination remains byte-for-byte unchanged on failure;
- stable multi-interface Count evidence exports successfully;
- stable input performs one caller row indexer read per admitted row.

`python scripts/preflight-door-opening-xlsx-transient-count-stability.py` is auto-discovered and pins the traversal ordering around the caller indexer.

## Validation

Run the focused preflight, aggregate preflight, Core build, and deterministic smoke suite. Repository Shared CI on the exact pushed SHA is authoritative. This is deterministic Core export/data-integrity work; licensed BricsCAD/private-DWG runtime evidence is not applicable and no `LOCAL_PASS` is produced.
