# Work claim — MAP-01A category measurement/work-item mapping contract

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-map01a-category-mapping-20260813-1334`
- Registered: `2026-08-13T13:34:00+07:00`
- Baseline main SHA: `c5cb213d89e615eae9ae4f3213d6d7d09936fe48`
- Priority: `MAP-01 / P0-P1` — establish a minimal deterministic classification/work-item mapping contract without embedding BOQ logic in reports or geometry

## Confirmed gap

Current `QS3D.Core` has no Classification, WorkItem, CostCode or BOQ mapping domain/file in the current Git tree. `ProjectState` owns zones/floors/families/elements/quantity rules/audit/metadata only; `Qs3dCatalog` is a fixed UI-label list; `ProjectQuantityReportBuilder` groups canonical quantities by floor/zone/category/family/material and contains no classification/work-item mapping identity or resolver. Current history/claim checks found no `MAP-01`, classification-mapping or work-item-mapping owner/implementation.

## Reserved scope

Add one pure-Core category-level mapping foundation from `(ElementCategory, MeasurementItemId)` to a stable mapping entry containing `ClassificationId` and `WorkItemId`.

The contract will:

- validate all identifiers as required canonical trimmed tokens and every category as a defined `ElementCategory`;
- expose a stable `MappingId` independent of display/report text;
- freeze caller-owned mapping input into deterministic read-only order;
- fail closed on case-insensitive duplicate `MappingId` values;
- fail closed when two entries target the same category + measurement-item identity case-insensitively instead of choosing one silently;
- resolve category + measurement item deterministically with an explicit `Mapped` / `Unmapped` result;
- preserve the canonical stored entry when lookup casing varies, without inventing a classification/work item for an unmapped measurement.

## Expected surfaces

- new `src/QS3D.Core/Mapping/MeasurementWorkItemMapping.cs` — immutable entry/catalog/resolution contract only;
- new `tests/QS3D.Core.SmokeTests/MeasurementWorkItemMappingSmoke.cs` — focused deterministic identity/ambiguity/unmapped regression;
- new `tests/QS3D.Core.SmokeTests/MeasurementWorkItemMappingRegistration.cs` — ModuleInitializer registration, avoiding the shared smoke-registration file;
- this claim file.

## Excluded scope

- No `ProjectState`, QSDB/persistence/schema/migration changes in this sub-lane; persistence/integration remains a separate MAP-01 sub-lane after the contract settles.
- No project-element-specific override policy, classification standard catalog, real company/BOQ codes, rates/costs, estimate logic or procurement assumptions.
- No MAP-02 coverage evaluator or MAP-03 UI/report projection.
- No changes to `ProjectQuantityReportBuilder`, XLSX/DWG renderers, regenerators, Quantity Rules, MeasurementTrace/Snapshot/Delta, geometry or BricsCAD adapters.
- No second quantity/report engine and no hard-coded mapping in geometry/report code.
- No GitHub Actions and no BricsCAD native PASS claim.

## Validation plan

- Publish this claim alone on current `main`, re-fetch it and recheck new ACTIVE/BLOCKED ownership before source work.
- Focused smoke covers deterministic ordering across caller order/culture, caller-list isolation, case-insensitive lookup with canonical stored identity, explicit unmapped state, duplicate mapping-ID rejection, duplicate category+measurement target rejection, undefined category rejection and blank-token rejection.
- Re-fetch exact implementation diff/files from current `main` after source push.
- Connector-only source inspection is not an executable `.NET` smoke/build run; no managed/native PASS will be claimed unless actually executed.

## Coordination

- The concurrent `REV-02A` claim owns only new Measurement Snapshot Delta files and explicitly excludes mapping; no overlap.
- Existing MTR/Rules/LOCAL/Curtain lanes remain excluded.
- The current report subsystem remains a consumer candidate for later MAP projection and is intentionally not modified here.

## Completion condition

A claim-first pure-Core mapping contract + focused auto-registered smoke is present on current `main`, duplicate/ambiguous mapping fails visibly, unmapped state is explicit, no existing quantity/report engine is modified, and this claim is updated to `COMPLETED` with exact implementation SHA and actual validation evidence.