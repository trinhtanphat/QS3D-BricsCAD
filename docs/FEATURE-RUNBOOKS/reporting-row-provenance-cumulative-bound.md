# Reporting row provenance cumulative bound

## Scope

`ReportingRowProvenance.AppendSourceHandles` is the shared Core helper that merges generated source-handle provenance into reporting rows. The published row must remain bounded even when provenance arrives through multiple individually valid batches.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core/reporting integrity and does not establish licensed BricsCAD `LOCAL_PASS`.

## Invariant

A published reporting row may contain at most **10,000 SourceHandles**.

The bound is cumulative across the existing target and the fully validated staged batch. The helper rejects an already-oversize target before target snapshot allocation or source enumeration. The existing per-input 10,000-entry traversal contract retains precedence: source Count/traversal/canonical/duplicate validation completes first, then cumulative published cardinality is checked before any staged handle is published.

Exactly 10,000 published handles remain accepted. Any otherwise-valid append that would publish entry 10,001 fails closed and leaves the target byte-for-byte unchanged. An input stream itself exceeding 10,000 entries continues to fail with the established input-bound diagnostic before cumulative publication validation.

## Preserved contracts

The correction preserves:

- source known-Count admission, agreement, drift, over-yield and under-yield checks;
- streaming input support and the existing 10,000-entry per-input traversal ceiling and diagnostic precedence;
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

The existing `ReportingRowProvenanceTraversalIntegritySmoke` independently preserves the 10,001-entry streaming-input rejection ordering. `preflight-reporting-row-provenance-cumulative-bound.py` pins target admission and the `per-input bound -> Current/source validation -> stage -> source completion -> cumulative bound -> publish` ordering.

## Merge acceptance

Require exact-head Shared CI `preflight` and `core` SUCCESS, latest protected-main refresh/collision scan, non-force reconciliation if main advanced, one canonical PR carrying `Lane-Key: issue-4906`, expected-head merge, and exact protected-main verification.
