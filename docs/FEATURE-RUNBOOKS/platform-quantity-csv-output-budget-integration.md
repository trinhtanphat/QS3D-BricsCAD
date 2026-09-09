# Platform Quantity Schedule CSV output-budget integration

Carrier: #6276  
Lane: C02 Quantity / Estimating / Measurement / CSV / Export

## Integration gap

The parent repository still pins `external/QS3D-Platform` at `5c1b650e475af3c823ae38aaf1a4a85f41985457`, while the qualified upstream bounded CSV implementation merged to Platform `main` as `728aa5779e3b1b0a30dae641359d95688228f50f`.

The upstream implementation bounds total emitted Quantity Schedule CSV data to 16 MiB UTF-8, reserves complete fields before append, accounts for delimiters/CRLF/quotes/formula-neutralizing apostrophes, emits line-ending and quote escaping without whole-field replacement copies, preserves deterministic order/provenance/invariant quantities, and has a hostile cumulative-output regression.

## RED-first parent gate

`scripts/preflight-platform-quantity-csv-output-budget.py` is intentionally introduced before the gitlink mutation. It requires the exact qualified upstream merge SHA and therefore must fail while this branch still inherits the old parent gitlink. A Reservation-v2-clean exact-head RED is required before the pointer is advanced.

## Integration contract

- Advance only `external/QS3D-Platform` to `728aa5779e3b1b0a30dae641359d95688228f50f`.
- Do not absorb a newer moving Platform generation into this carrier.
- Keep the focused preflight as provenance/evidence that the parent actually consumes the qualified upstream generation.
- Run aggregate feature guards and broad Core/submodule validation on exact head.
- Reconcile latest protected `main` non-force if necessary and rerun exact-head CI before expected-head merge.

Runtime classification: `REMOTE_SAFE` Platform/Core integration. No licensed BricsCAD runtime PASS is required or claimed.
