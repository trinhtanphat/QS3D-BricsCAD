# Measurement work-item coverage CSV bounded publication

## Scope

This carrier bounds `MeasurementWorkItemCoverageCsvExporter` without changing the public CSV schema or semantic provenance contract.

## Publication contract

- File publication and public `ToCsv` use the same strict UTF-8 byte ceiling.
- The UTF-8 BOM is counted against the file-shaped ceiling so `ToCsv` cannot materialize content that the file exporter would reject for size.
- Byte accumulation uses checked arithmetic and fails closed with `InvalidDataException` before the destination is replaced.
- File publication streams rows into an owned temporary file and commits through `AtomicFileCommit.ReplaceWithoutBackup`; failure preserves an existing destination and cleanup removes the owned temporary artifact.
- CSV row order, CRLF line endings, quoted-text behavior, invariant integer/version/timestamp formatting, and optional provenance columns remain deterministic.

## Identity and formula safety

Semantic identity fields keep the existing fail-closed rule for spreadsheet formula prefixes (`=`, `+`, `-`, `@`, including leading whitespace). They are never silently rewritten because that would corrupt measurement/provenance identity. Non-identity CSV text continues to use the existing quoting/formula-escape behavior.

## Regression evidence

`MeasurementWorkItemCoverageCsvBoundedWriteSmoke` covers strict UTF-8/provenance round-trip, formula-prefix rejection, hostile multibyte output overflow, destination-sentinel preservation, owned-temp cleanup, and the same byte ceiling on `ToCsv`.

Required qualification: focused preflight, relevant Measurement/Quantity/Estimating/Commercial/IFC/BCF/CSV-BBS regressions, full Core smoke, repository health/self-review, then exact-head protected CI. CI wait time is not engineering SOW.
