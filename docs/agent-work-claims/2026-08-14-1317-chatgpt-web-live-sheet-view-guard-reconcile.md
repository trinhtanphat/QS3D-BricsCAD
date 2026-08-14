# Live sheet Direct Draw view guard reconciliation

Status: SOURCE_FIXED
Owner: chatgpt-web-gpt56sol
Started: 2026-08-14 13:17 +07:00
Closed: 2026-08-14 13:20 +07:00
Baseline main: `ff13c47c2c3ea09d913f4af61470a592408f7808`

## Scope

- Reconcile `scripts/preflight-live-sheet-stt2-stt5.py` with the intentional Direct Draw viewport policy landed by `75d4f3eaa9743dd6cad81cc5b0b3b30646d05616` (`fix(v25): preserve view after direct draw`).
- Preserve STT2 behavior: Direct Draw must not queue `QS3DVIEW3D`; current source intentionally preserves the user's viewport for all Direct Draw categories.
- Keep the existing STT3-STT5 checks unchanged.

## Ownership boundary

Only this claim and `scripts/preflight-live-sheet-stt2-stt5.py` were modified. `DirectDrawCommands.cs`, Curtain/#1105/#1106, LOCAL_ONLY/runtime-qualification lanes, and other agents' claims were not modified.

## Completion evidence

- Intentional source-policy commit: `75d4f3eaa9743dd6cad81cc5b0b3b30646d05616` removes the remaining automatic `QS3DVIEW3D` dispatch from Direct Draw finalization.
- Guard reconciliation: `ab7894a7357378e31167d6e89410512bfda67a63` (`test: reconcile live sheet view preservation guard`).
- Read-back confirms the STT2 gate preserves `PaletteCoordinator.RefreshProject()`, `document.Editor.Regen()`, and `PaletteCoordinator.SetStatus(status)`, while failing if `FinalizeUi` contains `QS3DVIEW3D` or `SendStringToExecute`.
- STT3-STT5 assertions remain unchanged and current source read-back still satisfies them.
- `scripts/preflight-all.py` continues to auto-discover this `preflight-*.py` gate.
- No force-push was used.

## Qualification boundary

This reconciliation is SOURCE_FIXED. GitHub Actions release run #148 is not a valid qualification run for current `main`: it was intentionally aborted by the stale-dispatch/main-moved safety gate. A fresh workflow dispatch is required for CI/release qualification; native licensed BricsCAD V25 acceptance remains separate.
