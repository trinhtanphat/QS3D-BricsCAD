# Work claim — Quantity calculation settings clone validation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-clone-validation`
- Registered: `2026-08-11T22:08:00+07:00`
- Baseline main SHA: `f0d51a65a6aa8fefe61dd5de6e0a63746cd6085f`
- Priority: P1 — current settings validation explicitly rejects null rule entries with controlled domain errors, but `Clone()` dereferences those entries first and leaks `NullReferenceException` into every clone-before-validate consumer.

## Reserved scope

- Harden `QuantityCalculationSettings.Clone()` so malformed null category/intersection rule entries fail with the same explicit `InvalidOperationException` contract used by `NormalizeAndValidate()`, while valid entries remain deeply cloned and null collections retain their existing empty-list behavior.
- Add deterministic Core smoke coverage and a focused source preflight for the clone/validation boundary.

## Expected surfaces

- `src/QS3D.Core/Reporting/QuantityCalculationSettings.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCloneValidationSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCloneValidationSmokeRegistration.cs`
- `scripts/preflight-quantity-calculation-settings-clone.py`
- this claim file for close-out

## Excluded scope

- No edits to `QuantitySettingsStore.cs` or its recovery gate; the active local V25 build claim owns that lane.
- No Quantity Settings WPF/schema/default-value changes, no deduction planner/gate/rule-resolver semantics, no report-total mutation, no CAD/native source, Ribbon, updater, release or GitHub Actions.
- Do not infer new engineering thresholds or compatibility mappings.

## Validation plan

- Smoke-test deep clone independence for valid category/intersection rules, null-collection normalization, controlled rejection of null category/intersection entries, and clone-before-validate `QuantityCalculationRuleSet` behavior.
- Focused preflight requires explicit per-entry clone guards and forbids the unsafe direct `Select(x => x.Clone())` regression.
- Re-fetch current `main` before implementation and integrate only if this claim remains non-overlapping.

## Coordination

- The active Quantity intersection deduction-plan claim owns only its new planner/test/preflight files and explicitly excludes Quantity Settings schema edits.
- The local Quantity Settings V25 build claim owns `QuantitySettingsStore.cs` and `scripts/preflight-quantity-settings-recovery.py`; this lane does not touch either.

## Completion condition

- Malformed rule-list entries can no longer leak a generic null dereference from clone-before-validate paths; valid deep-clone behavior remains unchanged, regression coverage is present, and this claim is marked `COMPLETED` with exact pushed evidence.
