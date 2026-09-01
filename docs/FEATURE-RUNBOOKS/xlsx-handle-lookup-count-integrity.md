# XLSX handle lookup known-Count integrity

## Scope

This runbook covers deterministic Core validation for `XlsxHandleLookupResult` identity materialization. It does not require BricsCAD or licensed runtime evidence.

## Contract

`handles` and `elementIds` accept stable counted sources and pure streaming enumerables. When generic, read-only, or non-generic Count evidence is available, the admitted Count must be non-negative, mutually consistent, at most 16,384, stable around caller-controlled `MoveNext` and `Current`, and equal to the number of enumerated identity values. The streaming ceiling remains 16,384 observed values.

The traversal rejects a known over-yield before reading an unexpected `Current`, rejects terminal under-yield, and preserves trimming plus case-insensitive deduplication for accepted values.

## Deterministic validation

Run:

```powershell
python scripts/preflight-xlsx-handle-lookup-count-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Shared CI auto-discovers the focused preflight and runs the Core smoke suite. No `LOCAL_PASS` claim is applicable to this package.
