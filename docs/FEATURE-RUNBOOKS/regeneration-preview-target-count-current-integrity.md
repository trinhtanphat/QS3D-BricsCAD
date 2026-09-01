# Regeneration preview target known-Count / Current integrity

## Purpose

`RegenerationPreviewService.PreviewSubset` accepts caller-controlled `IEnumerable<string>` target ids. A supported known `Count` surface is collection-identity evidence, not merely an optimization. Once admitted, that exact Count contract must remain stable throughout target materialization and must be checked before caller `Current` can expose an item under stale cardinality evidence.

## Protected contract

For counted inputs, `CanonicalPreviewTargets` snapshots all supported Count surfaces (`ICollection<string>`, `IReadOnlyCollection<string>`, non-generic `ICollection`) and rejects negative or conflicting observations. Traversal is explicit and ordered as:

1. rebind the admitted Count before `MoveNext()`;
2. invoke `MoveNext()`;
3. after a successful move, rebind Count again;
4. enforce the admitted known-Count overrun bound;
5. enforce the project-element-count bound;
6. only then read `Current` and validate canonical target identity;
7. after traversal, rebind Count and require exact under-yield/final cardinality parity.

A transient growth, shrink, negative Count or cross-interface conflict caused by `MoveNext()` must fail before the affected `Current` is read. A source that reports Count=N but yields N+1 entries must expose exactly N `Current` values. Pure streaming sources without supported Count surfaces remain accepted and are still bounded by the project element count.

## Preserved behavior

The hardening does not change target canonical spelling, case-insensitive duplicate rejection, deterministic target sorting, project ownership/element-state freshness, detached preview semantics, guarded apply re-preview, health gating, or rollback behavior. It also does not alter `RegenerationEngine`; execution-side target enumeration has its own independent integrity contract.

## Deterministic evidence

`RegenerationPreviewTargetCountCurrentIntegritySmoke` covers known-count overrun ordering, MoveNext-induced transient growth/shrink/negative/conflicting Count observations, stable counted input, deterministic sort, and pure streaming input. Hostile transient cases assert zero `Current` reads for the affected item.

`scripts/preflight-regeneration-preview-target-count-current-integrity.py` is auto-discovered by the aggregate feature guard and pins the traversal ordering plus the regression surface.

## Runtime boundary

This is deterministic Core/model-lifecycle correctness. No licensed BricsCAD/private-DWG runtime is needed and no `LOCAL_PASS` should be claimed from this package.
