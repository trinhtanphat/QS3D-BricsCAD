# Quantity XLSX business-text fidelity

Issue #6381 protects caller-supplied business text emitted by `XlsxQuantityExporter`.

## Contract

- Reject malformed UTF-16 before path normalization, directory creation, temp ownership, or ZIP publication.
- Reject XML 1.0-illegal control characters instead of silently replacing them with U+FFFD.
- Preserve valid supplementary Unicode exactly.
- Apply the same validation path to standard and ED2 business fields through `ValidateCellText` and joined text validation.
- Preserve the existing 32,767-character Excel cell ceiling, numeric/evidence/provenance semantics, deterministic package order/timestamps, and 32/64/64 MiB XLSX bounds.
- No licensed BricsCAD runtime evidence is required; this is deterministic Core/export behavior.

## Validation

```text
python scripts/preflight-quantity-xlsx-business-text-fidelity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Merge only on the exact current PR candidate after required protected `preflight` and `core` are terminal GREEN and main freshness/collision checks remain clean.
