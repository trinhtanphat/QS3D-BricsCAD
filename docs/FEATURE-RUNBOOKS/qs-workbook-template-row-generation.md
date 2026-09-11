# QS Workbook Template Row Generation Fence

## Scope

This contract covers `QsWorkbookTemplateExporter` quantity-row publication into customer XLSX templates. It is a managed Core/Export boundary and does not claim licensed BricsCAD runtime coverage.

## Safety contract

- Admit the source `IReadOnlyList<QuantityReportRow>.Count` once for snapshot traversal.
- Fail closed if collection cardinality changes during capture or replay.
- Deep-copy semantic text, numeric quantities, evidence flags, drawing fingerprint, Element IDs and CAD handles before any destination directory/temp-file publication.
- Replay the admitted row generation and reject scalar, numeric, evidence, or provenance drift before using the detached snapshot.
- Preserve existing Excel row limits, mapping/formula guards, bounded package/XML processing, package validation and atomic destination replacement.
- A rejected generation must leave an existing destination byte-for-byte unchanged.

## Verification

Run:

```powershell
python scripts/preflight-qs-workbook-template-row-generation.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The deterministic smoke uses hostile `IReadOnlyList` implementations to force Count drift and row scalar/provenance drift. Both must fail closed before publication.
