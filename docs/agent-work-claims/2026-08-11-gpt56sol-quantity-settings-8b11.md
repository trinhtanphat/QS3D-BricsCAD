# Agent Work Claim

- Agent: `gpt56sol-quantity-settings-8b11`
- Status: `ACTIVE`
- Base branch: `main`
- Work branch: `agent/gpt56sol-quantity-settings-8b11/quantity-settings-rules`
- Started: `2026-08-11`
- Updated: `2026-08-11`

## Claim

- Add a QS3D-native quantity calculation settings dialog covering formwork/category rules, pairwise intersection rules, developer thresholds, template import/export, reset-to-default, validation, and per-user persistence.
- Treat the supplied BLT3D screenshot/JSON as functional compatibility input only. Do not copy BLT3D source/assets or commit its default settings file into QS3D.
- Preserve unknown numeric compatibility category codes when importing templates, while making QS3D-native categories first-class defaults.

## Intended files

- `docs/agent-work-claims/2026-08-11-gpt56sol-quantity-settings-8b11.md`
- `src/QS3D.Core/Reporting/QuantityCalculationSettings.cs`
- `src/QS3D.BricsCAD.V25/Services/QuantitySettingsStore.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/QuantitySettingsCommands.cs`

## Coordination notes

- Avoid `Ribbon/**`, `Commands.cs`, and `QuantitySummaryWindow.*` because other active work is touching ribbon/quantity-summary interaction surfaces.
- This branch will expose the new feature through a standalone command so it can merge without editing shared command/ribbon files.
