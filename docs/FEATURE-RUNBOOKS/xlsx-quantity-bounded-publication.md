# Quantity XLSX bounded publication

## Scope

Carrier #6313 hardens `XlsxQuantityExporter` for both the legacy `Khối lượng` workbook and ED2 `CHI_TIET` / `TONG_HOP` workbooks.

The exporter must never retain a complete worksheet XML string before ZIP publication. Worksheet XML is emitted incrementally through strict UTF-8 into a bounded entry buffer, then copied into the ZIP entry only after its byte budget is satisfied.

## Safety contract

- Maximum worksheet/XML entry: 32 MiB.
- Maximum aggregate uncompressed XML: 64 MiB.
- Maximum final archive: 64 MiB.
- Aggregate arithmetic is checked and failures occur before destination replacement.
- ZIP entry timestamps and entry order remain deterministic.
- Existing destination files survive budget failures and owned temporary files are cleaned up.
- Legacy and ED2 row schema, evidence flags, invariant numeric `R` formatting, XML escaping, and provenance semantics remain unchanged.

## Regression evidence

Run `python scripts/preflight-xlsx-quantity-bounded-write.py` and the Core smoke executable. `XlsxQuantityBoundedWriteSmoke` uses Excel-valid multibyte cell text to exceed the worksheet byte budget and asserts deterministic failure plus destination-sentinel preservation.
