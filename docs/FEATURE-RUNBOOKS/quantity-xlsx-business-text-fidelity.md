# Quantity XLSX identity-text fidelity

Issue #6381 protects semantic grouping/identity/provenance text emitted by `XlsxQuantityExporter` while retaining the established sanitizer for presentation/free-text cells.

## Contract

- Standard `Floor`, `Zone`, and `Category` reject malformed UTF-16 and XML 1.0-illegal code units before filesystem publication.
- ED2 `Floor`, `Zone`, `Category`, `FamilyId`, `Material`, and `DrawingFingerprint` use the same strict identity-text validation.
- Standard `FamilyName` and ED2 presentation `ElementName`/`Note` retain the existing `XlsxXmlText.Escape` replacement behavior required by `XlsxQuantityXmlSanitizationSmoke`.
- Valid supplementary Unicode remains exact.
- Existing 32,767-character cell ceiling, evidence/numeric/provenance semantics, deterministic ZIP ordering/timestamps, and 32/64/64 MiB bounds remain unchanged.

## Validation

```text
python scripts/preflight-quantity-xlsx-business-text-fidelity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Merge only on the exact current PR candidate after required protected `preflight` and `core` are terminal GREEN and freshness/collision checks remain clean.
