# Work claim — Semantic View Floor/Zone filter canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:52:00+07:00`
- Baseline main SHA observed: `8810ff3aaa9b302596feb4bf63dd0ed0226a00dd`
- Priority: P1 — deterministic semantic documentation correctness.

## Confirmed defect

`SemanticViewPlanner.Build()` normalizes and validates an explicit Floor/Zone filter through the planner's canonical reference resolver, but candidate elements are still filtered with raw `x.FloorId` / `x.ZoneId` equality. Existing Floor/Zone mutation semantics intentionally treat trimmed case-insensitive stored relation identity as the same semantic target and preserve padded/case-varied relation strings on no-op assignment. A valid semantic view can therefore resolve its Floor/Zone successfully yet silently omit a semantically matching element whose stored relation text contains padding.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs` — Floor/Zone candidate predicates inside `Build()` only.
- Focused Core smoke regression for padded/case-varied relation identity.
- Isolated smoke registration, focused static preflight and planning note.

## Explicit exclusions

- Completed semantic-view null-reference resolver behavior.
- `BuildCatalog()` capacity/ordering semantics.
- `SemanticViewDefinition` constructor collection materialization.
- Semantic Documentation catalog store/editor/schema.
- Semantic Schedule catalog and native CAD view/sheet materialization.
- BricsCAD V25/V26 runtime qualification.

## Implementation plan

1. Re-fetch moving `main` after this claim and confirm raw Floor/Zone predicates remain.
2. Compare candidate element relation IDs after trimming against normalized Floor/Zone filters with `OrdinalIgnoreCase`.
3. Preserve existing reference validation, category/include/exclude filters, duplicate project-element validation, ordering and read-only behavior.
4. Add a Core smoke where an element stores padded/lowercase FloorId/ZoneId while a canonical semantic view must still select it without touching project state or rewriting raw relations.
5. Add static preflight requiring trimmed relation predicates and rejecting the legacy raw comparison path.
6. Refresh moving `main`, verify no reserved-source overlap, merge a focused PR and close this claim with exact evidence.

## Validation policy

This is pure Core read-only planning behavior. GitHub Actions remain manual-only and are not dispatched. Executable smoke/preflight PASS and licensed BricsCAD runtime PASS will not be claimed without actual execution evidence.
