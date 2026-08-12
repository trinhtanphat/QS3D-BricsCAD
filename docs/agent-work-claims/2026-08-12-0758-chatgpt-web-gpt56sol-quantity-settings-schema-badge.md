# Work claim — Quantity Settings schema badge source-of-truth

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-schema-badge`
- Registered: `2026-08-12T07:58:00+07:00`
- Baseline main SHA: `f0074f030d0a320147969fd7ac51c03ee2d79ebe`
- Priority: P3 evidence-driven UI metadata integrity

## Confirmed defect

`QuantitySettingsWindow.xaml` hardcodes the visible badge as `Schema v2`, while the serialization and validation source of truth is `QuantityCalculationSettings.CurrentSchemaVersion`. A future schema bump can therefore leave the Setup & Rules window displaying stale version metadata even when the code is operating on a newer schema.

## Reserved scope

Render the visible Quantity Settings schema badge from `QuantityCalculationSettings.CurrentSchemaVersion` using invariant formatting, and add a focused static preflight preventing a hardcoded numeric schema badge from returning.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs`
- `scripts/preflight-quantity-settings-schema-badge.py`
- this claim file

## Explicit exclusions

- No schema-version bump or persistence format change.
- No `QuantitySettingsStore`/recovery/export/future-schema behavior changes.
- No category/intersection defaults, BLT mapping, deduction planner or native geometry changes.
- No GitHub Actions dispatch.

## Validation plan

- Re-fetch the final XAML, code-behind and focused preflight from current `main` and inspect the static contract.
- Confirm the badge has a named UI target, no hardcoded `Schema v<number>` literal remains in the XAML badge, and code derives the displayed value from `CurrentSchemaVersion` with invariant formatting.
- Do not claim .NET build, focused Python preflight or BricsCAD runtime execution unless actually run in a real checkout.

## Completion condition

The Setup & Rules schema badge stays aligned with the canonical schema constant without changing persisted settings behavior, and focused regression source is present before the claim is closed.