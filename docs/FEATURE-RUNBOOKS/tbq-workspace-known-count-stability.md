# TBQ workspace known-Count stability

## Scope

This contract covers caller-controlled collection inputs accepted by `TbqProjectWorkspaceState`: bill items, build-up rates, rate references, and BQ library entries. It is deterministic Core cost/TBQ integrity work and requires no licensed BricsCAD runtime.

## Invariants

- All available deterministic Count surfaces (`ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection`) are bound before traversal.
- Negative, conflicting, or oversized known Counts fail before caller-controlled enumeration.
- The first item beyond an admitted known Count fails before unexpected-item validation or materialization.
- Under-yield against the admitted Count fails after traversal and before publication.
- After an exact traversal, deterministic Count surfaces are rebound. Changed, negative, conflicting, or newly oversized evidence fails closed before the workspace is published.
- Stable multi-interface Count evidence remains accepted.
- Inputs without deterministic Count metadata remain supported as pure streaming sources under the independent traversal caps.
- Bill items, build-up rates, and BQ library entries remain bounded to 10,000 entries. Rate references remain bounded to 50,000 entries.
- Existing duplicate/null validation, canonical sorting, rate-reference graph behavior, library semantics, and cost arithmetic are unchanged.

## Deterministic regression matrix

The registered `TbqWorkspaceKnownCountSmoke` covers initial oversize/negative/conflicting metadata, over-yield and under-yield, post-traversal Count drift for all four source categories, post-traversal negative Count, post-traversal multi-interface conflict, stable multi-interface inputs, exact stable counted inputs, and pure streaming controls.

## Validation

Run:

```text
python scripts/preflight-tbq-workspace-known-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The repository Shared CI remains authoritative for exact-head branch and protected PR validation. Protected `preflight` and `core` must both succeed on the current candidate before merge.

## Runtime boundary

Runtime is `NOT_APPLICABLE`. This package changes deterministic Core collection integrity only; hosted CI is sufficient for the source contract and no licensed BricsCAD runtime or `LOCAL_PASS` is claimed.
