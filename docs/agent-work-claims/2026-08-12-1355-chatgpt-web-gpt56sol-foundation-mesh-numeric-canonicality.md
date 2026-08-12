# Work claim — Foundation mesh numeric metadata canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-foundation-mesh-numeric-canonicality-20260812-1355`
- Registered: `2026-08-12T13:55:00+07:00`
- Completed: `2026-08-12T13:57:00+07:00`
- Baseline main SHA: `fb5b6b20aabbd2c3b5c45229858f4758a1289851`
- Claim commit: `47583652599c57c5360dbf6020b9cd604d7fe25c`
- Product fix: `f230e6426933ca432f102d643c32bfb4ec0b5ca9`
- Regression commit: `3461dc31c632a12fd4d058d4e15f761f723d919a`
- Priority: owner-requested continue-all Core health integrity

## Confirmed defect

`FoundationMeshSolidBuilder.CommitSemanticUpdate()` persists five generated numeric snapshots with round-trip invariant text (`ToString("R", CultureInfo.InvariantCulture)`), but `GeneratedFoundationMeshHealthService` accepted any semantically parseable alias. Corrupted/non-canonical metadata such as a leading plus sign, redundant decimal spelling, or exponent alias could therefore pass health although it could not have been emitted by the canonical writer.

## Implemented contract

1. Existing finite positive/non-negative validation remains intact.
2. A successfully parsed generated numeric snapshot must exactly equal `value.ToString("R", CultureInfo.InvariantCulture)`.
3. Canonical writer output remains accepted unchanged.
4. Non-canonical aliases fail visible through the existing per-field warning code; health does not normalize or mutate persisted metadata.

## Regression coverage

`GeneratedFoundationMeshNumericCanonicalitySmoke` is auto-registered and covers:

- canonical `12`, `0.2`, and `0.03` writer text remains healthy across all five fields;
- `+12` fails X diameter;
- `12.0` fails Y diameter;
- `+0.2` fails X actual spacing;
- `2E-1` fails Y actual spacing;
- `3E-2` fails cover;
- each alias reports only its targeted numeric warning among the five numeric snapshot checks.

## Explicit exclusions

Foundation mesh CAD generation, rebar design policy, handles/count/enums, stale fingerprint semantics, persistence, release tooling, and unrelated generated-health services are out of scope.

## Validation boundary

Product-source and regression-source readback were verified. Regression commit `3461dc31c632a12fd4d058d4e15f761f723d919a` was verified as the merge base/ancestor of moving `main` at `a4abd6deb170c4332db72f659814b9852a6f764c`. No GitHub Actions, full build, executable smoke, release, or licensed BricsCAD V25/V26 runtime PASS is claimed.
