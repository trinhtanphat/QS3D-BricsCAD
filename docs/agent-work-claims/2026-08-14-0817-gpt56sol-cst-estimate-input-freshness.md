# Work claim — CST estimate input freshness

- Status: `ACTIVE`
- Agent: `gpt56sol-cst-estimate-freshness-20260814-0817`
- Registered: `2026-08-14T08:17:00+07:00`
- Baseline main SHA: `317036d921834d779d2db6f5b36cf23a5dbbd214`
- Priority: `P1` Wave-2 stale estimate detection on top of completed REV/CST frozen inputs.

## Confirmed gap

The current Cost domain can create an `EstimateLine` from a frozen `MeasurementSnapshot` and `RateBook`, and can project/revision-compare that frozen estimate state, but it has no pure-Core way to tell whether the line's measurement/rate inputs are still current relative to newer canonical snapshots. Callers therefore have to reimplement stale-input checks or silently continue presenting a frozen line without an explicit freshness finding.

## Reserved scope

- `src/QS3D.Core/Cost/EstimateLineFreshness.cs` — new file only
- `tests/QS3D.Core.SmokeTests/EstimateLineFreshnessSmoke.cs` — new focused self-registering smoke only
- this claim file

## Intended contract

Evaluate one existing `EstimateLine` against a current `MeasurementSnapshot` and current `RateBook` without recalculating quantity, commercial adjustment, unit rate, or final amount.

Deterministic findings may include:
- referenced measurement identity is missing;
- referenced measurement trace changed;
- rate-book provenance identity changed;
- the current rate source cannot resolve the line's cost-code/unit/currency at the frozen `RateAsOfUtc`;
- the resolved rate item payload/provenance changed.

The evaluator must preserve exact measurement identity semantics, reuse `MeasurementTrace.Equals` for canonical measurement content, reuse `RateBook.Resolve` at the line's frozen lookup timestamp, and return findings in deterministic order. A cloned current input with the same canonical frozen evidence remains current.

## Excluded scope

- no recalculation of measured/estimating quantity, commercial adjustment, unit rate, amount, revision cost impact, or BQ totals;
- no edits to `EstimateLine.cs`, `RateBook.cs`, `FrozenEstimateProjection.cs`, `EstimateRevisionCostImpact.cs`, MeasurementTrace/Snapshot source, persistence, UI, export, native BricsCAD, Rebar, MAP/QSC, or release automation;
- no GitHub Actions/native qualification.

## Validation plan

Focused smoke source will cover current inputs, missing/changed measurement, changed rate-book provenance, unmatched rate lookup, changed rate item, combined findings, deterministic finding order, and null guards. After each push re-fetch current `main`, inspect exact remote diff/readback, and record only validation actually executed.

## Coordination

Recent CST-01/02/03/04 claims are completed; current recent main activity is Rebar procurement, V25 update UX and completed MAP/IFC lanes. This claim uses new Cost/test files only and does not reserve any existing shared source file.

## Completion condition

Claim-first reservation, minimal freshness evaluator, focused regression, current-main ancestry/readback and an explicit validation boundary are all present on `main`, then this claim is closed `COMPLETED`.
