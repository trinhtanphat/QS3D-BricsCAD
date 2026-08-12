# Work claim — Semantic View Floor/Zone filter canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:52:00+07:00`
- Completed: `2026-08-12T00:56:00+07:00`
- Baseline main SHA observed: `8810ff3aaa9b302596feb4bf63dd0ed0226a00dd`
- Claim commit: `20cccc7f5c47dbede0517409d9e0b2d3a3586d76`
- PR: `#593`
- Squash merge on `main`: `a858b20621826eae1e47cf86be88d01740c2efb6`
- Priority: P1 — deterministic semantic documentation correctness.

## Defect closed

`SemanticViewPlanner.Build()` normalized and validated an explicit Floor/Zone filter through the planner's canonical reference resolver but previously filtered candidate elements with raw `x.FloorId` / `x.ZoneId` equality. Existing Floor/Zone mutation semantics intentionally preserve padded/case-varied same-target relation strings on no-op assignment, so a valid semantic view could silently omit a semantically matching element solely because its stored relation text contained padding.

## Implemented

- Floor candidate filtering now compares `(x.FloorId ?? string.Empty).Trim()` with the normalized `floorId` using `OrdinalIgnoreCase`.
- Zone candidate filtering now compares `(x.ZoneId ?? string.Empty).Trim()` with the normalized `zoneId` using `OrdinalIgnoreCase`.
- Existing null-safe canonical Floor/Zone reference validation remains unchanged.
- Category/include/exclude filters, unique semantic element validation and deterministic selected-id ordering remain unchanged.
- Planning remains read-only: no `ProjectState.Touch()` and no raw relation rewrite.
- Added focused Core smoke, isolated module registration, static preflight and planning documentation.

## Validation evidence

- Post-claim source re-fetched from `0583ae9499caad16844fed00d54f85e66b54be04` confirmed both raw predicates remained before implementation.
- PR #593 changed exactly five files; production source changed only two predicates (+2/-2).
- Moving-main comparison found 20 concurrent commits after branch baseline with zero overlap in `SemanticViewPlanner.cs` or the lane's new files.
- Squash merge used expected head `fb88b9f7e471fcfbbfc30b00a106ddaab293b811` and succeeded as `a858b20621826eae1e47cf86be88d01740c2efb6`.
- GitHub Actions were not dispatched because repository policy is manual-only.
- Executable smoke/preflight PASS and licensed BricsCAD runtime PASS are not claimed from this remote environment.

## Explicit exclusions

- Completed semantic-view null-reference resolver behavior.
- `BuildCatalog()` capacity/ordering semantics.
- `SemanticViewDefinition` constructor collection materialization.
- Semantic Documentation catalog store/editor/schema.
- Semantic Schedule catalog and native CAD view/sheet materialization.
- BricsCAD V25/V26 runtime qualification.
