# Work claim — Quantity Settings future-schema UI fail-closed

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-future-schema-ui`
- Registered: `2026-08-11T21:41:00+07:00`
- Completed: `2026-08-11T21:53:00+07:00`
- Baseline main SHA: `85ec9aec52a22b036a127b4d246ecba848299f0e`
- Priority: P1

## Defect

The settings store already rejects a primary `quantity_settings.json` whose schema is newer than the installed plugin and deliberately does not fall back to an older backup. The window previously caught that rejection as a generic load failure, displayed editable defaults, and left persistent Save available. A user could therefore overwrite the newer settings file from the fallback-default UI.

## Implemented

- `fe6118179b10a81af833eb7db1ac3fb8eb5f554e` — the window recognizes the existing `QS3D.QuantitySettings.UnsupportedSchema` marker, keeps a monotonic `_persistentSettingsWriteBlocked` state, shows an update-required/read-only warning, keeps import/reset from clearing the protection, and refuses `Save_Click` before `_store.Save(...)` when protection is active.
- `db0c1e5eb882f1b1bc84850cf028cef7e4ba33fd` — named the persistent Save control `SaveSettingsButton` without changing its command semantics, so future-schema startup can visibly disable the action.
- `4f7ea9fbdc7841b937a3ce6c398ce4a4d4c34c78` — disables `SaveSettingsButton` on future-schema startup and closes the remaining write path by refusing `ExportTemplate_Click` when the chosen output canonicalizes to the protected per-user settings path. Export to a different file remains available.
- `d2b7fa9aaaf378ebfd385ac7ed78c58040a2134d` — added `scripts/preflight-quantity-settings-future-schema-ui.py` guarding marker propagation, monotonic window protection, visible Save disablement, Save-before-persist ordering, same-path Export refusal, import/reset non-clearing behavior, XAML well-formedness, full intersection-matrix persistence and preservation of the completed directed-rule/developer-layout UI.

## Preserved behavior

- Ordinary corrupt/unreadable settings still load safe defaults and retain normal Save behavior; only the explicit future-schema marker activates the persistent write block.
- Supported template import/export remains usable while blocked; importing or resetting the in-memory view cannot unlock the protected primary file.
- No edit was made to `QuantitySettingsStore.cs`, Core quantity arithmetic, settings schema fields/defaults, rule resolution, Ribbon, shared Theme, Workspace/RightPanel, updater/release or Direct Draw.
- The existing Formwork tab, three-pane directed Intersection Rules browser, unknown compatibility-code retention and grouped developer settings remain intact.

## Validation

- Re-fetched current `QuantitySettingsWindow.xaml` and confirmed `SaveSettingsButton`, all grouped developer controls and the directed-rule browser are present.
- Re-fetched current `QuantitySettingsWindow.xaml.cs` and confirmed future-schema detection disables Save, the Save guard returns before `_store.Save`, and protected same-path Export returns before `_store.Export`.
- Re-fetched the focused preflight after creation. The aggregate runner auto-discovers `scripts/preflight-*.py`; no GitHub Actions workflow was dispatched.
- Current `main` moved after the test commit only through unrelated claim files at the final comparison; this lane's source/test files remained unchanged.

## LOCAL_ONLY disposition

Licensed BricsCAD V25 WPF rendering, disabled-button visual state, file-dialog interaction and DPI/focus behavior remain part of the existing local UI/runtime qualification queue. No duplicate local inbox item and no remote native runtime PASS were created.

## Completion evidence

A future-schema primary quantity-settings file can no longer be overwritten from the fallback-default window through either persistent Save or same-path Export. Current source/test tip: `d2b7fa9aaaf378ebfd385ac7ed78c58040a2134d`; concurrent `main` work was preserved and no force push was used.
