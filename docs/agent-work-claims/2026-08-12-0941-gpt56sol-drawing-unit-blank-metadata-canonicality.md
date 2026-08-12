# Work claim — Drawing-unit blank metadata canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-drawing-unit-blank-metadata-canonicality-20260812-0941`
- Registered: `2026-08-12T09:41:00+07:00`
- Baseline main SHA: `b3db2965ba164c83757c6e97ddd355eecd0499ba`
- Priority: P2 evidence-driven remote-safe unit metadata integrity

## Confirmed defect

Drawing-unit writers persist canonical named values for `QS3D.DrawingUnitOverride.v1` and `QS3D.DrawingUnitBound.v1`, while absence is represented by the metadata key not being present. The current readers use `string.IsNullOrWhiteSpace(raw)` together with missing-key detection, so an explicitly present blank value is treated as if the key were absent.

That lets corrupted persisted metadata such as an empty override silently resolve as "no override", and a blank quantity-unit binding can fall through to legacy effective-unit compatibility instead of failing the canonical binding contract.

## Reserved scope

- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file

## Expected fix

Distinguish missing optional metadata keys from present-but-blank values. Missing override/binding metadata keeps its existing absence/migration behavior; present blank values fail closed as invalid canonical metadata. Preserve case-insensitive named-token compatibility, padded-token rejection, numeric-alias rejection, writer output, and legacy effective-unit migration for genuinely missing binding keys.

## Excluded scope

- No unit factor or `UnitScale` changes.
- No redesign of legacy `QS3D.DrawingUnit` assumption parsing.
- No BricsCAD/native/runtime or GitHub Actions work.

## Validation plan

- Missing override remains unresolved without error.
- Present empty/whitespace override fails closed.
- Missing bound metadata with valid legacy effective unit remains compatible.
- Present empty/whitespace bound metadata fails closed instead of using the legacy fallback.
- Existing lowercase, padded and numeric named-token cases remain unchanged.

## Completion condition

Source and focused smoke regression are committed to `main`, exact integration SHAs are recorded, and this claim is marked `COMPLETED` without claiming local BricsCAD/runtime PASS.
