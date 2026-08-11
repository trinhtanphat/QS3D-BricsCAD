# Work claim — Semantic Documentation catalog save bounded enumeration

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:45:00+07:00`
- Baseline main SHA observed: `e59594aacb5187d57a527df86aa083836a153b31`
- Priority: P1 — deterministic Core persistence/resource-bound correctness.

## Confirmed defect

`SemanticDocumentationCatalogStore.Save()` materializes the complete `views` and `sheets` enumerables through unbounded `MaterializeViews()` / `MaterializeSheets()` before calling the existing planners. `SemanticViewPlanner.BuildCatalog()` and `SemanticSheetPlanner.BuildCatalog()` each already enforce a 10,000-item catalog capacity during enumeration. The store therefore bypasses those intended resource bounds during its own earlier materialization: a huge or non-terminating lazy source can be consumed without bound before the planner guard is reached.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs` — save-input view/sheet materialization only.
- Focused Core smoke regression for oversize lazy views and oversize lazy sheets.
- Focused static preflight and planning note.

## Explicit exclusions

- `SemanticViewPlanner` / `SemanticSheetPlanner` behavior or capacity values.
- `SemanticViewDefinition` / `SemanticSheetDefinition` constructor collection bounds.
- XML schema, payload format, canonical serialization and editor semantics.
- Native CAD placement, WPF/UI or BricsCAD commands.
- Semantic Schedule catalog work completed separately in PRs #574/#581.
- BricsCAD V25 runtime qualification.

## Implementation plan

1. Re-fetch moving `main` after claim and confirm store materialization is still unbounded.
2. Bound store view materialization at the existing planner contract of 10,000 views; throw the planner-compatible capacity error on the 10,001st yielded item and never request 10,002.
3. Bound store sheet materialization identically at 10,000 sheets.
4. Preserve null-entry errors, planner validation, empty-catalog removal, exact payload no-op semantics, serialization and `ProjectState.Touch()` ordering.
5. Add adversarial Core smoke sources for views and sheets that fail if item 10,002 is requested and prove capacity rejection occurs before project version/metadata mutation.
6. Add a static preflight requiring in-enumeration guards and rejecting unbounded store materializers.
7. Refresh moving `main`, verify no reserved-source overlap, merge only a focused PR, then close this claim with exact evidence.

## Validation policy

This is pure Core persistence behavior. GitHub Actions are manual-only and are not dispatched by this lane. Executable smoke/preflight PASS and licensed BricsCAD V25 runtime PASS will not be claimed without actual execution evidence.
