# Work claim — ElementGeometryPolicy defined-category integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-element-geometry-category-definedness`
- Registered: `2026-08-11T23:39:00+07:00`
- Baseline main SHA: `4d4b6e96cc6dbdcd266d8c385b8a1b60cd643958`
- Priority: P1 — undefined semantic categories must not fail open as non-generated geometry.

## Confirmed defect

`ElementGeometryPolicy` is a public Core policy surface. `RequiresGeneratedGeometry(...)` currently implements only equality checks against defined `ElementCategory` members, so an undefined numeric enum value such as `(ElementCategory)999` returns `false`. `SemanticCleanFlags(...)` then treats the same invalid category as not requiring generated geometry and includes `ElementDirtyFlags.Geometry` in the cleanable flags.

`ElementCategory` has no Unknown/Other sentinel; every value outside its defined members is invalid. Other current Core boundaries reject undefined enum values, so geometry/output invalidation must fail closed too.

## Reserved scope

- `src/QS3D.Core/Domain/ElementGeometryPolicy.cs`
- `tests/QS3D.Core.SmokeTests/ElementGeometryPolicyCategorySmoke.cs` (new auto-registered focused smoke)
- this claim file

## Intended contract

- Every public `ElementGeometryPolicy` method that accepts `ElementCategory` rejects undefined values with `ArgumentOutOfRangeException`.
- Existing behavior for all defined categories and property-name normalization remains unchanged.
- No new category or product policy is introduced.

## Excluded scope

- No changes to `ElementCategory` enum values.
- No regeneration/native geometry implementation changes.
- No ProjectElement/category persistence changes.
- No shared smoke registry/hardening-file edit.
- No GitHub Actions dispatch or BricsCAD runtime qualification claim.

## Validation plan

- Add an auto-registered Core smoke that checks undefined-category rejection across generated-geometry/output/clean-flags paths and preserves representative valid-category behavior.
- Re-fetch the source blob before update; use SHA guards and no force-push.
- Review exact diffs after publication and close the claim with exact SHAs.

## Completion condition

Undefined categories cannot silently downgrade geometry invalidation policy, focused regression is on `main`, and this claim is closed with source-only validation notes.
