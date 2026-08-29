# Commercial transient known-Count stability

## Boundary

This contract applies to deterministic caller-controlled counted enumerables consumed by `CommercialAuditLog.AppendBatch(...)` and shared `CommercialGuard.Snapshot<T>(...)`. It is a Core commercial/audit data-integrity boundary and does not require licensed BricsCAD runtime evidence.

## Required behavior

- Bind supported deterministic Count evidence at admission (`ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection`).
- After each successful `MoveNext()`, revalidate the admitted Count contract before the existing declared-count/hard-cap gate and before `IEnumerator.Current` is observed.
- Reject transient Count growth, shrink, negative Count, or cross-interface conflict even when the source restores its original Count before final traversal validation.
- Preserve declared Count N+1 rejection before unexpected semantic Current, post-traversal under-yield rejection, 10,000 audit capacity, 64-entry shared snapshot bound, pure streaming support, duplicate/null validation, detached snapshot semantics, and failure atomicity.
- Stable multi-interface counted sources with equivalent Count values remain valid.

## Regression evidence

`CommercialTransientKnownCountStabilitySmoke` uses adversarial counted collections whose Count changes immediately after successful `MoveNext()` and is restored only if `Current` is read. Rejection must therefore leave `CurrentReads == 0` for hostile cases. The historical commercial known-count overrun guard remains active and continues pinning N+1/null precedence.

## Validation lifecycle

Run auto-discovered feature preflights and deterministic Core smoke tests on the exact branch head. Before PR/merge, reconcile latest protected `main` non-force if needed, require exact current-candidate protected `preflight + core`, merge only with expected-head protection, and verify exact protected-main ancestry.
