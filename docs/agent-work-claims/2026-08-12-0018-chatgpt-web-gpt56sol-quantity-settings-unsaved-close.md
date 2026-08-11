# Work claim — Quantity Settings unsaved-close guard

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-unsaved-close-20260812-0018`
- Registered: `2026-08-12T00:18:00+07:00`
- Baseline main SHA: `07c986cc4419eae81d11adf505b4586f7247c030`
- Priority: P1 — prevent silent loss of edited Quantity Settings and newly authored rules when the user closes `QS3DSETUP` before pressing `Lưu Cài Đặt`.

## Confirmed defect

`QuantitySettingsWindow` already keeps `_loadedSettings` and updates that snapshot after a successful Save, but `Close_Click` currently calls `Close()` directly and the window has no `Closing` guard. Edits to category flags/thresholds, intersection flags, imported templates, restored defaults, and newly created rules can therefore be discarded by the Close button or window X with no warning.

## Reserved scope

- add one close-time dirty comparison against `_loadedSettings` using the existing `BuildSettingsFromView()` normalization boundary;
- prompt only when current valid in-window settings differ from the loaded/saved snapshot;
- `Yes` saves through the existing `_store.Save(...)` path then closes; `No` discards and closes; `Cancel` keeps the window open;
- invalid edited values fail closed: show the validation error and cancel closing rather than silently discard or persist malformed settings;
- future-schema read-only mode remains non-persistent and must never call `_store.Save` from close handling;
- cover both the explicit Đóng button and title-bar/window close through the WPF `Closing` event.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.UnsavedChanges.cs` (new isolated partial)
- `scripts/preflight-quantity-settings-unsaved-close.py` (new)
- this claim file for close-out

## Explicit exclusions

- Quantity Settings JSON store/recovery/cardinality/health-export lanes;
- Core quantity arithmetic, rule resolution/matrix diagnostics and command-line `QS3DRULECREATE`;
- project/CAD mutation, Build3D/geometry, updater/release and unrelated WPF windows;
- GitHub Actions and licensed BricsCAD V25 runtime qualification.

## Coordination

The prior Quantity Settings category-rule creation claim is completed. Recent Quantity Settings file-size and health snapshot work is store/diagnostic-only and does not own this WPF close lifecycle. Build3D and other current agent claims remain untouched.

## Validation gates

- window X and explicit Close route through the same `Closing` guard;
- dirty detection compares normalized current settings to the last loaded/saved snapshot without writing;
- clean close shows no prompt;
- `Yes` persists exactly once using `_store.Save`, updates `_loadedSettings`, then permits closing;
- `No` permits closing without persistence;
- `Cancel` cancels the close;
- malformed current UI values cancel close after an error message;
- `_persistentSettingsWriteBlocked` never writes and close remains available without overwriting a future-schema file;
- focused static preflight pins the event, decision branches and save boundary;
- no GitHub Actions dispatch.

## Completion condition

Both Close button and title-bar close can no longer silently drop valid unsaved Quantity Settings edits: clean/read-only paths close safely, dirty valid edits require explicit Save/Discard/Cancel, invalid edits remain open for correction, focused source coverage is merged to `main`, and this claim is marked `COMPLETED` with exact SHA evidence.