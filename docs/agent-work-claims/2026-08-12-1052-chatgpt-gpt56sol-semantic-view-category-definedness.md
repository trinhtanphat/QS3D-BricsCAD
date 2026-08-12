# Work claim — Semantic View category definedness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-semantic-view-category-definedness`
- Registered: `2026-08-12T10:52:00+07:00`
- Last Updated: `2026-08-12T10:52:00+07:00`
- Baseline main SHA: `84f391e023b08fce08084d7cf823f05e603123a7`
- Priority: evidence-driven Documentation filter integrity defect found during owner-requested `continue all`
- Task Key: `DOCUMENTATION-SEMANTIC-VIEW-CATEGORY-DEFINEDNESS`

## Confirmed defect

`SemanticViewPlanner.Build(...)` validates `SemanticViewKind` with `Enum.IsDefined(...)`, validates duplicate category filters, and validates semantic Floor/Zone/element references, but it never verifies that each `ElementCategory` filter is a defined enum value. A caller can therefore supply `(ElementCategory)999`; the planner accepts the invalid filter and silently filters every normal project element out rather than reporting invalid semantic input.

This is a symmetric integrity gap beside the completed undefined-`SemanticViewKind` validation contract: both enums are public planning inputs and undefined numeric values must fail closed rather than alter view semantics.

## Reserved scope

Validate every category filter as a defined `ElementCategory` before applying category filtering. Preserve duplicate detection, ordering, Floor/Zone relation normalization, include/exclude filters, catalog behavior and all existing defined categories.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs`
- focused new Core smoke + registration for category definedness
- this claim file

## Explicit exclusions / coordination

- Preserve the completed Semantic View kind-validation, filter-canonicality, bounds and readonly-catalog lanes.
- No changes to Semantic Sheet, Auto Layout, Tag, Catalog Editor/Store, UI/native or release surfaces.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime qualification.

## Validation plan

- `(ElementCategory)999` category filter fails closed with a deterministic `InvalidOperationException` before query filtering.
- Every currently defined `ElementCategory` remains accepted as a filter.
- Empty category filter remains valid and preserves current all-category behavior.
- Re-fetch moving `main` source blob and inspect exact PR diff before integration.

## Completion condition

Current `main` rejects undefined Semantic View category filters without changing defined filter semantics, focused regression source is merged, and this claim is closed `COMPLETED` with exact evidence.
