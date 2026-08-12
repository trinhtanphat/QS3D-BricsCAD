# Work claim — ProjectElement category reassignment invalidation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-element-category-invalidation-20260812-0837`
- Registered: `2026-08-12T08:37:00+07:00`
- Baseline main SHA: `9b6c343a5920e3e02eda59c4c43591aa85f92dac`
- Priority: P1 — ensure public semantic category reassignment invalidates derived quantity/geometry state.

## Reserved scope

`ProjectElement.Category` is a public reassignment setter and already validates that the new enum value is defined. A real valid category change currently only assigns `_category`; it does not mark dirty flags, advance element freshness, or mark existing generated outputs stale. Because category controls geometry policy, family/category health, quantity/report behavior and generated output semantics, an element can remain `Dirty=None` with apparently clean generated geometry after its category changes.

## Reserved surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs`
- `tests/QS3D.Core.SmokeTests/ProjectElementCategoryInvalidationSmoke.cs` (new focused module-initializer regression)
- this claim file

## Intended fix

- On a real valid category change, assign the new category then invalidate the element through the existing `MarkDirtyCore(ElementDirtyFlags.All, true)` primitive.
- Preserve constructor behavior (constructor writes backing field directly), same-category no-op behavior and undefined-category fail-before-mutate behavior.
- Do not clear/reassign FamilyId or change family/category health policy in this lane.
- Add focused smoke proving a clean element with generated output becomes All-dirty/stale on category change, while same-category and invalid-category calls preserve state.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25 runtime PASS claimed.
