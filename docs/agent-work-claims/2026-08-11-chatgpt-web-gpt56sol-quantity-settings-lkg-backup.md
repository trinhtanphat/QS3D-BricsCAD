# Work claim — Quantity Settings last-known-good backup rotation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-lkg-backup`
- Registered: `2026-08-11T21:41:00+07:00`
- Completed: `2026-08-11T21:46:00+07:00`
- Baseline main SHA: `8fd3f02c62e15f32b655996920365a4464ed25a8`
- Priority: P1 persistence hardening after verified backup-recovery behavior landed.

## Verified defect

`QuantitySettingsStore.Load()` can recover an ordinary corrupt/missing primary from `quantity_settings.json.bak`, but the previous `WriteAtomic()` always deleted the existing backup and then called `File.Replace(temp, path, backup, true)`. If the primary was corrupt and `.bak` was the last-known-good copy, saving recovered settings overwrote that good backup with the corrupt former primary. The newly written primary became valid while the recovery copy degraded exactly after a recovery/save cycle.

## Completed changes

- `63601d35a85830fefcf543870fd1bda68e9a0101` — validates the existing primary before deciding whether it is safe to rotate into `.bak`. A valid primary still becomes the normal backup; an ordinary corrupt/unreadable primary is atomically replaced using `File.Replace(temp, path, null, true)` so an existing last-known-good `.bak` is left intact. Unsupported future schemas are not caught by the ordinary-corruption classifier and therefore remain fail-closed.
- The same source change removes the pre-delete of an existing backup before normal rotation. `File.Replace` owns the replacement of the backup destination, avoiding a needless interval where the last-known-good backup has already been deleted but the primary replacement has not yet succeeded.
- `7393f736f91ea67f8fb21a162bb5b58cf5275626` — extends `scripts/preflight-quantity-settings-recovery.py` to require primary validation before rotation, valid-primary backup rotation, corrupt-primary backup preservation, atomic no-backup replacement for the corrupt-primary branch, future-schema fail-closed behavior, and rejection of the old pre-delete pattern.

## Validation evidence

- Re-fetched `QuantitySettingsStore.cs` from current `main` after the implementation; the committed source still contains `CanRotatePrimaryIntoBackup(...)`, routes ordinary corruption to `File.Replace(temp, path, null, true)`, routes valid primaries to the common `.bak` path, and leaves marked unsupported schemas uncaught.
- Re-fetched `scripts/preflight-quantity-settings-recovery.py`; the focused source gate locks the recovery/rotation ordering and rejects reintroducing `if (File.Exists(backup)) File.Delete(backup);`.
- Microsoft .NET `File.Replace` documentation states that an existing destination backup is replaced with the old destination content, and that passing `null` as `destinationBackupFileName` replaces the destination without creating a backup. This supports both the normal rotation and corrupt-primary preservation branches without changing the atomic replace primitive.
- GitHub reports no combined status checks and no automatic workflow runs for the regression commit; no Actions workflow was dispatched.
- The preceding local V25 build-compatibility claim had already restored and validated compilation of `QuantitySettingsStore.cs` after the sealed-exception repair. This remote lane did not independently run licensed BricsCAD V25/native runtime qualification and does not claim it.

## Coordination / exclusions respected

No edits were made to Quantity Settings UI/layout, Core runtime-rule resolution, quantity arithmetic, Ribbon, Workspace/RightPanel, updater/release, Direct Draw, Core persistence or GitHub Actions. Concurrent `main` commits were preserved; no force update was used.

## Result

A successful save after backup recovery can no longer replace the last-known-good `.bak` with a corrupt former primary. Normal valid-primary rotation remains atomic, future schemas remain fail-closed, and focused regression coverage is on `main`.
