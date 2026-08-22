# Work claim — quantity-rule element freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-quantity-rule-element-freshness`
- Registered: `2026-08-12T00:58:00+07:00`
- Baseline main SHA: `d440c0d46499326c320aa907a24f00dec34256e1`
- Claim commit: `8bcd4073e21d373293b90e33f802a4a594a181de`
- ProjectElement freshness primitive commit: `c6acf7a3b338cd94dc4de58103f2b141d6508490`
- QuantityRuleEngine implementation commit: `5ca00dee18f43ac7afc8b188bf66a44aba097de6`
- Regression commit: `6ff846a70ea869a6f9b4e79fdff772862f797c95`
- Priority: deterministic Core mutation freshness defect found during owner-requested continue-all audit

## Completed

`ProjectElement` now exposes one assembly-internal `TouchPersistenceState()` primitive that advances only `UpdatedUtc`. `QuantityRuleEngine` uses it only for direct dictionary mutations that actually change persisted rule state:

- `Rule:<output>` provenance assignment now no-ops when the exact provenance string is already present, otherwise writes and touches element freshness;
- stale managed-output cleanup tracks whether quantity/provenance removal actually changed state and touches freshness once only when needed.

`SetQuantity` keeps its exact same-value no-op semantics. No rule path acquired general `SetProperty` dirty/generated-stale side effects.

## Validation actually performed

- Verified claim publication on exact HEAD before substantive work.
- The first `QuantityRuleEngine.cs` write attempt received GitHub 409 because `main` advanced; refreshed and compared from `c6acf7a3...` to the new HEAD, confirming intervening commits touched only semantic-view/browser/family/wall-footprint/unrelated claim surfaces. Re-fetched the unchanged rule-engine blob before retrying; no force/reset was used.
- Inspected exact `ProjectElement` commit diff: only the five-line internal freshness primitive was added.
- Inspected exact `QuantityRuleEngine` diff: direct provenance assignments route through `SetProvenance`, stale cleanup tracks actual removals, and both call the internal freshness primitive only on real mutation.
- Re-fetched module-initialized regression from current `main`: first direct apply advances timestamp; identical reapply is timestamp-stable; version-only provenance change advances timestamp without changing `Dirty`; stale managed-output cleanup advances timestamp while preserving `Dirty` and unrelated state; empty `ApplyMatching` is a timestamp no-op.
- GitHub Actions were not dispatched and no BricsCAD V25/V26 runtime qualification is claimed.

## Excluded scope retained

- No ProjectState ChangeVersion, preview identity/freshness, formula evaluation, rule ordering/dependency, Dirty flags, generated geometry stale, persistence schema or UI/native changes.

## Completion condition

Satisfied on current `main`; direct quantity-rule provenance/cleanup mutations now maintain element freshness without no-op churn or dirty/stale semantic expansion, focused regression coverage is present, and this lane is released.
