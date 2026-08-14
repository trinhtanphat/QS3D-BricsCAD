# Live sheet Direct Draw view guard reconciliation

Status: ACTIVE
Owner: chatgpt-web-gpt56sol
Started: 2026-08-14 13:17 +07:00
Baseline main: `ff13c47c2c3ea09d913f4af61470a592408f7808`

## Scope

- Reconcile `scripts/preflight-live-sheet-stt2-stt5.py` with the intentional Direct Draw viewport policy landed by `75d4f3eaa9743dd6cad81cc5b0b3b30646d05616` (`fix(v25): preserve view after direct draw`).
- Preserve STT2 behavior: Direct Draw must not queue `QS3DVIEW3D`; current source intentionally preserves the user's viewport for all Direct Draw categories.
- Keep the existing STT3-STT5 checks unchanged.

## Ownership boundary

Only this claim and `scripts/preflight-live-sheet-stt2-stt5.py`. Do not modify `DirectDrawCommands.cs`, Curtain/#1105/#1106, LOCAL_ONLY/runtime-qualification lanes, or other agents' claims.

## Completion

Update the stale STT2 source assertion so the aggregate preflight accepts the intentional no-auto-view policy and fails if `FinalizeUi` reintroduces `QS3DVIEW3D`. Commit/push to `main`, read back, then mark this claim SOURCE_FIXED.
