# MEP/TBQ Current-induced Count integrity

Issue: #4902  
Lane-Key: `issue-4902`

## Source contract

`MepTbqProjectionService.BuildReport` accepts counted and pure-streaming quantity-group sources. When a supported Count surface is admitted, its cardinality must remain stable across every caller-controlled traversal boundary before a group enters validation or report-row staging.

For each successful element the bounded sequence is:

1. revalidate admitted Count before `MoveNext()`;
2. call caller-controlled `MoveNext()`;
3. revalidate Count after successful `MoveNext()`;
4. enforce admitted-count overrun and the 10,000-group ceiling;
5. read caller-controlled `Current`;
6. revalidate Count immediately after `Current`;
7. only then validate the group and materialize a `MepTbqReportRow`.

Existing negative/conflicting Count handling, over/under-yield checks, final rebound, deterministic sort, no-overread behavior and pure-streaming support remain unchanged.

## Deterministic validation

Run:

```text
python scripts/preflight-mep-tbq-current-count-integrity.py
python scripts/preflight-mep-tbq-count-bound.py
python scripts/preflight-mep-tbq-transient-known-count-stability.py
python scripts/preflight-mep-tbq-known-count-no-overread.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

`MepTbqCurrentCountIntegritySmoke` uses a hostile `IReadOnlyCollection<MepQuantityGroup>` whose `Current` getter changes Count while returning null. The canonical Count-stability exception must win before ordinary null-group validation/staging. A stable counted control must still produce one report row.

## Runtime boundary

This contract is deterministic Core/MEP commercial projection integrity and does not require licensed BricsCAD execution. Hosted Core/V25 compile checks are build evidence only and do not imply `LOCAL_PASS`.
