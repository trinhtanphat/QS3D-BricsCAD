# Issue #4298 — Quantity evidence known-Count overrun ordering

Lane-Key: `issue-4298`

## Purpose

`QuantityEvidenceCollectionSnapshot.Capture<T>` is the shared bounded snapshot path for contribution operands and explanation contributions/adjustments. It already binds supported known Count contracts before enumeration and preserves an independent 10,000-item bound for pure streams. This lane makes the known Count boundary fail earlier: once item `knownCount + 1` is observed, the collection contract is already invalid and that unexpected item must not reach null/semantic handling or retention.

## Contract

1. Bind `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` Count metadata before traversal.
2. Preserve pre-enumeration rejection for negative, oversized, and conflicting known Counts.
3. During traversal, when a known Count exists, reject the first item whose zero-based index equals the known Count before the global streaming guard, null validation, sorting, identity work, or retention.
4. Preserve the independent 10,000-item pure-streaming ceiling when no known Count exists.
5. Preserve post-traversal mismatch rejection for under-traversal.
6. Preserve deterministic ordering, evidence IDs, arithmetic reconciliation, export projection, and valid counted behavior.

## Deterministic validation

`QuantityEvidenceKnownCountOverrunSmoke` covers:

- underreported operand collection whose first unexpected item is null, proving Count mismatch wins before null validation;
- the same precedence through explanation contribution capture;
- overreported/under-traversal mismatch after otherwise valid enumeration;
- honest counted input and deterministic operand ordering;
- pure-streaming 10,001st-item capacity rejection.

`preflight-quantity-evidence-known-count-overrun.py` statically locks the source ordering and smoke/runbook registration contract. Normal Core smoke plus Shared CI remain authoritative validation.

## Runtime boundary

Licensed BricsCAD runtime is `NOT_APPLICABLE` for this Core-only collection-integrity change. No `LOCAL_PASS`, private-DWG, package-signing, or native-host evidence is required or claimed.

## Landing boundary

Terminal source state is `MERGED_MAIN`: canonical branch and PR for `issue-4298`, exact-head validation, latest-main reconciliation if needed, protected current-candidate `preflight` + `core` SUCCESS, expected-head merge, then exact protected-main verification.
