# Work claim — license XML child shape

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-license-xml-child-shape-20260812-0722`
- Registered: `2026-08-12T07:22:00+07:00`
- Baseline main SHA: `bb2d7e5aacf264c8c5051ecf9ea5d1b5dce81e95`
- Priority: P1 — keep the signed offline license XML grammar fail-closed and unambiguous.

## Reserved scope

`LicenseVerifier.Load(...)` extracted the recognized `valid`, optional `features`, and `signature` children but did not reject additional root child elements. Because the signature covers the canonical license fields rather than arbitrary XML markup, unexpected or namespaced child markup could be present without being represented in the signed payload.

## Reserved surfaces

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseXmlChildShapeSmoke.cs`
- this claim file

## Implemented fix

- Every direct child of `<qs3dLicense>` must now be an unnamespaced `valid`, `features`, or `signature` element.
- Existing cardinality rules remain unchanged: exactly one `valid`, at most one `features`, exactly one `signature`.
- Added focused `Load(...)` smoke coverage proving ordinary child shape still loads while an unexpected direct child and a namespaced lookalike child fail closed.
- `LicenseVerifierSmoke.cs`, canonical payload fields, signature verification, product/time policy, token whitespace rules, DTD/file-size protections, and Base64 handling were left unchanged.

## Integration evidence

- Initial claim: `1f693d81bc856acfd92976ea8d04dcdc7419e4cc`.
- Claim surface narrowing: `7cc5cd706505de52a39a40471129fb7c2cff87cc`.
- Branch source commit: `ad7a6f87e071956a32d2e5907083e76b48a956d1`.
- Branch smoke commit: `8de1e171c020f35d1133340e81713b63ffcdfa6e`.
- PR `#614` was opened but `main` advanced during merge attempts; GitHub rejected the merge because the base moved, not because of a code conflict. The PR was closed as redundant after direct integration.
- Comparison from `7cc5cd706505de52a39a40471129fb7c2cff87cc` to then-current `e29cff8360b6b0a5be21514fd333451dcc816550` showed 23 intervening commits and no modification of the reserved licensing source or new smoke path.
- Safe CAS source integration on current `main`: `7648751c0fb15b2f155d842d1d72914f1b7c53cd`.
- Safe CAS smoke integration on current `main`: `e22ebecc7ac75e744485542251da295bf4157242`.
- Post-write re-read confirmed source blob `ea7ece9bef056e1347dad7c57b207b74ced2e6a9` and smoke blob `5ead4834e745ba9e03a1e05d01c12750e9d74e1e` on `main`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD runtime PASS is claimed.
