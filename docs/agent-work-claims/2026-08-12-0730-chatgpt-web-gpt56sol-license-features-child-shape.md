# Work claim — license features child shape

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-license-features-child-shape-20260812-0730`
- Registered: `2026-08-12T07:30:00+07:00`
- Baseline main SHA: `610bb5b55477104b8ca22bc98ed1344d855c44f4`
- Priority: P1 — keep feature entitlements represented exactly by signed canonical payload fields.

## Reserved scope

The root license grammar was strict, but `LicenseVerifier.Load(...)` still enumerated `features?.Elements("feature")`. Unexpected or namespaced direct children inside `<features>` were silently skipped and therefore not represented in the signed canonical feature list.

## Reserved surfaces

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseFeaturesChildShapeSmoke.cs`
- this claim file

## Implemented fix

- If `<features>` exists, every direct child must now be an unnamespaced `<feature>` element.
- Ordinary feature loading, feature count/token/delimiter/duplicate validation, root child grammar, signature handling, product/time policy, DTD/file-size protections and canonical payload format remain unchanged.
- Added focused `Load(...)` smoke coverage for ordinary features, an unexpected child in `<features>`, and a namespaced `<feature>` lookalike.
- Attribute grammar and nested/text shape of an otherwise recognized `<feature>` were intentionally left out of this lane.

## Integration evidence

- Claim registration: `2ea6c2204422852ed875940d345d87afc85f1d5b`.
- Branch source commit: `d6793324312606b0bc764983d5043574df19c4a2`.
- Branch smoke commit: `1eb9988f1a9311b9697c91ffe7c5fe3877a275a7`.
- PR: `#620`.
- Squash merge on `main`: `5cbebc2981a263a220f6c50df5aa5d6f4e872bb7`.
- Branch diff contained exactly the reserved source file plus the new focused smoke file.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD runtime PASS is claimed.
