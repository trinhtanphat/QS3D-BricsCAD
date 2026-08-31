# BBS CSV Current-induced Count stability

## Scope

`RebarCsvExporter.ToCsv` accepts caller-provided `IEnumerable<RebarScheduleRow>` input. Collection Count is treated as integrity evidence and must remain stable across every caller-controlled enumerator boundary, including the `Current` getter itself.

## Contract

- Bind available collection Count evidence before traversal.
- Revalidate Count before and after `MoveNext()`.
- Read `enumerator.Current` exactly once for each admitted row.
- Revalidate Count immediately after `Current` and before null-row semantics, row-property snapshotting, validation, or acceptance.
- Preserve the 10,000-row ceiling and existing Count over/under-yield diagnostics.
- Preserve post-enumeration row-stability verification and CSV semantic identity/formula safety.

## Deterministic regression

`RebarCsvCurrentCountSmoke` supplies a counted enumerable whose `Current` getter changes Count from 1 to 2 and returns null. The required result is the Count-integrity diagnostic, not the null-row diagnostic, proving Count drift is rejected before semantic handling of the returned row. A stable counted control proves exactly one `Current` read and ordinary CSV output.

The auto-discovered `scripts/preflight-rebar-csv-current-count-stability.py` pins the single-Current and post-Current rebound ordering.

## Validation

```text
python scripts/preflight-rebar-csv-current-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Runtime classification: **NOT_APPLICABLE**. This is deterministic Core export-integrity validation and does not claim licensed BricsCAD runtime evidence.
