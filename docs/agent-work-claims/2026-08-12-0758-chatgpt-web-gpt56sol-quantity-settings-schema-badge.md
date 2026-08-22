# Work claim — Quantity Settings schema badge source-of-truth

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-schema-badge`
- Registered: `2026-08-12T07:58:00+07:00`
- Completed: `2026-08-12T08:05:00+07:00`
- Baseline main SHA: `f0074f030d0a320147969fd7ac51c03ee2d79ebe`
- Priority: P3 evidence-driven UI metadata integrity

## Confirmed defect

`QuantitySettingsWindow.xaml` hardcoded the visible badge as `Schema v2`, while the serialization and validation source of truth is `QuantityCalculationSettings.CurrentSchemaVersion`. A future schema bump could therefore leave the Setup & Rules window displaying stale version metadata even when the code was operating on a newer schema.

## Implemented

- `13def3dacf8e02bc1432489677328149891cb093` — registered this claim on `main` before source work.
- `fb14fa77df68d45b32dbd2d1740989494a1a8914` — replaced the hardcoded `Schema v2` XAML value with a binding to `SchemaVersionLabel`.
- `103b7bc98455f126bb68b3db816b9c1402dbee22` — added the SDK-included `QuantitySettingsWindow.SchemaVersion.cs` partial surface, deriving `SchemaVersionLabel` from `QuantityCalculationSettings.CurrentSchemaVersion` with `CultureInfo.InvariantCulture`.
- `33b29acfd16443c8d3da4d1f78d43afc94461bec` — added an auto-discovered focused static preflight that XML-parses the XAML, requires the canonical binding/property tokens and rejects a hardcoded numeric schema label.

The implementation intentionally used a small SDK-style partial class instead of modifying the existing large code-behind file. `QS3D.BricsCAD.V25.csproj` uses `Microsoft.NET.Sdk.WindowsDesktop`, so the new `.cs` file is included by the normal SDK compile item convention.

## Preserved behavior

- No schema-version bump or persistence format change.
- No `QuantitySettingsStore`, recovery, export or future-schema behavior change.
- No category/intersection defaults, BLT mapping, deduction planner or native geometry change.
- The window still sets `DataContext = this`; the schema badge now consumes the same view-model surface and the constant does not require runtime notifications.

## Validation performed

- Re-fetched the XAML from later `main` `f797d3c4e70f5db6a91ca09412d158fac867ab68` and confirmed the visible badge binds to `SchemaVersionLabel` with the hardcoded `Schema v2` removed.
- Re-fetched `QuantitySettingsWindow.SchemaVersion.cs` from the same later `main` and confirmed it derives the label from `QuantityCalculationSettings.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture)`.
- Re-fetched the focused preflight from the same later `main` and inspected its XML/binding/source-of-truth guards.
- Re-fetched `QS3D.BricsCAD.V25.csproj` and confirmed the project is SDK-style WindowsDesktop/WPF with a project reference to `QS3D.Core`; no explicit compile include is required for the new partial file.
- No GitHub Actions workflow was dispatched.
- This remote pass does **not** claim that a local .NET build, the focused Python preflight, or BricsCAD V25/V26 runtime was executed in a real checkout.

## LOCAL_ONLY disposition

This is deterministic UI metadata wiring and needs no new local-only queue item. Existing V25/V26 visual/runtime qualification boundaries remain unchanged; no remote runtime PASS is claimed.

## Completion evidence

The Setup & Rules schema badge is no longer a duplicated hardcoded version. It derives from the same canonical schema constant used by serialization/validation, and focused regression source is present. Source commits: `fb14fa77df68d45b32dbd2d1740989494a1a8914`, `103b7bc98455f126bb68b3db816b9c1402dbee22`; focused preflight: `33b29acfd16443c8d3da4d1f78d43afc94461bec`.