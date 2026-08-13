# Work claim — MAP-03A pure-Core coverage report projection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-map03a-coverage-report-projection-20260813-1705`
- Registered: `2026-08-13T17:05:00+07:00`
- Baseline main SHA: `2544bee6c129e84fad15b044a3b1508a46f1d2ea`
- Priority: `MAP-03 / P1` — expose MAP-02 quantity/work-item coverage as a deterministic detached report model without adding a second readiness engine or entering currently active host UI lanes

## Confirmed gap

MAP-01A and MAP-02A are completed on current `main`. `MeasurementWorkItemCoverageEvaluator` already emits deterministic detached findings with canonical mapping identity, `IsReady`, and explicit `MissingQuantity` / `StaleQuantity` / `UnmappedWorkItem` reasons. Current source/history contains no MAP-03 coverage projection/report model, and MAP-02A explicitly excluded MAP-03 UI/report projection.

## Reserved scope

Add one pure-Core presentation/report projection over existing `MeasurementWorkItemCoverageFinding` values only.

The projection will:

- copy each finding into an immutable/detached report row containing element/category/quantity scalar data, mapping identifiers, `IsReady`, and a read-only copy of issue reasons;
- sort rows deterministically using ordinal identity ordering, independent of input enumeration order and current culture;
- expose total/ready/not-ready counts plus explicit counts for missing quantity, stale quantity and unmapped work item;
- preserve issue-count overlap (for example stale + unmapped contributes to both reason counts) rather than pretending reasons form a partition;
- derive readiness solely from the evaluator-provided `finding.IsReady`; it will not inspect `ProjectState`, `ProjectElement`, geometry, health, quantity dictionaries, rules or renderers;
- fail visibly on null input collection or null finding entries instead of silently dropping data.

## Expected surfaces

- new `src/QS3D.Core/Mapping/MeasurementWorkItemCoverageReport.cs` — detached row/report projection only;
- new `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageReportSmoke.cs` — projection/count/order/detachment regression;
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` — aggregate smoke registration;
- this claim file.

## Excluded scope

- No edits to `MeasurementWorkItemCoverageEvaluator`, `MeasurementWorkItemMappingCatalog`, `ProjectState`, quantity/regeneration/health logic or measurement rules.
- No independent inference of missing classification/rule/geometry/host reasons not currently supplied by MAP-02.
- No WPF/Workspace/BQ window, BricsCAD V25/V26 adapter, DWG table, XLSX, persistence or schema work in this sub-lane.
- No RateBook/Estimate/REV work and no native/local qualification.
- No GitHub Actions dispatch and no BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current `main` immediately after this claim-only commit and recheck recent MAP/coverage claims/commits for overlap.
- Focused smoke will consume real MAP-02 evaluator findings, verify ready/not-ready and overlapping reason counts, mapping identity projection, deterministic ordering under reversed input/culture changes, read-only detached rows/issues, and null-input failure.
- Re-fetch exact source/test/registration blobs from pushed `main` before closeout.
- Managed build/smoke execution is `NOT_RUN` unless a real .NET execution path is available; source inspection is not reported as executable PASS.

## Coordination

- MAP-02A claim is `COMPLETED`; this lane consumes its public finding contract without editing the evaluator.
- MAP-03 host UI/rendering remains intentionally unclaimed so UI agents can work independently after this Core projection exists.
- Current Family Manager / Quantity Summary / runtime-fingerprint UI/native work is explicitly non-overlapping and excluded.
- REV, CST, Curtain and LOCAL/native qualification remain excluded.

## Completion condition

A claim-first deterministic detached coverage report model plus focused registered smoke is present on current `main`, report counts/rows are direct projections of MAP-02 findings with no duplicate readiness logic, remote blobs are verified, and this claim is closed with exact pushed SHAs plus validation actually executed.
