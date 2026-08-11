# Work claim — Quantity Settings import diagnostics coverage

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-import-diagnostics`
- Registered: `2026-08-12T00:14:00+07:00`
- Baseline main SHA: `f59ef7ab112d928605ba93634cb2d6db1d974a7f`
- Priority: P2

## Defect

`QuantitySettingsWindow` preserves imported unknown numeric category codes referenced by directed intersection rules, but its post-import diagnostic currently counts only `CategoryRules.CategoryCode`. Codes that exist only in `IntersectionRules.SourceCode` or `IntersectionRules.TargetCode` are therefore omitted from the warning even though the browser retains them.

## Owned files

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs`
- `scripts/preflight-quantity-settings-import-diagnostics.py`
- this claim file

## Planned source-safe fix

- Build the diagnostic code set from the union of category-rule codes and directed intersection source/target codes.
- Preserve `Distinct`/ordered reporting and preserve the imported payload unchanged.
- Add a focused static regression preflight that guards coverage of all three code sources and the existing `ApplySettings(imported)` path.

## Explicit exclusions

No edits to `QuantitySettingsStore.cs`, `QuantitySettingsWindow.xaml`, Core reporting/rule resolution, quantity arithmetic, BLT category inference, Ribbon, Workspace, updater/release or native geometry behavior. No rule pair will be mirrored, synthesized or filtered.

## LOCAL_ONLY disposition

This is a source-safe diagnostics fix. Licensed BricsCAD V25 WPF/runtime validation remains covered by the existing local qualification queue; no remote native-runtime PASS will be claimed.
