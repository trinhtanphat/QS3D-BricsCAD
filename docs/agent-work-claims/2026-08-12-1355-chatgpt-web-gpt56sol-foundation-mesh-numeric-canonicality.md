# Work claim — Foundation mesh numeric metadata canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-foundation-mesh-numeric-canonicality-20260812-1355`
- Registered: `2026-08-12T13:55:00+07:00`
- Baseline main SHA: `fb5b6b20aabbd2c3b5c45229858f4758a1289851`
- Priority: owner-requested continue-all Core health integrity

## Confirmed defect

`FoundationMeshSolidBuilder.CommitSemanticUpdate()` persists five generated numeric snapshots with round-trip invariant text (`ToString("R", CultureInfo.InvariantCulture)`), but `GeneratedFoundationMeshHealthService` currently accepts any semantically parseable alias. Corrupted/non-canonical metadata such as a leading plus sign, redundant decimal spelling, or exponent alias can therefore pass health although it could not have been emitted by the canonical writer.

## Owned scope

- `src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs`
- planned regression: `tests/QS3D.Core.SmokeTests/GeneratedFoundationMeshNumericCanonicalitySmoke.cs`

## Intended contract

1. Preserve existing finite positive/non-negative semantic validation.
2. After successful parsing, require exact round-trip invariant text for each generated numeric snapshot.
3. Keep canonical writer output accepted unchanged.
4. Fail visible through the existing per-field invalid warning codes; do not normalize or mutate persisted metadata.

## Explicit exclusions

Foundation mesh CAD generation, rebar design policy, handles/count/enums, stale fingerprint semantics, persistence, release tooling, and unrelated generated-health services are out of scope.
