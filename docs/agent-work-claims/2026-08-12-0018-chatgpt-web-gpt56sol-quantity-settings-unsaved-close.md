# Work claim — Quantity Settings unsaved-close guard

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-unsaved-close-20260812-0018`
- Registered: `2026-08-12T00:18:00+07:00`
- Completed: `2026-08-12T00:30:00+07:00`
- Baseline main SHA: `07c986cc4419eae81d11adf505b4586f7247c030`
- Priority: P1 — prevent silent loss of edited Quantity Settings and newly authored rules when the user closes `QS3DSETUP` before pressing `Lưu Cài Đặt`.

## Confirmed defect

`QuantitySettingsWindow` kept `_loadedSettings`, but `Close_Click` called `Close()` directly and there was no `Closing` guard. Category settings, intersection settings, restored defaults, imported templates and newly authored rules could therefore be discarded silently.

During implementation review, `_loadedSettings` was found unsuitable as the persisted dirty baseline because `LoadIntoView(...)` is also used by Import and Restore Defaults. The final implementation therefore captures a separate normalized persisted baseline after the window has loaded and refreshes it only after a successful existing Save.

## Actual implementation surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.CategoryRuleCreation.cs` — reuses the existing `Loaded` callback to initialize close tracking after constructor/view load.
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.UnsavedChanges.cs` — isolated close lifecycle, persisted baseline and exact settings comparison.
- `scripts/preflight-quantity-settings-unsaved-close.py` — focused static contract guard.
- this claim file for close-out.

No XAML edit was required because the existing loaded event already routes through the partial-class callback, and both the explicit Đóng button and window X ultimately raise WPF `Closing`.

## Implementation evidence

- `fe23be9938f4b2b6be82e9520ad30e798cc205cd` — implementation commit on `agent/chatgpt-quantity-unsaved-close-20260812-0018`.
- PR #573 — `fix(quantity): guard unsaved settings on close`.
- `997eab1c953a5f943074bda103928999cb2379c0` — authoritative PR #573 squash merge commit on `main`.

## Final behavior

- Clean settings close without a prompt.
- Dirty valid settings show explicit Save / Discard / Cancel.
- Save uses exactly one `_store.Save(current)` call in the close path, refreshes the persisted baseline and permits close only after success.
- Discard closes without persistence; Cancel leaves the window open.
- Invalid edited values fail closed: validation feedback is shown and closing is cancelled.
- Future-schema read-only mode never writes the protected settings file; dirty read-only state requires explicit discard confirmation or cancel.
- The existing Save button refreshes the separate persisted baseline only after the persisted store reload matches the normalized current view, preventing a failed save from being mistaken for a clean state.
- Import and Restore Defaults no longer become falsely clean merely because they pass through `LoadIntoView(...)`.

## Validation actually performed

- Reviewed current `main` before PR creation and compared changes since branch base; concurrent commits did not modify the three implementation surfaces.
- Reviewed the exact implementation commit/patch after branch push.
- Verified the close guard ordering by source inspection: build/validate current view → clean no-op → future-schema read-only handling → Save/Discard/Cancel → exactly one store save on Yes.
- Verified the post-existing-Save baseline hook reloads persisted settings and never performs a second save.
- Verified there are no direct JSON/file writes in the close handler.
- Reviewed PR #573 metadata after merge; GitHub reports it merged with head `fe23be9938f4b2b6be82e9520ad30e798cc205cd` and merge commit `997eab1c953a5f943074bda103928999cb2379c0`.
- The focused preflight source was added and reviewed, but it was not executed in a local checked-out repository in this remote session.
- No GitHub Actions were dispatched and no licensed BricsCAD V25/WPF runtime PASS is claimed.

## Coordination

The completed category-rule creation UI remains intact. Concurrent Quantity Core, persistence, openings, updater, Build3D/geometry and global WPF preflight claims were not modified. The global WPF XAML event-contract claim explicitly excludes existing product XAML/code-behind/partial-class edits and therefore does not overlap this product capability lane.

## Completion condition

Completed: both Close button and title-bar close can no longer silently drop valid unsaved Quantity Settings edits; clean/read-only paths remain safe, dirty valid edits require explicit Save/Discard/Cancel, invalid edits stay open for correction, focused source coverage is on `main`, and exact merge evidence is recorded above.