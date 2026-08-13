# Work claim — Floor first-save preflight sync

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-first-save-preflight-sync`
- Registered: `2026-08-13T23:05:00+07:00`
- Baseline main SHA: `194d4a5c6e011849886517553ff3d5e3d6137220`
- Priority: close V25 run #130 false failure caused by stale Save Floor handler expectations after the intentional first-save project bootstrap landed.

## Reserved scope

Synchronize only the feature guards that still require the legacy `OnSaveFloorClick` XAML handler with the current production handler `OnSaveFloorFirstBootstrapClick`.

## Expected surfaces

- `scripts/preflight-floor-level-responsive-footer.py`
- `scripts/preflight-material-floor-pickers.py`
- this claim file for close-out

## Excluded scope

- Production Floor/Level UI/runtime behavior and bootstrap implementation.
- MAP-01B, #987, #1005, LOCAL_ONLY/runtime lanes, private DWGs, packaging/signing/updater behavior.
- GitHub Actions dispatch/re-run and licensed BricsCAD V25 qualification.

## Validation plan

- Re-read the failed run and current production XAML.
- Replace only stale legacy handler expectations with the intentional first-save bootstrap handler.
- Read back both pushed guards and XAML from `main` and verify they agree.
- Do not weaken unrelated guard coverage and do not claim runtime/local PASS.

## Completion condition

Both feature guards match the intentional Save Floor first-save bootstrap path, preserving all unrelated assertions, and the claim is closed with commit/read-back evidence.