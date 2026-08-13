# Work claim — MAP-01A category measurement/work-item mapping contract

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-map01a-category-mapping-20260813-1334`
- Registered: `2026-08-13T13:34:00+07:00`
- Baseline main SHA: `c5cb213d89e615eae9ae4f3213d6d7d09936fe48`
- Priority: `MAP-01 / P0-P1` — establish a minimal deterministic classification/work-item mapping contract without embedding BOQ logic in reports or geometry

## Confirmed gap

At claim time `QS3D.Core` had no Classification, WorkItem, CostCode or BOQ mapping domain/file in the current Git tree. `ProjectState` owned zones/floors/families/elements/quantity rules/audit/metadata only; `Qs3dCatalog` was a fixed UI-label list; `ProjectQuantityReportBuilder` grouped canonical quantities by floor/zone/category/family/material and contained no classification/work-item mapping identity or resolver. Current history/claim checks found no `MAP-01`, classification-mapping or work-item-mapping owner/implementation.

## Implemented scope

Added one pure-Core category-level mapping foundation from `(ElementCategory, MeasurementItemId)` to a stable mapping entry containing `ClassificationId` and `WorkItemId`.

The contract now:

- rejects undefined `ElementCategory`, blank identifiers, leading/trailing whitespace and control characters;
- exposes a stable `MappingId` independent of display/report text;
- snapshots caller-owned mapping enumeration into a detached deterministic read-only list;
- rejects case-insensitive duplicate `MappingId` values;
- rejects two entries targeting the same category + measurement-item identity case-insensitively instead of choosing one silently;
- resolves category + measurement item case-insensitively with explicit `Mapped` / `Unmapped` state;
- returns the canonical stored mapping/measurement-item spelling when a lookup uses different casing and never invents classification/work-item identity for an unmapped measurement.

## Pushed commits

- Claim-only commit: `fc73b8058b68d87d18729a4a38277fc2c2c86f5d` — `chore(agent): claim MAP-01A category mapping contract`.
- Production contract: `165212d055e93a7177982369bcfd9e06d5944136` — `feat(mapping): add deterministic measurement work-item catalog`; exact GitHub commit readback shows only new `src/QS3D.Core/Mapping/MeasurementWorkItemMapping.cs`.
- Focused smoke: `62855dc32a4d9580da807a055a7dad7ad43c404e` — `test(mapping): cover deterministic work-item catalog`; exact GitHub commit readback shows only new `tests/QS3D.Core.SmokeTests/MeasurementWorkItemMappingSmoke.cs`.
- Auto-registration: `4b85f605b390044058f6be9ffdee93c03b640408` — `test(mapping): register work-item catalog smoke`; adds only `MeasurementWorkItemMappingRegistration.cs`.
- Nullable smoke correction: `f48999be1ac8f36a467305d71e895a2c4e2d8b63` — `fix(mapping): make mapping smoke nullable-correct`; captures the mapped entry into a non-null local before dereference under repository nullable/warnings-as-errors policy.

## Focused regression coverage committed

- deterministic ordering independent of caller enumeration order and `tr-TR` current culture;
- detached catalog after caller list mutation;
- case-insensitive lookup returning canonical stored mapping identity;
- explicit unmapped result with no invented mapping;
- duplicate mapping-ID rejection;
- duplicate category + measurement-item target rejection;
- null collection/null entry rejection;
- undefined category, blank token, padded token and control-character rejection;
- ModuleInitializer registration without touching the shared `SmokeTestRegistration.cs` surface.

## Excluded scope preserved

- No `ProjectState`, QSDB/persistence/schema/migration changes; persistence/integration remains a separate MAP-01 sub-lane.
- No project-element-specific override policy, classification standard catalog, real company/BOQ codes, rates/costs, estimate logic or procurement assumptions.
- No MAP-02 coverage evaluator or MAP-03 UI/report projection.
- No changes to `ProjectQuantityReportBuilder`, XLSX/DWG renderers, regenerators, Quantity Rules, MeasurementTrace/Snapshot/Delta, geometry or BricsCAD adapters.
- No second quantity/report engine and no hard-coded mapping in geometry/report code.
- No GitHub Actions dispatch and no BricsCAD native PASS claim.

## Validation actually executed

- Refreshed/reconciled `main` repeatedly before claim, after claim and around each write; concurrent REV-02A and Curtain P11 changes were inspected and remained outside mapping scope.
- Verified claim commit ancestry on current `main` and exact claim-only diff.
- Re-fetched current production mapping source, smoke and ModuleInitializer registration from GitHub after push.
- Re-fetched exact production/smoke commit diffs and verified their file scope.
- Re-read `Directory.Build.props` nullable/warnings-as-errors policy earlier in the session and corrected the discovered smoke nullable dereference before closeout.
- Local executable tool probe found no `dotnet`, `csc`, `mcs` or `msbuild` in this environment. Therefore `.NET build`, smoke execution and managed timing are `NOT_RUN`.
- GitHub Actions, BricsCAD V25/V26 runtime and licensed/native qualification were not run. No PASS is claimed for any unexecuted gate.

## Completion condition

Satisfied for this narrow MAP-01A contract foundation: deterministic mapping identity/resolution is on current `main`, ambiguity and malformed identity fail visibly, unmapped state is explicit, focused regression source is auto-registered, existing quantity/report engines remain untouched, and remaining persistence/project integration/coverage work is explicitly left for later separately claimed MAP lanes.