# Work claim — Drawing-unit named-token canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-drawing-unit-token-20260812-0038`
- Registered: `2026-08-12T00:38:00+07:00`
- Baseline main SHA observed before registration: `d78824301dcbb858c8a960d674e88dfebd949a13`
- Priority: P2 source-proven metadata-integrity regression hardening

## Reserved scope

Harden current drawing-unit metadata parsing so `QS3D.DrawingUnitOverride.v1` and `QS3D.DrawingUnitBound.v1` accept defined **named** `LengthUnit` tokens but reject numeric enum aliases such as `"1"`. Writers persist `unit.ToString()` names, while `Enum.TryParse(..., true)` currently also accepts numeric values that happen to map to a defined enum member. The legacy effective-unit compatibility path is intentionally separate and remains unchanged.

## Reserved surfaces

- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file

## Intended fix

- Require current override/bound metadata tokens to resolve to a defined enum member **and** match that member's enum name case-insensitively.
- Preserve named-token case insensitivity and current trimming behavior.
- Preserve `TryReadLegacyEffectiveUnit` support for historical values such as `"Millimeter (assumed)"`.
- Add focused smoke coverage proving numeric aliases are rejected at both override resolution and quantity-bound compatibility boundaries while lowercase named tokens remain accepted.

## Explicit exclusions

- No native INSUNITS mapping changes.
- No unit scale/conversion changes.
- No QSDB schema/persistence changes.
- No Direct Draw/UI/native runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Completion condition

Complete only after claim-first ancestry is verified, the source fix and focused regression are committed to `main`, current blobs are re-read, and this file records exact SHAs and actual validation boundary.
