# Rate book generation stability

## Scope

Issue #5490 hardens `RateBook` construction against caller-controlled counted collections that preserve every exposed `Count` while changing ordered `RateItem` content between enumerations.

## Failure boundary

When the input exposes an authoritative known count, `RateBook` must replay the source before publishing `Items` and require exact ordered semantic equality with the generation captured by the admitted traversal. Replacement, reorder, cardinality drift, null replay items, or any immutable rate-field change fails closed.

Semantic equality covers rate item ID, exact cost-code text, unit, currency, unit rate, effective UTC timestamp and version. Reference identity is not the contract.

Inputs without an authoritative known count remain single-pass compatible and are never replayed.

Existing 10,000-item capacity, negative/conflicting/changing Count, traversal-cardinality, duplicate ID, ambiguous effective timestamp, canonical ordering and `Resolve` semantics remain authoritative.

## Deterministic validation

`RateBookGenerationStabilitySmoke` covers same-count replacement, reorder, same-identity content mutation, stable counted replay, raw streaming single-pass compatibility, canonical ordering/Resolve preservation and no publication after rejected drift.

`scripts/preflight-rate-book-generation-stability.py` is auto-discovered by aggregate feature source guards and binds the production + regression structure.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core/cost correctness; no licensed BricsCAD evidence is required or claimed.
