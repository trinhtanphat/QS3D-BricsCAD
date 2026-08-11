# Work claim — Quantity Settings last-known-good backup rotation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-lkg-backup`
- Registered: `2026-08-11T21:41:00+07:00`
- Baseline main SHA: `8fd3f02c62e15f32b655996920365a4464ed25a8`
- Priority: P1 persistence hardening after verified backup-recovery behavior landed.

## Verified defect

`QuantitySettingsStore.Load()` can recover an ordinary corrupt/missing primary from `quantity_settings.json.bak`, but `WriteAtomic()` always deletes the existing backup and then calls `File.Replace(temp, path, backup, true)`. If the primary is corrupt and `.bak` is the last-known-good copy, saving the recovered settings overwrites that good backup with the corrupt former primary. The newly written primary is valid, but the recovery copy is degraded exactly after a recovery/save cycle.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Services/QuantitySettingsStore.cs`
- `scripts/preflight-quantity-settings-recovery.py`
- this claim file

## Contract

- Preserve atomic replacement of the primary file.
- Rotate a validated current primary into `.bak` on normal saves.
- When the current primary is ordinary corrupt/unreadable, do not overwrite an existing recovery backup with that corrupt primary; replace the primary atomically without creating a replacement backup from the corrupt destination.
- Keep unsupported future-schema primaries fail-closed; they must not be silently replaced or hidden behind an older backup.
- Keep the existing public `InvalidDataException` behavior and backup-path contract.

## Excluded scope

- Quantity arithmetic, runtime rule resolution, settings UI/layout, Ribbon, Workspace/RightPanel, updater/release, Direct Draw, Core persistence, or GitHub Actions.
- No native BricsCAD V25 runtime claim from this remote session.

## Validation plan

- Re-fetch latest `main` immediately before each source write and preserve concurrent winners.
- Extend the focused recovery preflight to guard valid-primary rotation, corrupt-primary backup preservation, and future-schema fail-closed behavior.
- Re-read committed source/gate and inspect status evidence without dispatching Actions.

## Completion condition

A recovery/save cycle cannot replace the last-known-good backup with a corrupt former primary, normal valid-primary backup rotation remains intact, future schemas remain fail-closed, focused regression coverage is on `main`, and this claim is marked `COMPLETED`.
