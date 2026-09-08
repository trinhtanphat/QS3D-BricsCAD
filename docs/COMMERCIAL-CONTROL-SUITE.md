# QS3D Commercial Control Suite

Issue: #6124

## Purpose

This slice closes two host-neutral QS commercial gaps without creating parallel pricing engines:

- Tender / procurement lifecycle above the existing `QS3D.Core.Cost.TenderEvaluationService`.
- Cost control / CVR / forecast lifecycle using existing Commercial exact-decimal and revision contracts.

Variation, IPC and Final Account remain owned by `CommercialQsSettlement`.

## Tender / procurement

`TenderProcurementPackage` binds one revisioned package to:

- tender requirements;
- mandatory/optional compliance requirements;
- submitted bids in one canonical currency.

A package must be `Closed` before evaluation. `TenderProcurementService.Evaluate(...)` delegates all commercial bid arithmetic and ranking to `TenderEvaluationService`; the procurement layer only applies compliance gating and selects the lowest-ranked complete bid that passes all mandatory requirements.

`Award(...)` fails closed when the package/evaluation revision is stale, the bid is incomplete, mandatory compliance fails, or the selected bid is not the deterministic current recommendation. `TenderAwardDecision` retains package, bid, evaluated total and both package/award revision provenance.

## Cost control / CVR / forecast

`CommercialControlPeriod` is a revisioned immutable period input containing:

- original budget;
- approved variation net change;
- committed cost;
- actual cost;
- accruals;
- earned value;
- forecast cost to complete.

`CommercialCostControlService.Evaluate(...)` returns:

- revised budget = original budget + approved variation net change;
- cost to date = actual + accrual;
- committed exposure = max(committed cost - cost to date, 0);
- forecast final cost (EAC) = cost to date + forecast cost to complete;
- forecast variance = revised budget - forecast final cost;
- CVR margin = earned value - cost to date.

All additions/subtractions route through the existing exact-decimal commercial arithmetic helpers and fail closed when the exact result cannot be represented.

## Period governance

- `Freeze(...)` changes an open period to `Frozen` under a fresh revision.
- Frozen periods reject forecast mutation.
- `Reopen(...)` requires canonical non-empty audit text and a fresh revision.
- `ReviseForecast(...)` is allowed only on an open period and requires another fresh revision.

The model is immutable: each lifecycle operation returns a new period snapshot instead of mutating the previous one.

## Verification

`TenderProcurementWorkflowSmoke` covers deterministic tender ranking reuse, compliance gating and award rejection.

`CommercialCostControlSmoke` covers revised budget, CVR, EAC, variance, freeze/reopen and revision freshness.

`scripts/preflight-commercial-control-suite.py` guards the architecture so tender arithmetic stays in `TenderEvaluationService` and cost-control arithmetic stays in the Commercial exact-decimal authority.

Hosted CI proves Core/source/build behavior only. It does **not** manufacture licensed BricsCAD V25/V26 interactive runtime evidence; native qualification remains a separate fail-closed requirement.
