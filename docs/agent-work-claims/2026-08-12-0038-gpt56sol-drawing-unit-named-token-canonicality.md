# Work claim — Drawing-unit named-token canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-drawing-unit-token-20260812-0038`
- Registered: `2026-08-12T00:38:00+07:00`
- Completed: `2026-08-12T00:41:00+07:00`
- Baseline main SHA observed before registration: `d78824301dcbb858c8a960d674e88dfebd949a13`
- Claim commit: `42e9a0df6521ffbfba66125eaca6c81fa8c4ab31`
- Source fix commit: `7baf816d3ddefd0fa27b0ea898a385544711782f`
- Regression commit: `e0bfc2d30bff506586d7839f7907b488218c9550`
- Priority: P2 source-proven metadata-integrity regression hardening

## Reserved scope

Harden current drawing-unit metadata parsing so `QS3D.DrawingUnitOverride.v1` and `QS3D.DrawingUnitBound.v1` accept defined **named** `LengthUnit` tokens but reject numeric enum aliases such as `"2"`. Writers persist `unit.ToString()` names, while `Enum.TryParse(..., true)` previously also accepted numeric values that happened to map to a defined enum member. The legacy effective-unit compatibility path remains intentionally separate and unchanged.

## Implemented surfaces

- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file

## Implemented fix

- Added one shared named-token parser for current override/bound metadata.
- A current token must parse to a defined `LengthUnit` and match `Enum.GetName(...)` case-insensitively, so `"meter"` remains accepted while numeric aliases such as `"2"` are rejected.
- Current trimming behavior is preserved.
- `TryReadLegacyEffectiveUnit` remains unchanged, preserving historical values such as `"Millimeter (assumed)"`.
- Regression coverage proves lowercase named override/bound tokens remain valid and numeric aliases fail closed at both current metadata boundaries.

## Explicit exclusions honored

- No native INSUNITS mapping changes.
- No unit scale/conversion changes.
- No QSDB schema/persistence changes.
- No Direct Draw/UI/native runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Validation actually performed

- The standalone claim commit was verified as an ancestor of current `main` before substantive writes; concurrent changes after the claim were disjoint from Units.
- Exact current source/test blobs were re-fetched and SHA-guarded before writes.
- Re-read current `main` after implementation and verified `TryParseNamedUnitToken(...)` is used for override resolution and bound metadata, and the smoke contains both lowercase-name acceptance and numeric-alias rejection.
- Legacy assumed-unit coverage remains present and unchanged.
- No reset/force push was used.
- No local checkout/.NET smoke execution is claimed in this connector-only lane.
- No BricsCAD V25 runtime or GitHub Actions execution is claimed.

## Completion condition

Completed. Current drawing-unit metadata now requires named enum tokens rather than numeric aliases, focused regression coverage is committed on `main`, current blobs were re-read, and this claim records exact SHAs and actual validation boundary.
