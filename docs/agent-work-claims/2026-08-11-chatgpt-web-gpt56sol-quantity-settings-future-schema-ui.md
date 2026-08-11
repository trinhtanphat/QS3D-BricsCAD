# Work claim — Quantity Settings future-schema UI fail-closed

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-future-schema-ui`
- Registered: `2026-08-11T21:41:00+07:00`
- Baseline main SHA: `85ec9aec52a22b036a127b4d246ecba848299f0e`
- Priority: P1 — current source can correctly reject a newer on-disk settings schema in the store, then the window's generic catch replaces the view with defaults and leaves Save available, allowing the protected future-schema primary file to be overwritten if the user clicks Save.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs`
- `scripts/preflight-quantity-settings-future-schema-ui.py`
- this claim file for close-out

## Contract

- Detect the existing unsupported-schema marker carried in the caller-visible `InvalidDataException` without editing or duplicating the active settings-store implementation.
- When startup load fails specifically because the primary settings file uses a future schema, keep the file untouched, show a clear update-required warning, and disable the persistent `Lưu Cài Đặt` action for the lifetime of that window.
- Other corrupt/unreadable settings failures retain the existing fallback-to-safe-default UI and normal Save behavior.
- Import/export may still be used to inspect/export supported templates, but importing/resetting must not clear the startup write block or allow overwriting the future-schema primary file.
- Preserve all completed Formwork, directed Intersection Rules and grouped developer-tab behavior.

## Exclusions

- No edits to `QuantitySettingsStore.cs` or its recovery gate; the active local V25 build-compatibility lane owns those files.
- No Core settings schema/arithmetic/rule-resolver changes, Ribbon, shared Theme, Workspace/RightPanel, updater/release, Direct Draw or GitHub Actions.

## Validation plan

- Re-fetch current UI files before write; preserve concurrent winners.
- Add a focused auto-discovered gate requiring marker-specific detection, startup write block, named disabled Save button, Save-time fail-closed guard, and retention of the intersection/developer layout contracts.
- Re-fetch final source/current main; no Actions dispatch and no remote native UI PASS.

## Completion condition

- A future settings schema cannot be overwritten through the fallback-default window, ordinary corrupt-file fallback remains usable, focused regression guard is present, and the claim is closed with exact SHAs.
