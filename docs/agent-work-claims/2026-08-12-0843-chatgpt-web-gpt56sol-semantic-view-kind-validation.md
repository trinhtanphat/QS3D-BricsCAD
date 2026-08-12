# Work claim — Semantic View kind validation

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:43:00+07:00`
- Baseline main SHA observed: `dab99a78ee217a1b552cef4161caac191fc85557`
- Priority: P1 — fail-closed semantic documentation input integrity

## Confirmed defect

`SemanticViewPlanner.Build(...)` validates IDs, references, categories and filter integrity but copies `SemanticViewDefinition.Kind` directly into `SemanticViewPlan`. Because .NET enums may carry undefined integral values, a malformed value such as `(SemanticViewKind)999` can enter a semantic view plan instead of failing closed.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs` enum-kind validation only
- focused Core regression/preflight for undefined `SemanticViewKind`
- this claim file

## Contract

1. `Build(...)` rejects any undefined `SemanticViewKind` before returning a plan.
2. All currently defined semantic view kinds remain accepted unchanged.
3. Existing ID/reference/filter/category/catalog behavior remains unchanged.
4. No persisted token format, catalog store/editor behavior, native CAD behavior, or issue #77 completion claim changes.

## Non-overlap

- Do not alter `SemanticDocumentationCatalogStore`, nested/root XML cardinality, native MLeader/TableStyle/Layout/Viewport/PaperSpace paths, licensing, regeneration, XLSX/BOM/interchange/health lanes.
- No GitHub Actions dispatch or release publication.

## Closure

Claim first, exact source re-fetch on moving `main`, minimal fail-closed validation, focused regression using an undefined enum value plus defined-value coverage where existing test surfaces allow it, ancestry/readback verification, and truthful closure without unexecuted CI or BricsCAD V25 runtime PASS claims.
