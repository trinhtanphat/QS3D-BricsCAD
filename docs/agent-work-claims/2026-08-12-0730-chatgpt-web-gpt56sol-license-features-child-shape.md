# Work claim — license features child shape

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-license-features-child-shape-20260812-0730`
- Registered: `2026-08-12T07:30:00+07:00`
- Baseline main SHA: `610bb5b55477104b8ca22bc98ed1344d855c44f4`
- Priority: P1 — keep feature entitlements represented exactly by signed canonical payload fields.

## Reserved scope

The root license grammar is now strict, but `LicenseVerifier.Load(...)` still enumerates `features?.Elements("feature")`. Any unexpected or namespaced direct child inside `<features>` is silently skipped and therefore not represented in the signed canonical feature list. Tighten only the `<features>` container child grammar so unsigned/unrecognized markup cannot be ignored there.

## Reserved surfaces

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseFeaturesChildShapeSmoke.cs` (new focused module-initializer regression)
- this claim file

## Intended fix

- If `<features>` exists, every direct child must be an unnamespaced `<feature>` element.
- Preserve ordinary feature loading, feature count/token/delimiter/duplicate validation, root child grammar, signature handling, product/time policy, DTD/file-size protections and canonical payload format.
- Add focused `Load(...)` smoke coverage for ordinary features, an unexpected child in `<features>`, and a namespaced `<feature>` lookalike.
- Do not change attribute grammar or nested/text shape of an otherwise recognized `<feature>` in this lane.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD runtime PASS claimed.

## Completion condition

Unexpected direct children inside `<features>` fail closed on current `main`, ordinary entitlements still load, and this claim records exact integration evidence without overlapping another ACTIVE claim.
