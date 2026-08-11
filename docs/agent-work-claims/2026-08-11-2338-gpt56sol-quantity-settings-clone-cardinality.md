# Work claim — Quantity Settings clone cardinality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-clone-cardinality-20260811-2338`
- Registered: `2026-08-11T23:38:00+07:00`
- Baseline main SHA: `0ab55e0e96e0a386bc76f5f8aedb432bf81fd43a`
- Priority: P1 — close the remaining pre-validation amplification gap after the completed Quantity Settings cardinality hardening.

## Confirmed defect

`NormalizeAndValidate()` now bounds CategoryRules, IntersectionRules and the observed category universe, but several valid consumers intentionally call `settings.Clone()` before validation. `Clone()` currently enumerates and deep-copies the raw collections before any cardinality check, so a caller holding an oversized unvalidated settings object can still duplicate a very large collection before the new validation boundary gets a chance to fail closed.

## Reserved scope

- Add the same early raw collection-count guard to `QuantityCalculationSettings.Clone()` before LINQ/deep-copy enumeration.
- Reuse one private collection-cardinality helper from both `Clone()` and `NormalizeAndValidate()` so the two boundaries cannot silently diverge.
- Extend existing clone/cardinality smoke and source preflight coverage for clone-time overflow refusal.

## Expected surfaces

- `src/QS3D.Core/Reporting/QuantityCalculationSettings.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCloneValidationSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCardinalitySmoke.cs`
- `scripts/preflight-quantity-settings-cardinality.py`
- this claim file for close-out

## Excluded scope

- No Quantity Settings WPF/store changes, no matrix diagnostic algorithm changes, no deduction/CAD/reporting work, no Ribbon/updater/release/GitHub Actions.
- Preserve null-collection-as-empty clone behavior and existing explicit null-entry failure messages.
- Preserve exact unknown non-negative integer category codes and the 256/65,536 limits already merged.

## Validation plan

- `Clone()` resolves null collections to empty locals, runs the shared count guard, then deep-clones entries.
- Oversized CategoryRules and oversized IntersectionRules fail from `Clone()` before `Select(...).ToList()` can duplicate them.
- `QuantityCalculationRuleSet` and matrix diagnostics inherit the same protection because they clone before validation.
- Default, null collection and deep-clone behavior remain unchanged.
- Focused preflight pins guard-before-enumeration ordering and shared-helper reuse.
- Re-fetch latest `main` before implementation/merge; preserve concurrent work without force push.

## Coordination

The earlier clone-validation and cardinality claims are `COMPLETED`. The Quantity Settings create-missing-rule UI claim is also `COMPLETED`. This lane is Core-only and does not overlap current installer, material, persistence, grid or other active claims.

## Completion condition

No public/internal consumer can duplicate an oversized raw Quantity Settings rule collection through `Clone()` before cardinality validation; focused regression source is on `main`, and this claim is marked `COMPLETED` with exact merge evidence.
