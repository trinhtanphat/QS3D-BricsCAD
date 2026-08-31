# SourceHandleResolver transient root known-Count stability

## Boundary

This contract applies to caller-controlled root semantic element-id sequences consumed by `SourceHandleResolver.Resolve(...)`. It is deterministic Core Locate/data-integrity behavior and does not require licensed BricsCAD runtime evidence.

## Required behavior

- Bind supported deterministic Count evidence at admission from `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection`.
- After each successful `MoveNext()`, re-read all supported Count surfaces before the existing admitted-count and 10,000-entry gates and before `IEnumerator.Current` is observed.
- Reject transient growth, shrink, negative Count, or cross-interface conflict even if the source would restore its original Count when `Current` is read or before final traversal validation.
- Preserve N+1 rejection before unexpected `Current`, under-yield/final Count rebound, canonical semantic-id validation, blank-id filtering, pure streaming inputs, project `ChangeVersion` validation, and element-ownership stability checks.

## Deterministic regression

`SourceHandleRootTransientKnownCountStabilitySmoke` uses counted enumerables whose Count changes immediately after successful `MoveNext()`. Hostile cases restore only if `Current` is read, so rejection must leave `CurrentReads == 0`. Stable counted and pure streaming inputs remain successful.

Run the auto-discovered feature preflight and full deterministic Core smoke suite on the exact candidate SHA. Merge only after current protected `preflight + core` succeed and exact protected-main ancestry is verified.
