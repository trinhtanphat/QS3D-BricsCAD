# BBS CSV row snapshot stability

## Scope

This runbook covers the source-safe integrity boundary in `RebarCsvExporter.ToCsv` for caller-owned mutable `RebarScheduleRow` objects. It does not change rebar design, code-compliance, BBS XLSX, native BricsCAD behavior, or release qualification.

## Invariant

BBS CSV output must represent one admitted logical snapshot. The exporter therefore performs the work in this order:

1. bind and continuously re-check any supported known `Count` evidence while traversing the input;
2. capture every admitted source row into an exporter-owned value snapshot and validate the captured numeric row;
3. after traversal, verify the admitted Count again and compare every caller-owned source row with its captured snapshot;
4. only after all row-stability checks succeed, project the captured snapshots into CSV text;
5. `Export` writes that completed text to a temporary file and atomically publishes it.

If a source row changes while later caller-controlled traversal is still occurring, serialization fails closed before CSV projection/publication instead of returning a mixed-time document.

The existing 10,000-row bound, conflicting/negative/changed known-Count rejection, pure streaming support, strict UTF-8 BOM validation, canonical ElementId protection and spreadsheet-formula prefix handling remain unchanged.

This contract protects against mutation exposed through caller-controlled traversal. It is not a substitute for external synchronization when unrelated threads intentionally race mutable row objects after the final stability check.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/BbsCsvCountStabilitySmoke.cs` includes `RowMutationRejectsAfterTraversalBeforeProjection`. A streaming enumerable yields the first row, mutates that already-consumed row before yielding the second, and proves `ToCsv` rejects the drift. Stable known-Count and pure-streaming controls remain in the same smoke.

## Source guard

`scripts/preflight-bbs-csv-row-snapshot-stability.py` pins the critical source shape and order:

`SnapshotRow(sourceRow) -> EnsureRowStable(...) -> CSV StringBuilder projection`

It also requires the deterministic mutation regression and prevents direct projection from `sourceRow`.

## Validation

Source-safe validation requires:

```text
python scripts/preflight-bbs-csv-row-snapshot-stability.py
python scripts/preflight-bbs-csv-count-stability.py
python scripts/preflight-all.py
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Repository Shared CI remains authoritative for the exact pushed candidate. Licensed BricsCAD execution is `NOT_APPLICABLE` for this deterministic Core CSV integrity slice; no `LOCAL_PASS` claim is produced by this work.
