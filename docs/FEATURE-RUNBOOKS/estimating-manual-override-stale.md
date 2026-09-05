# Estimating stale manual override admission

## Defect

A manual rate override is a commercial mutation that assumes the estimating line is current. `ApplyManualRateOverride` historically rejected blocked rows but admitted rows already marked stale, even though bulk rate assignment treats stale rows as non-committable. That allowed an override and audit event to be created against an out-of-date quantity/source generation while the resulting line remained `Stale`.

## Contract

- require an existing referenced/base rate before a manual override;
- reject blocked lines before replacement or audit mutation;
- reject stale lines before replacement or audit mutation;
- preserve the original portfolio and audit log on rejection;
- preserve current-line manual override behavior and `rate-override-created` audit semantics;
- do not turn stale rows current as a side effect of rate editing; the quantity/source freshness workflow remains authoritative.

## Validation

`EstimatingManualOverrideStaleSmoke` exercises failure atomicity for a stale row and a valid-current control. `preflight-estimating-manual-override-stale.py` pins admission ordering so stale/blocked checks cannot drift behind replacement or audit publication.

Runtime classification: `REMOTE_SAFE / NOT_APPLICABLE`; this is deterministic managed Core commercial correctness and does not require licensed BricsCAD execution.
