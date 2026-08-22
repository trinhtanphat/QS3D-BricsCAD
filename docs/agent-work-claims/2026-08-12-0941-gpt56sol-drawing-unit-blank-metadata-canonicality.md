# Work claim — Drawing-unit blank metadata canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-drawing-unit-blank-metadata-canonicality-20260812-0941`
- Registered: `2026-08-12T09:41:00+07:00`
- Completed: `2026-08-12T09:44:00+07:00`
- Baseline main SHA: `b3db2965ba164c83757c6e97ddd355eecd0499ba`
- Claim commit: `2fb0247461dd32d10dce40a3d55b07531c8926be`
- Source fix commit: `50d107a57dfb0bb69200389337af30e97ca39d88`
- Regression commit: `c09ae331089095130bcc7dbb80b58346635d4b02`
- Priority: P2 evidence-driven remote-safe unit metadata integrity

## Confirmed defect

Drawing-unit writers persist canonical named values for `QS3D.DrawingUnitOverride.v1` and `QS3D.DrawingUnitBound.v1`, while absence is represented by the metadata key not being present. The previous readers used `string.IsNullOrWhiteSpace(raw)` together with missing-key detection, so an explicitly present blank value was treated as if the key were absent.

That allowed corrupted persisted metadata such as an empty override to silently resolve as "no override", and a blank quantity-unit binding could fall through to legacy effective-unit compatibility instead of failing the canonical binding contract.

## Reserved scope

- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file

## Implemented fix

`TryResolve(...)` now treats only a missing override key as absence and rejects an explicitly present blank override. `TryReadCanonical(...)` likewise returns `false` only for a missing canonical key and rejects present blank values before any legacy quantity-unit fallback can run.

Case-insensitive named-token compatibility, padded-token rejection, numeric-alias rejection, writer output, native INSUNITS precedence, and legacy effective-unit migration for genuinely missing bound metadata are unchanged.

## Validation evidence

- `c09ae331089095130bcc7dbb80b58346635d4b02` adds empty and whitespace override regressions.
- The same smoke adds an empty bound regression and a whitespace bound case carrying otherwise-valid legacy effective-unit metadata, proving the malformed binding cannot bypass canonical validation.
- The existing missing-key legacy migration, lowercase named-token, padded-token and numeric-alias coverage remains in place.
- Source and smoke were re-read from current `main` after integration and the expected guards are present.

## Excluded scope

- No unit factor or `UnitScale` changes.
- No redesign of legacy `QS3D.DrawingUnit` assumption parsing.
- No BricsCAD/native/runtime or GitHub Actions work.

## Completion condition

Completed: source and focused smoke regression are committed to `main`, exact integration SHAs are recorded, and no local BricsCAD/runtime PASS is claimed.
