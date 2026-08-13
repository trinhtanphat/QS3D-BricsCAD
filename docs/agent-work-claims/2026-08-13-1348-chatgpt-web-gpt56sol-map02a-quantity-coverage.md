# Work claim — MAP-02A quantity/work-item coverage evaluator

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-map02a-quantity-coverage-20260813-1348`
- Registered: `2026-08-13T13:48:00+07:00`
- Baseline main SHA: `e1aa3f294fa65441eff5565ebd47e2109ed9d439`
- Priority: `MAP-02 / P0-P1` — derive deterministic quantity/work-item coverage from canonical project quantity state plus the MAP-01A catalog

## Confirmed gap

MAP-01A now provides deterministic `(ElementCategory, MeasurementItemId) -> ClassificationId/WorkItemId` resolution with explicit unmapped state. Current source/history contains no `MAP-02` evaluator. `ProjectElement` already owns canonical quantity values and `ElementDirtyFlags.Quantity` stale state; `ProjectQuantityReportBuilder` is a projection and must not become a second readiness engine.

## Reserved scope

Add one pure-Core coverage evaluator that consumes a `ProjectState` plus `MeasurementWorkItemMappingCatalog` and emits deterministic detached findings for current element quantities.

For this sub-lane:

- each element with no quantity entries emits one explicit `MissingQuantity` finding;
- each existing quantity is evaluated independently in deterministic element/quantity order;
- `ElementDirtyFlags.Quantity` contributes `StaleQuantity` to every currently stored quantity without treating it as ready;
- MAP-01A resolution contributes `UnmappedWorkItem` when no mapping exists;
- a stale quantity may also be unmapped, so findings carry multiple issue reasons rather than hiding one behind precedence;
- an existing mapped, non-stale quantity with a finite value is `Ready` and exposes the canonical stored mapping entry;
- non-finite values injected through the public mutable quantity dictionary fail visibly instead of being reported as ready/zero;
- duplicate/blank/non-canonical element identities and undefined categories fail closed before coverage is returned.

## Expected surfaces

- new `src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs` — finding/issue/evaluator contract only;
- new `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageSmoke.cs` — focused deterministic/stale/unmapped/integrity regression;
- new `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageRegistration.cs` — ModuleInitializer registration;
- this claim file.

## Excluded scope

- No QSDB/persistence/schema, ProjectState mapping collection, real BOQ/classification codes or rate/estimate work.
- No MAP-03 UI/report projection and no changes to `ProjectQuantityReportBuilder`/XLSX/DWG renderers.
- No attempt to infer `missing measurement rule`, geometry validity or host ambiguity in this narrow lane; those require explicit canonical rule/health inputs rather than guesses.
- No changes to Quantity Rules, MeasurementTrace/Snapshot/Delta, semantic regenerators, geometry or BricsCAD adapters.
- No second quantity calculation or health engine; evaluator observes existing quantity/stale/mapping state only.
- No GitHub Actions and no BricsCAD native PASS claim.

## Validation plan

- Re-fetch current `main` after this claim-only commit and recheck claim overlap before source changes.
- Smoke covers mapped-ready, unmapped-ready-input, stale+mapped, stale+unmapped, missing quantity, deterministic ordering/culture independence, detached finding data, duplicate/noncanonical element identity, undefined category and non-finite quantity corruption.
- Re-fetch exact implementation commits/files from GitHub before closeout.
- Connector-only source inspection is not an executable `.NET` smoke/build run; unexecuted gates remain `NOT_RUN`.

## Coordination

- MAP-01A is `COMPLETED`; this lane consumes its public catalog without editing it.
- REV-02/03 Measurement Snapshot Delta/Reason files are separate and remain excluded.
- Current LOCAL/Curtain/MTR/Rules ownership remains excluded.

## Completion condition

A claim-first deterministic coverage evaluator + auto-registered smoke is present on current `main`, missing/stale/unmapped states are explicit without silently becoming zero/ready, existing reports remain projections, and the claim is closed with exact pushed SHAs plus actual validation evidence.