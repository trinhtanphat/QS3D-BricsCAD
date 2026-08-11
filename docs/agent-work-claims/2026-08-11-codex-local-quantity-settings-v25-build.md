# Work claim — Quantity Settings V25 build compatibility

- Status: `ACTIVE`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + licensed BricsCAD V25 agent)
- Registered: `2026-08-11T21:25:00+07:00`
- Baseline main SHA: `adb2d1d4241398ea023dff721a0a9b6618f05963`
- Priority: restore the exact local V25 adapter build after the completed Quantity Settings recovery lane introduced a private exception derived from the sealed `InvalidDataException` type.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Services/QuantitySettingsStore.cs`
- `scripts/preflight-quantity-settings-recovery.py`
- this claim file

## Contract

- Preserve `InvalidDataException` as the caller-visible failure category for unsupported future schemas.
- Preserve primary-first load, validated backup recovery for ordinary missing/corrupt state, and fail-closed refusal to hide an unsupported future primary behind an older backup.
- Do not change quantity arithmetic, settings schema, UI behavior, Ribbon, updater, Workspace, Level placement, release workflows or GitHub Actions.
- Validate with the installed BricsCAD V25 references, focused settings gates, Core smoke where relevant, and aggregate source preflights.

## Coordination

- The previous Quantity Settings schema/recovery claims are `COMPLETED`; no current `ACTIVE` claim reserves the two implementation/gate files above.
- Updater compile errors remain under separate ACTIVE updater claims and are explicitly excluded.

