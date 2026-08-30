# Reporting row provenance cumulative bound

## Scope

`ReportingRowProvenance.AppendSourceHandles` is the shared Core helper that merges generated source-handle provenance into reporting rows. The published row must remain bounded even when provenance arrives through multiple individually valid batches.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core/reporting integrity and does not establish licensed BricsCAD `LOCAL_PASS`.

## Invariant

A published reporting row may contain at most **10,000 SourceHandles**.

The bound is cumulative across the existing target and the fully validated staged batch. The helper rejects an already-oversize target before target snapshot allocation or source enumeration. During a valid append, each new handle still passes canonicality and duplicate-identity validation before the cumulative ceiling is checked; no staged handle is published until the full source traversal succeeds.

Exactly 10,000 published handles remain accepted. Any append that would publish entry 10,001 fails closed and leaves the target byte-for-byte unchanged.

## Preserved contracts

The correction preserves:

- source known-Count admission, agreement, drift, over-yield and under-yield checks;
- streaming input support and the existing 10,000-entry per-input traversal ceiling;
- target stability checks throughout source traversal;
- canonical handle validation and case-insensitive normalized duplicate rejection;
- atomic publication after complete successful traversal;
- deterministic source order.

## Deterministic regression

`ReportingRowProvenanceCumulativeBoundSmoke` covers:

1. 9,999 existing handles plus one unique handle reaches the exact 10,000 boundary;
2. 9,999 plus two unique handles fails atomically with no target mutation;
3. an already-oversize target fails before the source enumerator is requested;
4. an ordinary small append remains accepted.

`preflight-reporting-row-provenance-cumulative-bound.py` pins target admission and the `canonicalize -> duplicate validation -> cumulative bound -> stage -> publish` ordering.

## Merge acceptance

Require exact-head Shared CI `preflight` and `core` SUCCESS, latest protected-main refresh/collision scan, non-force reconciliation if main advanced, one canonical PR carrying `Lane-Key: issue-4906`, expected-head merge, and exact protected-main verification.
