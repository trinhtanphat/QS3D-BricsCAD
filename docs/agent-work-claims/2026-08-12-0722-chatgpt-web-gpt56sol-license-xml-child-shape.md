# Work claim — license XML child shape

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-license-xml-child-shape-20260812-0722`
- Registered: `2026-08-12T07:22:00+07:00`
- Baseline main SHA: `bb2d7e5aacf264c8c5051ecf9ea5d1b5dce81e95`
- Priority: P1 — keep the signed offline license XML grammar fail-closed and unambiguous.

## Reserved scope

`LicenseVerifier.Load(...)` currently extracts the recognized `valid`, optional `features`, and `signature` children but does not reject additional root child elements. Because the signature covers the canonical license fields rather than arbitrary XML markup, an unexpected or namespaced child can be present without being represented in the signed payload. The loader should reject unrecognized child markup instead of silently ignoring it.

## Reserved surfaces

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- this claim file

## Intended fix

- Require every direct child of `<qs3dLicense>` to be an unnamespaced `valid`, `features`, or `signature` element.
- Keep existing cardinality rules: exactly one `valid`, at most one `features`, exactly one `signature`.
- Add focused `Load(...)` smoke coverage for an unexpected direct child and a namespaced lookalike child.
- Do not alter canonical payload fields, signature verification, product/time policy, token whitespace rules, DTD/file-size protections, or Base64 handling.

## Validation boundary

Committed Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD runtime PASS claimed.

## Completion condition

Unexpected direct license children fail closed on current `main`, the regression is integrated without overwriting neighboring claims, and this claim records exact merge evidence.
