# 2D takeoff PDF EOF-line admission

## Benchmark gap

The 2D sheet ingestor already validates supported PDF versions, header line termination and a terminal `%%EOF` token. The remaining boundary gap was that `%%EOF` could be attached directly to preceding data on the same line and still be admitted as sheet evidence.

## Compatibility rule

PDF ingestion now requires the terminal `%%EOF` marker to begin immediately after a PDF line terminator. LF, CR and CRLF layouts remain supported, and trailing PDF whitespace after `%%EOF` remains accepted. Inline or space-indented terminal markers fail closed before a `DrawingSheet2D` or SHA-256 evidence record is published.

No public ingestion API or downstream calibration, markup, zone/layer, revision, quantity, classification, formula, inventory, snapshot or estimate contract changes.

## Workflow evidence

The hardened admission boundary remains upstream of the benchmark workflow:

`Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate`

A malformed sheet therefore cannot enter package evidence and later appear as trusted quantity or estimate provenance. Valid PDF sheets retain the same source reference, calibration, page number and SHA-256 evidence semantics.

## Validation

`Qs2DSheetIngestionPdfHeaderSmoke` covers a canonical object payload with terminal EOF plus trailing whitespace, and rejects inline and space-indented EOF forms. `scripts/preflight-2d-takeoff-pdf-eof-line.py` checks that the production guard is present after exact marker validation and that the deterministic smoke remains registered.
