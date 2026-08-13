# Work claim — MAP-03A pure-Core coverage report projection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-map03a-coverage-report-projection-20260813-1705`
- Registered: `2026-08-13T17:05:00+07:00`
- Baseline main SHA: `2544bee6c129e84fad15b044a3b1508a46f1d2ea`
- Priority: `MAP-03 / P1` — expose MAP-02 quantity/work-item coverage as a deterministic detached report model without adding a second readiness engine or entering currently active host UI lanes

## Confirmed gap

MAP-01A and MAP-02A were already completed. `MeasurementWorkItemCoverageEvaluator` emitted deterministic detached findings with canonical mapping identity, `IsReady`, and explicit `MissingQuantity` / `StaleQuantity` / `UnmappedWorkItem` reasons, while current source/history had no MAP-03 coverage report projection. MAP-02A explicitly excluded this projection layer.

## Implemented scope

Added a pure-Core presentation/report projection over existing `MeasurementWorkItemCoverageFinding` values only.

The projection now:

- copies each finding into a detached report row containing element/category/quantity scalar data, mapping identifiers, evaluator `IsReady`, and a read-only copy of issue reasons;
- sorts rows deterministically using ordinal identity ordering independent of source enumeration order/current culture;
- exposes total/ready/not-ready counts plus missing-quantity, stale-quantity and unmapped-work-item counts;
- preserves overlapping issue counts, so stale + unmapped contributes to both reason counters;
- derives readiness solely from `finding.IsReady` and does not inspect `ProjectState`, `ProjectElement`, geometry, health, quantity dictionaries, measurement rules or renderers;
- fails visibly on null input collection or null finding entries;
- exposes report rows and row issues through read-only detached collections.

## Pushed commits

- Claim-only: `ec9614ac9ab73fc6ecf617b982b182f93a3deb23` — `chore(agent): claim MAP-03A coverage report projection`.
- Core projection: `34f102a913ffddb711efe9c674af6e2b52195ab1` — `feat(mapping): add coverage report projection`.
- Focused smoke: `9e360404d4671987cd68db2a8612d87e1b58632b` — `test(mapping): cover coverage report projection`.
- Claim registration-surface refinement: `620bf2c4fb3127aecede4755732c8dffae123e8e` — `chore(agent): refine MAP-03A registration surface`.
- Smoke registration: `911f084c346f65f5c4f4981cecad59d8e87a726a` — `test(mapping): register coverage report smoke`.

## Exact remote surfaces verified

- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverageReport.cs` — remote blob `5204de4add446659d3da53ffac7d1f5d68cd6f3e`.
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageReportSmoke.cs` — remote blob `92f67f9bab9678e4c104a8c7583daef8501f25a2`.
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageRegistration.cs` — remote blob `699e5a08034685fb1fd1bacfde916c3240dd0e70`.

Registration refinement: the initial claim named the aggregate `SmokeTestRegistration.cs` before the current MAP-02 registration surface was re-fetched. Current `main` already had dedicated `MeasurementWorkItemCoverageRegistration.cs`, so the claim was refined before registration write to reuse that narrower Mapping surface. The aggregate runner remained untouched.

## Focused regression coverage committed

- report counts reconcile to five real MAP-02 evaluator findings: one ready, four not-ready, one missing quantity, two stale and two unmapped;
- mapped/fresh row preserves quantity value plus MappingId / ClassificationId / WorkItemId;
- stale + unmapped row preserves both issue reasons and does not invent mapping identity;
- missing-quantity row preserves missing scalar state rather than inventing zero;
- row ordering/content remains stable under reversed finding input and `tr-TR` / `vi-VN` current culture changes;
- report remains detached after later project quantity/stale mutation;
- report row and issue collections are read-only;
- null collection/null finding fail visibly; empty input remains a valid zero-count report.

## Excluded scope preserved

- No edits to `MeasurementWorkItemCoverageEvaluator`, `MeasurementWorkItemMappingCatalog`, `ProjectState`, quantity/regeneration/health logic or measurement rules.
- No independent inference of missing classification/rule/geometry/host reasons not supplied by MAP-02.
- No WPF/Workspace/BQ window, BricsCAD V25/V26 adapter, DWG table, XLSX, persistence or schema work.
- No RateBook/Estimate/REV work and no native/local qualification.
- No GitHub Actions dispatch and no BricsCAD runtime PASS claim.

## Validation actually executed

- Refreshed current `main`, recent Mapping/coverage history and coordination/workstream/product-boundary docs before claim/source work.
- Published the claim alone, then verified the claim commit was the merge-base/ancestor of current `main`; concurrent post-claim changes were Curtain/Workspace/UI/native and did not touch Mapping/report scope.
- Re-fetched and inspected the exact production projection, focused smoke and Mapping registration from remote `main` after push.
- Compared claim commit to later current-main lineage; the only Mapping report surfaces were this lane's three reserved files, while concurrent MTR/Curtain/UI changes remained non-overlapping.
- Performed static C# contract review for nullable/read-only/deterministic projection behavior.
- Local toolchain probe found no `dotnet`, `csc`, `mcs`, `msbuild` or `xbuild`; managed build and smoke execution are therefore `NOT_RUN`.
- GitHub Actions were not dispatched. BricsCAD V25/V26 runtime/native qualification was not run. No PASS is claimed for unexecuted gates.

## Coordination

- MAP-02A remains `COMPLETED`; this lane consumes its public finding contract without editing the evaluator.
- MAP-03 host UI/rendering remains unclaimed by this completed Core sub-lane and can be taken separately.
- Concurrent Family Manager / Quantity Summary / runtime-fingerprint / Curtain / MTR work remained outside this scope.
- REV, CST and LOCAL/native qualification remain excluded.

## Completion condition

Satisfied for MAP-03A: a claim-first deterministic detached coverage report model plus focused registered smoke is present on current `main`; counts/rows are direct projections of MAP-02 findings with no duplicate readiness logic; exact remote blobs were verified; and all executable/native gates not actually run are recorded as `NOT_RUN` rather than PASS.
