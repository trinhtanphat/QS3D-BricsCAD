# Interchange remap Unicode scalar boundary

## Scope

This carrier protects import-as-new remap IDs and names from being truncated between the UTF-16 code units of one valid supplementary Unicode scalar.

## Production contract

`ProjectInterchangeRemapPlanner.AppendBounded` remains length-bounded in UTF-16 code units because the downstream runtime limits are expressed by `string.Length`, but the retained prefix must never end on an unmatched high surrogate. Before publishing a bounded candidate it also fails closed for malformed UTF-16 source or suffix input.

Existing trim behavior, `-import` / `-import-N` and ` (Imported)` suffixes, deterministic allocation order, case-insensitive occupancy semantics and configured maximum lengths remain unchanged.

## Deterministic coverage

`ProjectInterchangeRemapUnicodeBoundarySmoke` covers:

- a Zone ID whose supplementary scalar lands exactly across the truncation boundary;
- an over-limit Family name containing a supplementary scalar near the retained-prefix boundary;
- an ordinary BMP collision control preserving the historical `DUP-import` result;
- malformed UTF-16 fail-closed behavior at the private bounded-append boundary.

Focused static guard: `python scripts/preflight-interchange-remap-unicode-boundary.py`.

Runtime classification: `NOT_APPLICABLE`; this is deterministic Core interchange identity correctness and does not claim licensed BricsCAD runtime evidence.
