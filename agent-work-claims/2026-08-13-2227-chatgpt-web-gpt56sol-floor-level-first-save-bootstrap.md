# Work claim — V25 Floor / Level first-save project bootstrap

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-floor-level-first-save-bootstrap-20260813`
- Registered: `2026-08-13T22:27:00+07:00`
- Completed: `2026-08-13T22:55:00+07:00`
- Baseline main SHA: `ba2932267f1ca168cb9a043faa88b3b58ea49cc7`

## Result

`QS3DLEVELS` now permits the explicit first **new-floor Save** on a drawing whose latest Refresh observed no QS3D project. The Save validates the floor draft first, verifies the bound DWG is still active, re-checks that no project appeared since Refresh, then uses the canonical `ProjectContextCoordinator.GetOrCreate` path. Existing-floor edits and all other Floor/Level mutations retain the exact refreshed-project fail-closed guard.

A failed mutation after first-save project creation restores the project snapshot and calls `ProjectContextCoordinator.Forget`, so a failed attempt does not strand a replacement in-memory project. Refresh and inspection remain non-creating.

## Source / regression commits

- `e7c2db75bdea6dfd4cc845a600193e359aaa14ee` — guarded first-save project acquisition helper
- `32151bfd7bd2670500627f56fbbfe10ba7193917` — focused Save handler with validation and rollback/Forget
- `88cf3cca80170657de2f8a4bbd68fe7a8bec2bee` — wire XAML Save button to the guarded handler
- `b6ebd54e0e9037612985d48c061038798844de74` — preserve/extend stale-project source guard
- `ffff71f17d9582e59aa6c1084a44bec0264d3f81` — focused first-save bootstrap regression

`ffff71f17...` is an ancestor of the subsequently moving `main`; comparison against `9fd18c64574006ea4af01f64638a4efd59c58892` showed `behind_by=0`.

## Validation boundary

Source wiring, canonical project acquisition, stale-project invariants, validation ordering, and rollback/Forget contracts were read back from `main`. The V25 project is SDK-style WPF so the new partial `.cs` files are included by default. No GitHub Actions workflow was dispatched. Real BricsCAD V25 NETLOAD/UI execution remains `LOCAL_ONLY` and is not claimed as runtime PASS here.
