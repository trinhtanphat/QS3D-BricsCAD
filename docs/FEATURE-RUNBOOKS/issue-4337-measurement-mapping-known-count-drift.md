# Issue 4337 — Measurement/work-item mapping known Count drift

Status: `SOURCE_FIX_ACTIVE`

Lane-Key: `issue-4337`

Canonical owner: QS3D schedule worker C01

## Defect

`MeasurementWorkItemMappingCatalog` observes known collection cardinality before traversal. Before this lane, an over-yielding collection could advertise `Count = 1`, yield two mappings, and have the second mapping semantically inspected/indexed before the constructor rejected the final cardinality mismatch.

Known cardinality is a fail-closed input boundary. Once one mapping has been accepted for `Count = 1`, the next yielded item is outside the reviewed/trusted collection shape and must be rejected before null/identity/ambiguity validation or indexing.

## Source contract

- Oversized, negative, and conflicting known Counts remain rejected before traversal.
- The first item whose zero-based index is `>= knownCount` fails immediately before mapping semantic processing.
- Under-yield remains detected after traversal.
- Pure streaming sources without a known Count retain the independent 10,000-entry cap.
- Honest counted inputs retain existing sorting, duplicate-ID checks, category/item ambiguity checks, and `Resolve` semantics.

## Deterministic validation

`MeasurementWorkItemMappingCatalogTraversalCountSmoke` proves:

1. under-yield rejects after traversal;
2. over-yield rejects at the first unexpected item;
3. the overrun guard precedes semantic validation by making that unexpected item `null`;
4. honest Count/traversal agreement remains accepted;
5. pure streaming input remains accepted.

`scripts/preflight-measurement-work-item-mapping-known-count-early-drift.py` locks the guard ordering and smoke controls.

## Runtime boundary

This is a Core data-integrity fix. No licensed BricsCAD runtime, private DWG, signing, packaging, or `LOCAL_PASS` evidence is required or claimed by this lane.
