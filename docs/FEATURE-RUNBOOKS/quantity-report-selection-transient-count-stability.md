# Quantity report selection transient Count stability

Canonical carrier: Issue #4963 / Lane-Key `issue-4963`.

Runtime: `NOT_APPLICABLE`. This is deterministic Core reporting/selection integrity and does not establish licensed BricsCAD `LOCAL_PASS`.

## Boundary

`ProjectQuantityReportBuilder.Group/Detail(project, elementIds)` accepts caller-controlled element-id enumerables. Supported `ICollection<string>`, `IReadOnlyCollection<string>` and non-generic `ICollection` Count surfaces are integrity evidence once admitted. Pure streaming enumerables remain supported and one-pass under the independent 10,000-id ceiling.

## Required traversal ordering

For a source with admitted Count evidence, selection traversal must:

1. bind compatible finite Count evidence before enumeration and reject negative, conflicting or oversized Counts;
2. revalidate the admitted Count immediately before caller-controlled `MoveNext()`;
3. execute `MoveNext()`;
4. revalidate Count immediately after `MoveNext()` before trusting its result;
5. when moved, reject known-count N+1 and the hard 10,000 ceiling before reading `Current`;
6. read `Current` exactly once;
7. revalidate Count immediately after `Current` before retaining or semantically validating that id;
8. after terminal `MoveNext`, revalidate Count and require exact observed cardinality before publication.

Existing blank/non-canonical/duplicate/unknown-id rejection, project revision/fresh-instance checks, and case-insensitive canonical lookup remain authoritative.

## Threat model

A hostile counted enumerable may change Count inside `MoveNext()` and restore it from `Current`, or change Count inside `Current` and restore it on the next `MoveNext()`. Admission-plus-final cardinality checks miss both transient windows. The #4963 contract detects the first before `Current` and the second before any returned id enters the selected set.

## Deterministic evidence

`QuantityReportSelectionTransientCountSmoke` covers transient post-`MoveNext` drift, transient post-`Current` drift, a stable counted one-item control with seven Count observations, and a pure-streaming one-pass control. Historical `QuantityReportSelectionCountIntegritySmoke` continues to cover negative/conflicting/oversized Count evidence, over/under-yield, canonical identity semantics, and the 10,001 streaming bound.

Source-safe validation:

```text
python scripts/preflight-quantity-report-selection-transient-count.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Hosted CI/Core/V25 compile results are repository integration evidence only and must not be described as licensed BricsCAD runtime PASS.
