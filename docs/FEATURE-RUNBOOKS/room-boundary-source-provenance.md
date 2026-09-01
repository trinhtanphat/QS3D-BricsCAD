# Room boundary source provenance integrity

Carrier: `issue-5162`

Runtime classification: `NOT_APPLICABLE` — deterministic Core geometry/provenance behavior; no licensed BricsCAD runtime evidence is required or claimed.

## Defect

`BoundarySegment` is the public admission point for room-boundary source provenance. Before this carrier it only applied `Trim()` to `sourceId`, allowing malformed UTF-16 and control characters to become immutable `SourceId` values. Direct `RoomBoundaryEngine.Discover` callers do not pass through `RoomBoundaryDiagnosticService.ValidateSourceProvenance`, so invalid provenance could reach discovered `RoomBoundary.SourceIds`.

## Contract

- `null` and whitespace-only source provenance remain optional and canonicalize to empty text.
- Existing surrounding-whitespace trimming remains stable.
- Non-empty source provenance must contain well-formed UTF-16; isolated high or low surrogates fail at `BoundarySegment` construction.
- Control characters fail at `BoundarySegment` construction before topology discovery.
- Well-formed surrogate pairs remain accepted.
- Direct engine discovery continues to retain canonical source provenance on discovered boundaries.
- Existing geometry validation, topology bounds, diagnostic Count integrity, and source deduplication are not weakened.

## Deterministic validation

Run the focused source guard:

```text
python scripts/preflight-room-boundary-source-provenance.py
```

Run Core smoke/build through the repository's normal deterministic Core validation. The module-initialized `RoomBoundarySourceProvenanceSmoke` covers malformed high/low surrogate input, control input, optional/trim normalization, a well-formed surrogate-pair control, and direct-engine provenance retention.

## Acceptance

Merge only after the exact PR head has fresh protected `preflight` and `core` SUCCESS, latest-main reconciliation is current, and expected-head merge verifies the protected main contains the feature head. Licensed BricsCAD `LOCAL_PASS` is not applicable to this Core-only contract.
