# Rebar CSV bounded publication

## Scope

`RebarCsvExporter` and `RebarProcurementCsvExporter` must bound accepted CSV output to 16 MiB including the UTF-8 BOM while preserving existing row-count, numeric, semantic-identity, formula-safety, Unicode and atomic-publication contracts.

## Required behavior

- Snapshot/admit the complete row sequence before filesystem mutation. BBS schedule rows retain deep value-stability checks; procurement summaries are immutable and are retained as the admitted snapshot.
- Measure the exact emitted strict-UTF8 representation, including BOM, quoting, doubled quotes, formula escaping, delimiters and CRLF, with checked accumulation. Reject output above 16 MiB with `InvalidDataException`.
- `Export` must not call public `ToCsv` or materialize a whole-file string. After prevalidation it streams the admitted snapshot directly to the owned temporary file and atomically replaces the destination.
- Public `ToCsv` remains supported, but only after the same exact byte-budget validation; therefore its returned string can never exceed the publication contract.
- Malformed UTF-16, formula-leading semantic identities, invalid numerics, row-count drift and oversize payloads must fail before destination replacement; existing destination data and unrelated files remain unchanged.
- Numeric fields retain invariant round-trip (`R`) formatting and deterministic schema/CRLF order.

## Validation

Run:

```powershell
python scripts/preflight-rebar-csv-bounded-publication.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Hosted merge admission additionally requires exact-head protected `preflight` and `core` success, current-main freshness, Reservation-v2 collision cleanliness and zero unresolved review threads.

## Runtime classification

REMOTE_SAFE managed Core/export logic. No licensed BricsCAD runtime claim is required for this carrier.
