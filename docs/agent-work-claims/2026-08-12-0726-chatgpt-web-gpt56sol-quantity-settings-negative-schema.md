# Work claim — Quantity Settings negative schema fail-closed

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-negative-schema`
- Registered: `2026-08-12T07:26:00+07:00`
- Baseline main SHA: `a0acf67ad9b6f777840840e20915ca9750c6dfb8`
- Priority: P2 evidence-driven remote-safe settings integrity

## Confirmed defect

`QuantityCalculationSettings.NormalizeAndValidate()` currently treats every `SchemaVersion <= 0` as an omitted legacy schema and silently upgrades it to `CurrentSchemaVersion`. A missing DataContract integer naturally deserializes as `0`, so preserving the zero-value compatibility path is reasonable; a negative schema version, however, is explicit malformed state and is currently normalized into a valid current-schema object instead of failing closed.

This allows corrupted/programmatic negative schema metadata to bypass the schema integrity boundary before runtime lookup/store consumers clone and validate the settings.

## Reserved scope

Split the zero/missing-schema compatibility case from negative schema input: `0` continues upgrading to `CurrentSchemaVersion`, while any negative schema version is rejected deterministically before other normalization.

## Expected surfaces

- `src/QS3D.Core/Reporting/QuantityCalculationSettings.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsSchemaValidationSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsSchemaValidationSmokeRegistration.cs`
- `scripts/preflight-quantity-calculation-settings-schema-validation.py`
- this claim file

## Explicit exclusions

- No Quantity Settings Store/WPF/export/recovery/future-schema behavior changes.
- No schema-version bump and no change to schema `0` compatibility.
- No category/intersection defaults, BLT mapping, deduction planner or native geometry changes.
- No GitHub Actions dispatch.

## Validation plan

- Prove schema `0` still normalizes to `CurrentSchemaVersion`.
- Prove a negative schema version throws the explicit validation error and remains negative after rejection.
- Prove current schema still validates normally.
- Add focused smoke registration and auto-discovered static preflight.
- Re-fetch final source/test/preflight from current `main`; do not claim .NET, preflight or BricsCAD runtime execution unless actually run.

## Completion condition

Negative Quantity Settings schema metadata can no longer be silently promoted to current schema, while the existing missing-schema zero compatibility remains unchanged; focused regression/preflight sources are present and the claim is closed with exact commits.