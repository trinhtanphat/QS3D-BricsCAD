# Work claim — Quantity Rule duplicate-id integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-duplicate-id-integrity-20260812-0856`
- Registered: `2026-08-12T08:56:00+07:00`
- Completed: `2026-08-12T08:59:00+07:00`
- Baseline main SHA: `f9f6332bde8bc958cdeda748b586a59b15ae8b5e`
- Claim commit: `407b715081b2d1937e49eedab90b959c094e7a27`
- Implementation commit: `59116bf025ae2bdae62e181cf1d7677110b4a399`
- Regression-test commit: `d2772dcac9a9644402796923e0e49424c1692620`
- Final pushed product/test SHA: `d2772dcac9a9644402796923e0e49424c1692620`
- Priority: P1 persisted identity / rule provenance integrity found during owner-requested full review/fix continuation

## Confirmed defect

`ProjectState.FindQuantityRule(...)` defines duplicate Quantity Rule IDs as invalid project state, but `QuantityRuleEngine.ApplyMatching(...)` validated only duplicate output names. Two persisted rules could therefore share the same `Id` while targeting different outputs; both were evaluated and published provenance using the indistinguishable `RuleId@Version` token. This bypassed the canonical rule-identity invariant and made generated quantity provenance ambiguous.

## Implemented

`ApplyMatching(...)` now validates the full persisted Quantity Rule collection before category filtering. The preflight preserves the canonical null-rule error and additionally rejects duplicate rule IDs case-insensitively before stale-output discovery/cleanup, expression evaluation, quantity writes, or provenance writes.

## Regression coverage

`QuantityRuleDuplicateIdPreflightSmoke` pins both sides of the contract:

- duplicate `RULE-1` / `rule-1` identities are rejected even when the second rule belongs to another category, proving validation is global rather than hidden by category filtering;
- rejection leaves preexisting stale managed quantity, provenance, and element freshness unchanged;
- distinct rule IDs continue to apply normally and publish distinct canonical `RuleId@Version` provenance tokens.

## Concurrency handling

The first source write received HTTP 409 because `main` moved concurrently. The current source was re-read and still lacked duplicate-ID validation, so the patch was retried against the current blob without force-push or overwriting unrelated work.

## Validation boundary

The implementation and smoke source were read back from current `main` after their writes. No GitHub Actions workflow was dispatched or re-run. No executable Core build/smoke PASS and no licensed BricsCAD V25/V26 runtime qualification are claimed from this web session.

## Outcome

Quantity Rule evaluation now preserves the same unique persisted identity contract enforced by `ProjectState.FindQuantityRule(...)`, preventing ambiguous generated provenance while retaining existing valid rule behavior.
