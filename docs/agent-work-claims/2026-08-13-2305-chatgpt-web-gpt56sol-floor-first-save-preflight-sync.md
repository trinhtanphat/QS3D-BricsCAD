# Work claim — Floor first-save preflight sync

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-floor-first-save-preflight-sync`
- Registered: `2026-08-13T23:05:00+07:00`
- Baseline main SHA: `194d4a5c6e011849886517553ff3d5e3d6137220`
- Claim commit: `2dd36e1f77179957d33488cca4169f4421430928`
- Fix merge commit: `3b139282d14df438d68429bd4cf7f4fad18fde74`

## Result

V25 run #130 failed because two feature guards still required the legacy `OnSaveFloorClick` XAML handler after the intentional Floor first-save bootstrap had moved the Save button to `OnSaveFloorFirstBootstrapClick`.

The fix commit updates only:

- `scripts/preflight-floor-level-responsive-footer.py`
- `scripts/preflight-material-floor-pickers.py`

Both now require `OnSaveFloorFirstBootstrapClick`. Read-back of `src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml` confirms the production Save button uses that same handler. All unrelated guard assertions remain intact.

A concurrent write landed the exact intended two-token synchronization after this claim was registered; the attempted duplicate write correctly hit a SHA mismatch and was not forced or overwritten.

No BricsCAD/local runtime PASS is claimed by this source-level close-out.