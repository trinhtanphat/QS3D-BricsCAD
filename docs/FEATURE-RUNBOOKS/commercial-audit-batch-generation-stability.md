# Commercial audit batch generation stability

## Scope

Issue #5483 hardens `CommercialAuditLog.AppendBatch` against caller-controlled counted collections that preserve `Count` while changing ordered record content across enumerations.

## Failure boundary

For an input exposing an authoritative known `Count`, admission must fail closed if a bounded replay does not match the exact ordered semantic state captured by the first traversal. No event may be appended on failure.

Semantic equality covers every immutable `CommercialAuditRecord` field and the ordered `CommercialRevisionRef` triplets. Reference identity is not the contract.

Raw streaming `IEnumerable<CommercialAuditRecord>` inputs without an authoritative known count remain single-pass compatible.

Existing capacity, duplicate EventId, negative/conflicting/changing Count, null record and atomic batch semantics remain authoritative.

## Deterministic validation

`CommercialAuditBatchGenerationStabilitySmoke` covers same-count replacement, same-count reorder, stable counted input, streaming input and no-partial-append behavior.

`scripts/preflight-commercial-audit-batch-generation-stability.py` is auto-discovered by aggregate feature source guards and binds production + regression structure.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core/commercial correctness; no licensed BricsCAD evidence is required or claimed.
