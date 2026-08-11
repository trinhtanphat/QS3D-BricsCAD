# Work claim — Quantity Settings health command

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-health-command`
- Registered: `2026-08-11T22:17:00+07:00`
- Baseline main SHA observed: `d1b931136e0d5f4e28921c5ef6b6aadf8d5d734a`
- Priority: P1 — expose the completed read-only matrix diagnostics through a native BricsCAD command without touching the concurrently moving Quantity Settings WPF/settings-core lanes.

## Reserved scope

- `src/QS3D.BricsCAD.V25/QuantitySettingsDiagnosticCommands.cs` (new)
- `scripts/preflight-quantity-settings-diagnostics-command.py` (new)
- this claim file for close-out

## Contract

- Add a modal read-only command `QS3DQSETTINGSHEALTH`.
- Load Quantity Settings through the existing `QuantitySettingsStore.Load()` recovery/future-schema contract.
- Analyze through `QuantityCalculationMatrixDiagnostics.Analyze(...)` only; do not repair or synthesize rules.
- Print a compact matrix summary plus bounded details for intersection-only codes, unreferenced category rules and missing directed pairs.
- Never print the settings file path, mutate project state, create/cache a QS3D project, save/export/import settings, open CAD transactions, or alter the drawing.
- Unsupported future schema remains fail-closed through the store and is reported as a command error; no fallback write is allowed.

## Excluded scope

- No edits to `QuantityCalculationSettings.cs`, `QuantitySettingsStore.cs`, Quantity Settings WPF, Ribbon, report builders, CAD geometry, updater/release or GitHub Actions.
- No native-runtime PASS claim from the remote session.

## Validation plan

- Focused static preflight pins command registration, Load -> Analyze order, bounded diagnostic output and all read-only/no-write boundaries.
- Re-fetch final command/preflight from latest `main` and preserve concurrent winners.

## Completion condition

- Users/local agents can run one native command to inspect rule-matrix integrity without opening the settings editor or changing any QS3D/drawing state.
