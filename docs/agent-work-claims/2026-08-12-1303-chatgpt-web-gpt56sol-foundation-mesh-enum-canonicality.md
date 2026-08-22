# Work claim — Foundation mesh enum metadata canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-foundation-mesh-enum-canonicality-20260812-1303`
- Registered: `2026-08-12T13:03:00+07:00`
- Completed: `2026-08-12T13:50:00+07:00`
- Baseline main SHA: `1aeeb42b6f02e4c53b47b0530f4799f8beeba618`
- Claim commit: `f999c0806b77bb94c768a240aeaffcaffff00466`
- Product fix: `7db88758d965e9bf535b4353ed6a8ee0b83328f2`
- Regression commit: `377a4633f73cf7a05999c9ef0a5427bd63e69330`
- Regression cleanup: `6d164a547c8c04aaacfeba4df0450e08c0ce947f`
- Priority: owner-requested continue-all Core health integrity

## Confirmed defect

`GeneratedFoundationMeshHealthService` validated `GeneratedFoundationMeshFaces`, `GeneratedFoundationMeshMode`, and present `GeneratedFoundationMeshFootprintMode` with case-insensitive comparisons. Non-canonical persisted tokens such as `bottom`, `foundationmeshxy`, or case-drifted footprint values could therefore pass health even though these are generated snapshot metadata tokens with canonical spellings. The same health surface already requires canonical invariant text for generated count/handles, so casing drift remained fail-open for these enum-like snapshots.

## Implemented contract

1. Existing semantic values are unchanged.
2. Present Faces accepts only exact `Bottom`, `Top`, or `Both`.
3. Mode accepts only exact `FoundationMeshXY`.
4. Present FootprintMode accepts only exact `RectangleLocalXY` or `PolygonGlobalXY`.
5. Missing FootprintMode remains valid for legacy rectangle metadata.
6. No normalization or mutation of persisted metadata is introduced.

## Regression coverage

`GeneratedFoundationMeshEnumCanonicalitySmoke` is auto-registered and covers:

- canonical enum metadata remains free of the three enum-integrity warnings;
- legacy metadata with missing FootprintMode remains accepted;
- lower-case/non-canonical Faces, Mode, and FootprintMode each fail visible through their existing warning code without contaminating the other enum checks.

## Explicit exclusions

- Foundation mesh generation/native CAD behavior;
- handle/count/ownership validation;
- numeric snapshot validation;
- stale fingerprint semantics;
- unrelated generated-health services, release tooling, persistence, or UI.

## Validation boundary

Exact product-source and regression-source readback were verified. `6d164a547c8c04aaacfeba4df0450e08c0ce947f` was verified as an ancestor/merge base of moving `main` at `9f70598a5683394e55dcd245a23a567870aec024`. No GitHub Actions, full build, executable smoke, release, or licensed BricsCAD V25/V26 runtime PASS is claimed.
