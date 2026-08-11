# Work claim — Quantity Settings future-schema backup export protection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-future-backup-export`
- Registered: `2026-08-12T00:44:00+07:00`
- Baseline main SHA: `4eb47ea0f9b6c873b75ab5782f0aa3949a28a5d5`
- Priority: P1

## Defect

`QuantitySettingsStore.Load()` can surface an unsupported future schema from `quantity_settings.json.bak` when the primary file is absent. The window correctly enters future-schema read-only mode, but `ExportTemplate_Click` currently protects only `_store.SettingsPath`. A user can therefore choose the active backup path and overwrite the only future-schema settings copy with an older supported-schema export.

## Owned files

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs`
- `scripts/preflight-quantity-settings-future-schema-ui.py`
- this claim file

## Planned source-safe fix

- Treat both the canonical primary settings path and its `.bak` companion as protected export destinations while future-schema write protection is active.
- Preserve export to unrelated template paths and all existing Save/import/reset behavior.
- Extend the existing future-schema static preflight to guard the backup-path refusal before `_store.Export`.

## Explicit exclusions

No edit to `QuantitySettingsStore.cs`, XAML, Core quantity arithmetic/rule semantics, category mapping, Ribbon, Workspace, updater/release or native geometry behavior.

## LOCAL_ONLY disposition

This is a source-side persistence safety fix. File-dialog/native BricsCAD V25 UI behavior remains in the existing local qualification queue; no remote native-runtime PASS will be claimed.
