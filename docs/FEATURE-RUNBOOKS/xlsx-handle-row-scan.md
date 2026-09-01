# XLSX Handle bounded worksheet row scan

`XlsxHandleReader.ReadHandleLookup` must not materialize the worksheet's complete row sequence merely to locate one requested row.

The reader scans worksheet rows once, validates every declared row number, retains at most the requested row plus ten preceding header candidates, rejects duplicate requested rows, and continues validation through trailing rows. Existing modern-schema, formula-identity, cell-coordinate and XLSX row-limit checks remain authoritative.

Regression coverage:

```text
python scripts/preflight-xlsx-handle-row-scan.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Runtime classification: Core-only / NOT_APPLICABLE to licensed BricsCAD runtime.
