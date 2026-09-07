# Measurement/work-item mapping token resource bound

## Scope

This contract applies to the identity strings accepted by `MeasurementWorkItemMapping` and to measurement-item lookup tokens passed to `MeasurementWorkItemMappingCatalog.Resolve`.

The canonical maximum is **1024 UTF-16 code units per token**. The bound is enforced by `MeasurementWorkItemMappingContract.RequireToken`, the same admission path already responsible for required-value, canonical-whitespace, control-character, and XML-persistability checks.

## Why this boundary exists

The mapping catalog already limits cardinality to 10,000 entries. Without a per-token resource bound, a caller could still make a bounded catalog perform disproportionate allocation, hashing, comparison, XML validation, and later persistence work by supplying arbitrarily large individual mapping identities.

The token-length fence runs before trimming/control-character scanning and XML character validation so over-bound input is rejected at the resource boundary before downstream linear work. In-bound values retain the existing canonicality and XML rules.

## Covered surfaces

The constructor applies the bound independently to:

- `mappingId`;
- `measurementItemId`;
- `classificationId`;
- `workItemId`.

`MeasurementWorkItemMappingCatalog.Resolve` applies the same bound to its `measurementItemId` query token before dictionary lookup.

## Deterministic acceptance matrix

`MeasurementWorkItemMappingTokenBoundSmoke` runs as a module initializer and proves:

1. exactly 1024 UTF-16 code units remain accepted across all constructor identity surfaces and Resolve;
2. 1025 code units fail closed on each constructor identity surface;
3. a 1025-code-unit Resolve query fails before lookup;
4. an oversized token with an XML-invalid tail reports the length boundary first, proving ordering;
5. existing whitespace/control/XML-invalid in-bound rejection remains active.

`scripts/preflight-measurement-work-item-mapping-token-bound.py` pins the production constant, constructor/Resolve coverage, validation ordering, smoke matrix, and module-initializer execution contract.

## Runtime boundary

REMOTE_SAFE managed Core. No licensed BricsCAD, private DWG, Windows UI, or `LOCAL_PASS` evidence is required or claimed.
