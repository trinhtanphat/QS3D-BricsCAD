# HealthSummary known-Count stability

## Scope

This runbook covers deterministic Core diagnostics ingestion in `HealthSummary(IEnumerable<ModelHealthIssue>)`. It does not cover licensed BricsCAD runtime, UI health panels, private DWGs, release publishing, or host process behavior.

## Integrity contract

`HealthSummary` supports pure streaming inputs and collection-backed inputs exposing deterministic `Count` through generic `ICollection<ModelHealthIssue>`, `IReadOnlyCollection<ModelHealthIssue>`, or non-generic `ICollection`.

For deterministic Count evidence, materialization is fail-closed:

1. bind all supported Count surfaces before caller-controlled enumeration;
2. reject negative, conflicting, or greater-than-1,000,000 Count evidence before traversal;
3. reject the first traversal item beyond the admitted Count before retaining it or advancing any later tail;
4. keep the independent 1,000,000-entry ceiling for pure streaming inputs;
5. reject under-yield after traversal;
6. re-read all supported Count surfaces post-traversal and reject drift, negative or conflicting evidence before semantic severity validation or publication.

The admitted Count therefore describes the exact traversal cardinality, not merely a preallocation hint. A hostile counted enumerable cannot replace that evidence during enumeration and still publish a summary.

## Preserved behavior

- input order is preserved;
- valid Error, Warning and Info severity accounting is unchanged;
- `IsHealthy` and `IsReleaseReady` semantics are unchanged;
- null issue and undefined severity rejection remain fail-closed;
- honest multi-interface counted inputs remain supported;
- pure streaming inputs remain supported up to the independent 1,000,000-entry ceiling.

## Deterministic regression

`HealthSummaryKnownCountStabilitySmoke` auto-registers and covers early known-Count overrun with a later throwing tail, post-traversal Count drift across generic/read-only/non-generic surfaces, post-traversal negative evidence, existing under-yield behavior, and stable counted/streaming controls.

`scripts/preflight-health-summary-known-count-stability.py` is auto-discovered by aggregate preflight and pins production, smoke-registration and runbook boundaries.

## Runtime boundary

Runtime is `NOT_APPLICABLE`. Hosted deterministic Core smoke plus protected Shared CI provide the repository-safe acceptance evidence. No licensed BricsCAD, private-DWG or `LOCAL_PASS` claim is applicable.
