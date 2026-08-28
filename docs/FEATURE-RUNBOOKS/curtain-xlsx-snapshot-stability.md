# Curtain Wall XLSX snapshot stability

Lane-Key: `issue-4448`

Curtain Wall XLSX is a reporting/interchange deliverable. Its exported row payload and provenance must come from one stable source snapshot before any destination directory, temporary package, or existing workbook is mutated.

The exporter reads each caller-owned outer row index exactly once and builds a detached validated snapshot. It caches the row object references from that single traversal and, after all rows have been copied, revalidates each cached row's scalar schedule values, `ElementIds`, and `SourceHandles` against the detached snapshot. In-place row or provenance mutation that occurs while later rows are being read is therefore rejected fail-closed before filesystem work. Outer-list slot rebinding after an index has already been read does not require a second caller-list read and cannot contaminate the detached deliverable snapshot. Ordinary outer `Count` drift remains rejected as before.

`ElementIds.Count == WallCount` remains the wall-cardinality contract. `SourceHandles` intentionally preserves flattened source provenance and is not required to equal `WallCount`.

Deterministic coverage lives in `CurtainWallXlsxSmoke` and the existing `CurtainWallXlsxRowSnapshotSmoke`: stable workbook compatibility, one-read-per-caller-index behavior, count-stable cross-row provenance mutation rejection, and destination-preservation assertions. The static source guard is `scripts/preflight-curtain-xlsx-snapshot-stability.py` and explicitly forbids restoring a second `rows[rowIndex]` pass.

Runtime classification: `NOT_APPLICABLE`; this is Core-only snapshot/serialization integrity and does not require licensed BricsCAD, Windows UI, or Excel UI execution.
