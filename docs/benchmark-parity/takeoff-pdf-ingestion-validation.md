# 2D Takeoff PDF ingestion validation

`Qs2DSheetIngestor.IngestPdf` is the managed admission boundary for PDF drawing sheets used by calibrated 2D takeoff. It now rejects payloads that do not have a standards-shaped `%PDF-x.y` header or that are truncated before the terminal `%%EOF` marker.

The terminal check ignores PDF whitespace after `%%EOF` (NUL, tab, line feed, form feed, carriage return, and space), so ordinary exported PDFs with trailing line endings remain compatible. The change does not render, rewrite, or parse drawing geometry and does not introduce a parallel PDF engine.

This validation protects provenance before scale calibration, markup extraction, revision comparison, Takeoff Package review, classification, formula, inventory, and estimate stages. SHA-256 evidence continues to be calculated over the exact admitted payload, and `PdfPageNumber` behavior is unchanged.

Compatibility boundary: valid existing PDF sheet payloads retain their existing DTOs and evidence identity. Payloads that previously passed solely because they began with `%PDF-` but had a malformed version header or no terminal EOF marker now fail closed with `InvalidOperationException`; callers should surface the source file as invalid/truncated and request a fresh export rather than creating quantity evidence from it.
