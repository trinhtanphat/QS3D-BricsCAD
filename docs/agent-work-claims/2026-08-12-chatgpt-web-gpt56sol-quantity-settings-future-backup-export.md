# Work claim — Quantity Settings future-schema backup export protection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-future-backup-export`
- Registered: `2026-08-12T00:44:00+07:00`
- Completed: `2026-08-12T00:47:00+07:00`
- Baseline main SHA: `4eb47ea0f9b6c873b75ab5782f0aa3949a28a5d5`
- Priority: P1

## Defect

`QuantitySettingsStore.Load()` can surface an unsupported future schema from `quantity_settings.json.bak` when the primary file is absent. The window correctly enters future-schema read-only mode, but `ExportTemplate_Click` protected only `_store.SettingsPath`. A user could therefore choose the active backup path and overwrite the only future-schema settings copy with an older supported-schema export.

## Implemented

- `17d3fe4fe4fe63c621ec640e6a91362ffd5ace12` — future-schema export protection now routes through `IsProtectedSettingsPath`, which canonicalizes and rejects both `_store.SettingsPath` and `_store.SettingsPath + ".bak"` before `_store.Export`; unrelated template destinations remain available.
- `15180b1cbd2f6eb84ec8e0623d28499415abc98f` — extended the existing future-schema static preflight to guard Store backup provenance, primary/backup protected-path coverage and fail-closed ordering before export.

## Preserved behavior

- Persistent Save remains disabled/fail-closed only for the explicit unsupported-schema state.
- Import/reset cannot clear the monotonic future-schema write block.
- Export to unrelated user-selected template files remains supported.
- No edit was made to `QuantitySettingsStore.cs`, XAML, Core quantity arithmetic/rule semantics, category mapping, Ribbon, Workspace, updater/release or native geometry behavior.

## Validation

- Re-fetched `17d3fe4fe4fe63c621ec640e6a91362ffd5ace12`; its source diff is limited to replacing the single-path export guard with the primary-plus-backup protected-path helper and updating the warning text.
- Re-fetched `15180b1cbd2f6eb84ec8e0623d28499415abc98f`; its diff extends only the existing future-schema source preflight.
- The aggregate runner auto-discovers `scripts/preflight-*.py`; no GitHub Actions workflow was dispatched and this remote lane does not claim the preflight was executed.
- No force push or overwrite of unrelated concurrent files was used.

## LOCAL_ONLY disposition

File-dialog/native BricsCAD V25 UI behavior remains part of the existing local qualification queue. No duplicate local inbox item and no remote native-runtime PASS were created.

## Completion evidence

When unsupported future-schema settings originate from the backup because the primary is absent, neither the primary path nor its `.bak` copy can be overwritten by an older supported-schema export from the read-only window. Source commit: `17d3fe4fe4fe63c621ec640e6a91362ffd5ace12`; regression commit: `15180b1cbd2f6eb84ec8e0623d28499415abc98f`.
