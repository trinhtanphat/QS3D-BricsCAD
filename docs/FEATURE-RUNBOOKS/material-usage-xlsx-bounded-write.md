# Material Usage XLSX bounded publication

## Scope

C02 hardening for `MaterialUsageXlsxExporter`. The exporter must not retain a complete hostile worksheet string before package publication.

## Contract

- validate and snapshot rows/provenance before publication;
- stream worksheet XML through strict UTF-8 with a bounded worksheet-entry writer;
- fail closed above 32 MiB per XML entry, 64 MiB aggregate uncompressed XML, or 64 MiB final archive;
- use checked cumulative arithmetic and deterministic ZIP timestamps/order;
- preserve invariant `R` numeric serialization, XML escaping, Count contracts and provenance identity;
- on any bounded-write/package failure preserve an existing destination and remove owned temp artifacts.

## Validation

Run `python scripts/preflight-material-usage-xlsx-bounded-write.py`, focused Material Usage smokes, repository health, Core Release smoke, and protected exact-head `preflight + core` before merge.
