# Work claim — Quantity calculation settings clone validation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-clone-validation`
- Registered: `2026-08-11T22:08:00+07:00`
- Completed: `2026-08-11T22:16:00+07:00`
- Baseline main SHA: `f0d51a65a6aa8fefe61dd5de6e0a63746cd6085f`
- Priority: P1 — settings validation explicitly rejects null rule entries with controlled domain errors, while the previous `Clone()` dereferenced those entries first and leaked `NullReferenceException` into clone-before-validate consumers.

## Implemented

- `bc2d37c91c419804c32f9780cb0d71ca41c9c3c1` — `QuantityCalculationSettings.Clone()` now routes category/intersection entries through explicit nullable guards, using the same error messages as `NormalizeAndValidate()` instead of a generic null dereference. Valid entries remain deep-cloned; null rule collections retain their prior empty-list behavior.
- `a3498195e1c8d708845c355d0f9354832c0fa90b` — added deterministic smoke coverage for deep clone independence, null-collection behavior, controlled null-entry rejection, and the clone-before-validate `QuantityCalculationRuleSet` path.
- `ca7f73f31885b1dd333cb83e4252734e7597789d` — module-registers the focused smoke without modifying the shared registration file.
- `be64d522bb5c34550b4f78f0b5a23c531b9006b2` — added `scripts/preflight-quantity-calculation-settings-clone.py`, which guards the two explicit clone helpers/messages, module registration, focused smoke cases, and forbids the previous unsafe direct `Select(x => x.Clone())` pattern.

## Preserved contracts

- No edit was made to `QuantitySettingsStore.cs` or `scripts/preflight-quantity-settings-recovery.py`; the active local V25 build lane remains untouched.
- No Quantity Settings WPF/schema/default-value, deduction planner/gate, rule-resolution, report arithmetic, compatibility mapping, native CAD, Ribbon, updater or release behavior changed.
- Unknown non-negative compatibility category codes remain supported exactly as before.

## Validation

- Re-fetched current `main` source after all writes and confirmed `Clone()` uses `CloneCategoryRule` / `CloneIntersectionRule`, both guards throw the shared controlled validation messages, and `NormalizeAndValidate()` uses the same messages.
- Re-fetched the focused smoke, registration and preflight from current `main`; all expected files and source tokens are present.
- `be64d522bb5c34550b4f78f0b5a23c531b9006b2` is an ancestor of current `main`; later concurrent commits did not touch this lane's source/test/preflight files in the final comparison.
- GitHub exposes no combined status checks for the final preflight commit. No GitHub Actions workflow was dispatched. This connector-only session did not claim a local .NET/V25 runtime execution.

## LOCAL_ONLY disposition

- None added by this source-only Core integrity fix. Existing exact-V25 Quantity Settings qualification remains owned by its separate local claim.

## Completion evidence

Malformed null rule-list entries can no longer leak `NullReferenceException` from clone-before-validate paths; callers now receive the same explicit validation contract while valid deep clones remain independent. Final pushed source/test tip for this lane: `be64d522bb5c34550b4f78f0b5a23c531b9006b2`.
