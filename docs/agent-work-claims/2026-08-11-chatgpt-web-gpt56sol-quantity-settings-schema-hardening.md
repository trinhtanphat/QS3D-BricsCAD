# Work claim — Quantity settings schema/persistence hardening

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-schema-hardening`
- Registered: `2026-08-11T21:00:00+07:00`
- Priority: P1

## Scope

- Audit the newly added per-user quantity settings persistence/template workflow for schema-version correctness and lossless save/export behavior.
- Ensure successfully loaded older supported settings are written back using the current schema version instead of retaining a stale schema marker after the user edits/saves/exports them.
- Preserve unknown compatibility category codes and all rule values; do not connect unconfirmed BLT intersection/formwork semantics into production quantity geometry.
- Add deterministic source regression coverage.

## Reserved files

- `src/QS3D.BricsCAD.V25/Services/QuantitySettingsStore.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs`
- `scripts/preflight-quantity-settings-schema.py`
- this claim file for close-out

## Exclusions

- Core quantity formulas, intersection deduction geometry, formwork generation, Ribbon, updater/release work, `QuantityInsightPanel*`, and native V25 runtime qualification.

## Completion condition

- Save/export always emits `QuantityCalculationSettings.CurrentSchemaVersion` after validation while preserving rule payloads, and the UI cannot deliberately re-stamp an old imported schema version.
- Static preflight is committed and the claim is closed with exact SHA evidence.
