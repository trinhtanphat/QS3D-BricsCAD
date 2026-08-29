# Room-finish XLSX row snapshot stability

Status: source-safe deterministic guard for Issue #4605.

## Product boundary

Room Finish rows remain authoritative outputs of `RoomFinishScheduleBuilder`; this change does not alter finish quantity, room-link, material, unit or category calculations. It hardens only the Core XLSX serialization boundary documented by the README reporting/export capability and remains independent of BricsCAD runtime qualification.

`RoomFinishXlsxExporter.Export(...)` accepts a caller-owned `IReadOnlyList<RoomFinishScheduleRow>`. The outer list is intentionally read by index exactly once per row so validation does not repeatedly traverse a live caller collection. The exporter then works from detached row snapshots.

## Integrity invariant

A detached row must represent one stable source-row state before any filesystem mutation. Reading a later outer row must not be able to mutate an earlier already-snapshotted source row and silently publish stale text, numeric or provenance values.

The exporter therefore:

1. binds the deterministic outer row count;
2. reads each outer row index once and retains that already-read source reference;
3. snapshots and validates the detached row;
4. confirms the outer count remains unchanged;
5. revalidates every retained source row against its detached snapshot without re-reading the caller-owned outer list;
6. only after stability succeeds creates the destination directory/temp workbook and performs atomic replacement.

Text and provenance comparisons are ordinal after the same null-to-empty normalization used by the snapshot. Integer values compare directly. `double` values use `Double.Equals`, avoiding operator-specific `NaN` equality semantics while numeric admission continues to require finite non-negative values.

Nested `ElementIds`, `RoomIds` and `SourceHandles` must retain both count and ordinal value order through the snapshot boundary. Any drift throws `InvalidOperationException` before filesystem work.

## Regression coverage

`RoomFinishXlsxRowSnapshotSmoke` covers:

- stable input still succeeds while every caller-owned outer row index is read once;
- a later row read mutating an earlier text field fails closed;
- a later row read mutating earlier provenance membership/value fails closed;
- an existing destination remains byte-for-byte unchanged on detected drift.

`scripts/preflight-room-finish-xlsx-row-snapshot-stability.py` is auto-discovered by aggregate preflight and locks the retained-source/stability-check ordering plus the single-read regression tokens.

## Non-goals

This carrier does not change `RoomFinishScheduleBuilder`, Room Finish formulas, native BricsCAD tables, other XLSX exporters, local V25 qualification, workflow policy or release behavior.

## Validation

Required repository validation is the ordinary exact-head automatic branch Shared CI followed by the protected current PR candidate `preflight` and `core` checks. No LOCAL_ONLY evidence is introduced by this source-only serialization hardening.
