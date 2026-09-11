# Door Opening XLSX bounded publication

Issue: #6309
Lane: C02 Quantity / Export
Runtime: REMOTE_SAFE deterministic Core/export.

## Defect

`DoorOpeningXlsxExporter` admitted deterministic known row counts and protected row/provenance snapshot stability, but the final worksheet path still built the complete 16-column XML document in an unbounded `StringBuilder` before ZIP creation. Excel-valid 32,767-character text across many rows could therefore amplify managed memory before package validation or atomic destination replacement could reject the output.

## Contract

- preserve the existing 1,048,575-row admission ceiling and every known-Count revalidation boundary;
- preserve row snapshot stability, numeric `R` invariant serialization, XML escaping and `ElementIds`/`HostIds`/`SourceHandles` provenance;
- emit worksheet XML through strict UTF-8 into a bounded stream rather than retaining one complete XML string;
- enforce 32 MiB per XML entry, 64 MiB aggregate uncompressed XML and 64 MiB final archive ceilings with checked cumulative arithmetic;
- use deterministic ZIP timestamps/order;
- on any byte-budget failure, leave an existing destination untouched and remove the owned temporary package.

## Deterministic validation

Run:

```text
python scripts/preflight-door-opening-xlsx-bounded-write.py
python scripts/preflight-door-opening-xlsx-count-integrity.py
python scripts/preflight-door-opening-xlsx-snapshot-stability.py
python scripts/preflight-door-opening-xlsx-transient-count-stability.py
python scripts/preflight-door-opening-schedule.py
```

The module-initialized hostile regression uses multibyte text at Excel's cell-character ceiling across enough rows to exceed the worksheet byte budget and proves destination sentinel preservation plus owned-temp cleanup.

Protected exact-head `preflight + core` remains merge authority after current-main reconciliation and Reservation-v2 collision validation. No licensed BricsCAD runtime evidence is required or claimed for this Core/export boundary.