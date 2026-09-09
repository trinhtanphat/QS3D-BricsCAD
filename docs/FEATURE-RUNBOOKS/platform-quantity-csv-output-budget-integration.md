# Platform Quantity Schedule CSV output-budget integration

Carrier: #6276 / PR #6277  
Lane: C02 Quantity / Estimating / Measurement / CSV / Export

## Root cause / integration gap

The parent repository was pinned to `external/QS3D-Platform@5c1b650e475af3c823ae38aaf1a4a85f41985457` after the bounded Quantity Schedule CSV implementation had already qualified and merged upstream as exact Platform commit `728aa5779e3b1b0a30dae641359d95688228f50f`. The parent therefore continued consuming the old generation whose CSV exporter could materialize semantically admitted data without a bounded total-output contract.

The qualified upstream generation bounds total emitted Quantity Schedule CSV data to 16 MiB UTF-8, bounds field size, reserves complete encoded fields before append, accounts for delimiters/CRLF/quotes/formula-neutralizing apostrophes, streams through a bounded writer, preserves deterministic order/provenance/invariant quantities, publishes atomically, cleans owned temp files, and covers exact-boundary plus hostile cumulative-output cases.

## RED-first evidence

Test/runbook-only parent head `92f7154de7ea5b76828983a6348c23c9b54b6854` deliberately retained the old gitlink while `scripts/preflight-platform-quantity-csv-output-budget.py` required exact `728aa5779e3b1b0a30dae641359d95688228f50f`.

Shared CI run `34318918051` passed exact candidate binding, Reservation-v2/path collision and generic source guard, then failed at aggregate feature guards. That is the intended parent RED. Downstream Core was not used as evidence for the RED phase.

Upstream Platform PR #299 independently qualified and merged the production implementation as exact commit `728aa5779e3b1b0a30dae641359d95688228f50f`; this parent carrier must consume that exact generation rather than a newer moving Platform head.

## Production integration

Production integration first reconciles from protected parent `main`, preserves the focused guard/runbook, and advances only `external/QS3D-Platform` to `728aa5779e3b1b0a30dae641359d95688228f50f`. A non-force reconciliation must remain behind=0 versus the protected main used to construct the candidate, and the effective feature diff must stay confined to the reserved gitlink/guard/runbook paths.

## Validation / merge contract

- Focused preflight must observe the exact qualified gitlink.
- Reservation-v2/path collision and generic source guard must pass on the exact candidate.
- Aggregate feature guards and applicable broad Core/submodule validation must be terminal green on the exact head.
- PR metadata-only runs do not substitute for a fresh full candidate qualification after source/gitlink/runbook mutation.
- Recheck protected-main freshness immediately before merge; if main advanced, reconcile non-force and rerun exact-head required CI.
- Merge only through the protected PR path with an expected-head guard; never direct-push main or reuse stale GREEN evidence.

Runtime classification: `REMOTE_SAFE` Platform/Core integration. No licensed BricsCAD runtime PASS is required or claimed.
