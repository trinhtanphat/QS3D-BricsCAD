# Grid naming Current-induced Count integrity

Issue: #4900  
Lane-Key: `issue-4900`

## Source contract

`GridNamingService.Renumber` may accept a counted or pure-streaming target source. When a supported Count surface is admitted, the same cardinality must remain stable for every caller-controlled traversal boundary before any target ID enters semantic validation or staging.

For each successful element the bounded sequence is:

1. revalidate admitted Count before `MoveNext()`;
2. call caller-controlled `MoveNext()`;
3. revalidate Count after successful `MoveNext()`;
4. enforce admitted-count overrun and the 2,000-element ceiling;
5. read caller-controlled `Current`;
6. revalidate Count immediately after `Current`;
7. only then validate and stage the target ID.

Project `ChangeVersion`, negative/conflicting Count surfaces, under-yield/post-traversal drift, duplicate IDs, original target identity and atomic project mutation rules remain unchanged. Pure-streaming inputs remain supported without synthetic Count reads.

## Deterministic validation

Run:

```text
python scripts/preflight-grid-naming-current-count-integrity.py
python scripts/preflight-grid-naming-known-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

`GridNamingCurrentCountIntegritySmoke` uses a hostile `IReadOnlyCollection<string>` whose enumerator mutates Count from the `Current` getter while returning a value that would otherwise fail ordinary value validation. The canonical Count-stability exception must win before value validation/staging and the project/Grid must remain untouched. A stable counted control must still renumber successfully.

## Runtime boundary

This contract is deterministic Core/domain integrity and does not require licensed BricsCAD execution. Hosted Core/V25 compile checks are build evidence only; no licensed `LOCAL_PASS` claim is implied.
