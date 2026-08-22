# Work claim — CST estimate input freshness

- Status: `COMPLETED`
- Agent: `gpt56sol-cst-estimate-freshness-20260814-0817`
- Registered: `2026-08-14T08:17:00+07:00`
- Baseline main SHA: `317036d921834d779d2db6f5b36cf23a5dbbd214`
- Priority: `P1` Wave-2 stale estimate detection on top of completed REV/CST frozen inputs.

## Confirmed gap

The Cost domain could create an `EstimateLine` from a frozen `MeasurementSnapshot` and `RateBook`, and could project/revision-compare that frozen estimate state, but it had no pure-Core way to tell whether the line's measurement/rate inputs were still current relative to newer canonical snapshots. Callers otherwise had to reimplement stale-input checks or silently continue presenting a frozen line without an explicit freshness finding.

## Reserved scope

- `src/QS3D.Core/Cost/EstimateLineFreshness.cs` — new file
- `tests/QS3D.Core.SmokeTests/EstimateLineFreshnessSmoke.cs` — new focused self-registering smoke
- this claim file

## Implemented contract

`EstimateLineFreshnessEvaluator.Evaluate()` compares one existing frozen estimate line against a current `MeasurementSnapshot` and current `RateBook` without recalculating quantity, commercial adjustment, unit rate, final amount, or BQ totals.

Deterministic findings are emitted for:
- referenced measurement identity missing;
- referenced measurement trace changed;
- rate-book provenance identity changed;
- current rate source unable to resolve the frozen cost-code/unit/currency at the line's `RateAsOfUtc`;
- resolved rate item payload/provenance changed.

The evaluator uses exact ordinal measurement identity, `MeasurementTrace.Equals` for canonical measurement content, `RateBook.Resolve` at the frozen lookup timestamp, case-insensitive `RateItemId` identity consistent with `RateBook` duplicate identity handling, and fixed deterministic finding order. Equivalent cloned measurement/rate inputs remain current.

## Commits

- Claim-only: `5e6c2d9f63d44e3f28b630dd04a92100282ba260`
- Source: `acb3946eedf11b6a13908b02cac1cccd8b066037`
- Focused smoke: `2b653325a1d3ab980cf733217d76d3a96e7b6470`

## Validation actually performed

- Remote source commit readback verified the new evaluator is isolated to `src/QS3D.Core/Cost/EstimateLineFreshness.cs` and does not edit existing Cost/Measurement formulas.
- Remote smoke commit readback verified focused coverage for equivalent current inputs, changed/missing measurement, changed rate-book provenance, unavailable rate, changed rate, combined deterministic findings, RateItem identity casing, and null guards.
- Smoke self-registers through the repository's existing `[ModuleInitializer]` pattern; no shared smoke registration file was modified.
- Current-main lineage check after the smoke commit showed one intervening V25 UpdateCenterWindow-only commit and no changes to the reserved Cost/test files.
- GitHub combined status for smoke SHA reports no attached statuses/checks (`total_count = 0`); no GitHub Actions were dispatched.
- Local/container executable check found `dotnet` is not installed, so managed Core smoke/build execution is `NOT_RUN` and is not claimed as PASS.
- BricsCAD V25/V26 native runtime is outside this pure-Core evaluator and no native PASS is claimed.

## Excluded scope preserved

- no recalculation of measured/estimating quantity, commercial adjustment, unit rate, amount, revision cost impact, or BQ totals;
- no edits to `EstimateLine.cs`, `RateBook.cs`, `FrozenEstimateProjection.cs`, `EstimateRevisionCostImpact.cs`, MeasurementTrace/Snapshot source, persistence, UI, export, native BricsCAD, Rebar, MAP/QSC, or release automation;
- no force-push and no GitHub Actions dispatch.

## Completion

`COMPLETED`: claim-first reservation, pure-Core freshness projection, focused regression, remote readback/lineage verification and explicit validation boundary are all recorded on `main`.
