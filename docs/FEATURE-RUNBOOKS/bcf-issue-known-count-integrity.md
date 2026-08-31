# BCF bounded collection known-Count integrity

Canonical issue: #4541  
Lane-Key: `issue-4541`  
Runtime: `NOT_APPLICABLE` — deterministic Core/export integrity.

## Contract

`BcfIssueExchangeContract.MaterializeBounded<T>` is the shared caller-controlled enumerable boundary used by BCF topics, viewpoints, comments, and viewpoint components. Supported collection Count evidence is an admission contract, not a hint.

For every successful `MoveNext()` the implementation must perform the independent bounded-count checks before reading `IEnumerator.Current`:

1. reject when the observed item cardinality has already reached the admitted known Count;
2. reject when the operation-specific hard maximum has already been reached;
3. only then read `Current` and retain the item.

After exact traversal, a supported known Count must equal the observed cardinality. The same supported Count surfaces are read again after traversal; source-set changes, value drift, malformed/negative evidence, conflicting interfaces, or newly oversized evidence fail closed. Pure streaming enumerables with no supported Count surface remain valid and are governed only by the hard maximum.

A single supported Count source is sufficient evidence for pre-`Current` overrun rejection. Multiple collection interfaces remain useful for conflict and source-set stability detection, but corroboration is not required before enforcing the admitted cardinality.

## Deterministic regression

`BcfIssueKnownCountIntegritySmoke` uses adversarial collections that count Count reads, `MoveNext` calls, and `Current` reads independently. Count=1/yield=2 must reach the second successful `MoveNext` while keeping `CurrentReads == 1`. A Count value that changes after an exact one-item traversal must fail after a second Count read. Representative top-level topic and nested component surfaces prove the shared boundary, while a pure streaming topic source proves compatibility.

The auto-discovered `scripts/preflight-bcf-issue-known-count-integrity.py` locks the source ordering and rejects regression to caller-controlled `foreach`.

## Validation

Run the feature guard and deterministic Core smoke through Shared CI. Protected exact-current `preflight + core` must be SUCCESS before merge. Licensed BricsCAD/private-DWG runtime evidence is neither required nor claimed for this Core-only contract.
