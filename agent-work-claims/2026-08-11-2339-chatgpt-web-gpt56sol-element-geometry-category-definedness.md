# Work claim — ElementGeometryPolicy defined-category integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-element-geometry-category-definedness`
- Registered: `2026-08-11T23:39:00+07:00`
- Completed: `2026-08-11T23:41:00+07:00`
- Baseline main SHA: `4d4b6e96cc6dbdcd266d8c385b8a1b60cd643958`
- Reservation commit: `bfce67a17e2c2fc87adcd9e6fb3e059147e8e905`
- Priority: P1 — undefined semantic categories must not fail open as non-generated geometry.

## Defect fixed

`ElementGeometryPolicy` is a public Core policy surface. `RequiresGeneratedGeometry(...)` previously implemented only equality checks against defined `ElementCategory` members, so an undefined numeric enum value such as `(ElementCategory)999` returned `false`. `SemanticCleanFlags(...)` could then treat the invalid category as not requiring generated geometry and include `ElementDirtyFlags.Geometry` in its cleanable flags.

`RequiresGeneratedGeometry(...)` now rejects undefined enum values before evaluating policy. All other public geometry/output/clean-flag methods route through that boundary, so undefined categories fail closed consistently.

## Published commits

- `483b1be93ad3270cb89d20bc982af518962f45e8` — reject undefined categories at the geometry-policy boundary.
- `030a9894ef515c229add338d88804d5e15cf7e42` — add auto-registered regression covering every public category path plus representative valid-category behavior.

## Delivered contract

- Every public `ElementGeometryPolicy` method that accepts `ElementCategory` rejects undefined values with `ArgumentOutOfRangeException`.
- Existing behavior for defined categories and trimmed property names remains unchanged.
- `ElementCategory` values/product policy are unchanged.

## Validation notes

- Exact post-publication source diff adds one defined-enum guard and helper only.
- The focused smoke is isolated in a new auto-registered file; shared smoke registry/hardening files were not touched.
- Source and regression diffs were fetched after publication.
- No force-push and no GitHub Actions dispatch.
- This remote environment does not provide exact .NET/BricsCAD V25 qualification, so executable/native runtime PASS is not claimed.

## Excluded scope

- No regeneration/native geometry implementation changes.
- No ProjectElement/category persistence changes.
- No product category redesign.

## Completion condition

Satisfied for the source/static Core contract. Executable/native qualification remains separate.
