# Curtain Wall XLSX snapshot stability

Lane-Key: `issue-4448`

Curtain Wall XLSX is a reporting/interchange deliverable. Its exported row payload and provenance must come from one stable source snapshot before any destination directory, temporary package, or existing workbook is mutated.

The exporter therefore binds both the outer row identities and each copied row value. After traversal it revalidates the same indexed row identity, scalar schedule values, `ElementIds`, and `SourceHandles`. A caller that changes/rebinds rows or provenance while preserving collection `Count` is rejected fail-closed rather than producing a mixed-state workbook. Ordinary Count drift remains rejected as before.

`ElementIds.Count == WallCount` remains the wall-cardinality contract. `SourceHandles` intentionally preserves flattened source provenance and is not required to equal `WallCount`.

Deterministic coverage lives in `CurtainWallXlsxSmoke`, including stable workbook compatibility, count-stable row replacement, count-stable provenance mutation, and destination-preservation assertions. The static source guard is `scripts/preflight-curtain-xlsx-snapshot-stability.py`.

Runtime classification: `NOT_APPLICABLE`; this is Core-only snapshot/serialization integrity and does not require licensed BricsCAD, Windows UI, or Excel UI execution.
