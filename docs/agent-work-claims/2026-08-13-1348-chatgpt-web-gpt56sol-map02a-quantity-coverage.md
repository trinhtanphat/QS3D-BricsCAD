# Work claim — MAP-02A quantity/work-item coverage evaluator

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-map02a-quantity-coverage-20260813-1348`
- Registered: `2026-08-13T13:48:00+07:00`
- Baseline main SHA: `e1aa3f294fa65441eff5565ebd47e2109ed9d439`
- Priority: `MAP-02 / P0-P1` — derive deterministic quantity/work-item coverage from canonical project quantity state plus the MAP-01A catalog

## Confirmed gap

MAP-01A provided deterministic `(ElementCategory, MeasurementItemId) -> ClassificationId/WorkItemId` resolution with explicit unmapped state. Current source/history contained no `MAP-02` evaluator. `ProjectElement` already owns canonical quantity values and `ElementDirtyFlags.Quantity` stale state; `ProjectQuantityReportBuilder` remains a projection and was not turned into a second readiness engine.

## Implemented scope

Added one pure-Core coverage evaluator that consumes a `ProjectState` plus `MeasurementWorkItemMappingCatalog` and emits deterministic detached findings for current element quantities.

Implemented semantics:

- element with no quantity entries emits exactly one `MissingQuantity` finding;
- each existing quantity is snapshotted/evaluated independently in deterministic element/quantity order;
- `ElementDirtyFlags.Quantity` contributes `StaleQuantity` to each stored quantity;
- MAP-01A resolution contributes `UnmappedWorkItem` when mapping is absent;
- stale + unmapped remains two explicit issues rather than losing one to precedence;
- mapped + finite + non-stale quantity has no issues and `IsReady == true`;
- mapped stale quantities retain canonical mapping identity for diagnosis but are not ready;
- non-finite values injected through the mutable quantity dictionary fail visibly rather than becoming zero/ready;
- null/duplicate/noncanonical element or quantity identity and undefined category corruption fail closed before coverage is returned;
- findings contain detached scalar quantity state and read-only issue lists, not live quantity dictionaries.

## Pushed commits

- Claim-only: `945f71a451748b44dea53ba4a79b1f5ec97d2d7f` — `chore(agent): claim MAP-02A quantity coverage`.
- Production evaluator: `321f1aa1c0064b23d4569fb8d75e53137950568b` — `feat(mapping): add deterministic quantity coverage evaluator`.
- Focused smoke: `2d3539e64be02149cde04e33e0f57c4732a85376` — `test(mapping): cover quantity coverage states`.
- Auto-registration: `5c8176ca72b6475c58cd30d14ad05e19a641d8e6` — `test(mapping): register quantity coverage smoke`.

## Exact source/test surfaces

- new `src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs`;
- new `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageSmoke.cs`;
- new `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageRegistration.cs`;
- this claim file only.

Exact GitHub compare from claim commit to registration commit confirmed those three implementation/test files and no report, persistence, rule, measurement, geometry or native file changes.

## Focused regression coverage committed

- mapped + fresh quantity is ready and exposes canonical MappingId/ClassificationId/WorkItemId;
- fresh but unmapped quantity remains explicitly `UnmappedWorkItem` and not ready;
- stale mapped quantity remains mapped but not ready;
- stale unmapped quantity preserves both `StaleQuantity` and `UnmappedWorkItem`;
- element without quantities exposes `MissingQuantity` without invented quantity/mapping data;
- finding quantity value remains detached after source dictionary mutation;
- deterministic ordering/content survives reversed project insertion and `tr-TR` current culture;
- duplicate case-insensitive element identity, null element, non-finite quantity, padded quantity key and deliberately corrupted undefined category fail closed.

## Excluded scope preserved

- No QSDB/persistence/schema or ProjectState mapping collection; MAP persistence remains a future separately claimed lane because current schema-v3 strict validation/migration requires a coherent versioned change rather than metadata shortcuts.
- No real BOQ/classification codes, rate/estimate work, MAP-03 UI/report projection or changes to `ProjectQuantityReportBuilder`/XLSX/DWG renderers.
- No inferred `missing measurement rule`, geometry validity or host ambiguity; those require explicit canonical inputs from their owning domains.
- No changes to Quantity Rules, MeasurementTrace/Snapshot/Delta/DeltaReason, semantic regenerators, geometry or BricsCAD adapters.
- No second quantity calculation or health engine.
- No GitHub Actions dispatch and no BricsCAD native PASS claim.

## Validation actually executed

- Refreshed current `main` before claim and verified the claim-only commit was current before source work.
- Reconciled current-main deltas; concurrent Measurement Snapshot DeltaReason work remained outside Mapping coverage scope.
- Re-fetched and read back the full production evaluator, focused smoke and ModuleInitializer registration from GitHub after push.
- Exact GitHub compare from `945f71a4...` to `5c8176ca...` confirmed only the three reserved implementation/test files.
- Static nullable/integrity review confirmed nullable mapping/quantity fields are explicit and mutable source dictionaries are snapshotted into scalar/read-only finding data.
- Local executable environment still has no `dotnet`, `csc`, `mcs` or `msbuild`; `.NET` build and smoke execution are `NOT_RUN`.
- GitHub Actions, BricsCAD V25/V26 runtime and licensed/native qualification were not run. No PASS is claimed for unexecuted gates.

## Completion condition

Satisfied for this narrow MAP-02A lane: deterministic canonical coverage findings are present on current `main`, missing/stale/unmapped states remain explicit and composable, ready state requires an existing finite mapped non-stale quantity, report/persistence/calculation ownership remains unchanged, focused smoke is auto-registered, and all unexecuted gates are recorded truthfully.