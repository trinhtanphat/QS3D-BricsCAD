# BCF deterministic Count stability

Issue #4392 / Lane-Key `issue-4392` hardens the shared BCF package materializer used by topics, viewpoints, comments, and viewpoint components.

## Contract

For inputs exposing deterministic `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection` Count evidence, BCF materialization uses a two-phase contract:

1. bind and validate supported Count surfaces before caller-controlled enumeration;
2. preserve the existing bounded traversal and #4349 corroborated early-overrun precedence;
3. reject under-yield against the admission Count exactly as before;
4. only after an otherwise exact traversal, re-bind the supported deterministic Count surfaces before sorting or publishing the immutable BCF domain snapshot;
5. fail closed if the post-traversal Count is negative, conflicting, oversized, or differs from the admission Count/surface evidence.

Pure streaming `IEnumerable<T>` inputs continue to use the independent per-level maxima without acquiring a synthetic Count contract. Stable counted inputs preserve canonical ordering, duplicate/reference validation, identity and XML/package semantics.

## Deterministic regression

`BcfIssueExchangeKnownCountStabilitySmoke` mutates Count metadata only when enumeration completes and proves fail-closed behavior independently for topics, viewpoints, comments, and components. It also covers post-traversal negative/conflicting Count evidence plus stable counted and pure-streaming controls.

`scripts/preflight-bcf-known-count-stability.py` is auto-discovered by the aggregate feature guard and locks the production rebind, all four collection levels, #4349 compatibility, regression coverage, and this runbook.

## Validation boundary

This lane is deterministic Core/export integrity work. Required remote evidence is exact-head shared CI followed by strict-current protected PR `preflight` + `core`. Licensed BricsCAD, private DWG, signing, package publication, and `LOCAL_PASS` are not applicable and must not be inferred from hosted CI.
