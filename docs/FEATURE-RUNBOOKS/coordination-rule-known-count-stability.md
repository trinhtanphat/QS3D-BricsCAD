# Coordination rule known-Count stability

## Scope

`CoordinationRuleProfile` materializes caller-provided rule collections through `CoordinationRuleCollectionContract.MaterializeBounded<T>`. When a source exposes a supported known `Count`, that cardinality is admission evidence and must remain stable throughout caller-controlled enumeration.

The contract applies to generic `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` Count surfaces. Pure streaming `IEnumerable<T>` sources remain one-pass and do not gain extra enumeration.

## Required ordering

For admitted counted sources, each traversal step is fail-closed in this order:

1. rebind the admitted Count before `MoveNext()`;
2. execute caller-controlled `MoveNext()`;
3. rebind Count immediately after `MoveNext()`;
4. if an item exists, reject known-count over-yield and the 10,000-entry ceiling before `Current`;
5. read caller-controlled `Current`;
6. rebind Count immediately after `Current`, before snapshot retention;
7. retain the rule and increment observed cardinality;
8. after traversal, require exact observed cardinality and rebind Count once more.

A changed, unavailable, negative, or conflicting Count fails before inconsistent rule data can be retained. Existing null/duplicate rule validation, deterministic rule resolution, severity/clearance/version provenance, maximum-entry semantics, and streaming behavior remain unchanged.

## Deterministic regression

`CoordinationRuleKnownCountStabilitySmoke` covers:

- Count drift caused inside `MoveNext`, proving failure occurs before `Current`;
- Count drift caused by the `Current` getter while returning a value that would otherwise reach semantic validation;
- a stable counted profile that resolves normally;
- a pure streaming profile that remains supported.

Historical `CoordinationRuleCollectionCountStabilitySmoke`, bound, no-overread, and normal coordination profile tests continue to cover post-traversal drift, over/under-yield, multi-interface Count behavior, maximum bounds and ordinary rule semantics.

## Source-safe validation

Run:

```text
python scripts/preflight-coordination-rule-known-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The auto-discovered preflight pins the production ordering `Count rebound -> MoveNext -> Count rebound -> over-yield guard -> Current -> Count rebound -> snapshot` and the final traversal rebound.

This package is host-neutral Core coordination integrity. Hosted source/Core/V25 compile CI is sufficient for repository integration, but it must never be reported as licensed BricsCAD `LOCAL_PASS` evidence.
