# Work claim — Quantity Settings clone cardinality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-clone-cardinality-20260811-2338`
- Registered: `2026-08-11T23:38:00+07:00`
- Completed: `2026-08-11T23:44:00+07:00`
- Baseline main SHA: `0ab55e0e96e0a386bc76f5f8aedb432bf81fd43a`
- Priority: P1 — close the remaining pre-validation amplification gap after the completed Quantity Settings cardinality hardening.

## Confirmed defect

`NormalizeAndValidate()` already bounded CategoryRules, IntersectionRules and the observed category universe, but several consumers intentionally clone before validation. Before this batch, `Clone()` enumerated and deep-copied raw collections before any cardinality check, allowing an oversized unvalidated settings object to duplicate a very large collection before validation could fail closed.

## Delivered scope

- `src/QS3D.Core/Reporting/QuantityCalculationSettings.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCloneValidationSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCardinalitySmoke.cs`
- `scripts/preflight-quantity-settings-cardinality.py`
- this claim file

## Implemented contract

- `Clone()` now resolves null collections to empty local lists, invokes `RequireCollectionCardinality(...)`, and only then runs `Select(Clone...).ToList()` deep-copy enumeration.
- `NormalizeAndValidate()` uses the same shared helper, so clone-time and validation-time collection count ceilings cannot silently diverge.
- Existing null-collection-as-empty behavior is preserved.
- Existing explicit null-entry failures are preserved for in-bound payloads.
- Exact unknown non-negative integer category codes and the previously merged 256 observed-code / 65,536 directed-rule limits remain unchanged.
- Sparse in-bound rule lists may still clone; the distinct observed-code universe remains a semantic validation concern and is rejected by `NormalizeAndValidate()` before matrix diagnostics.

## Regression coverage

- Existing clone smoke now covers oversized CategoryRules and IntersectionRules with a null first entry, proving the count guard wins before per-entry clone enumeration/error handling.
- Existing cardinality smoke now requires `Clone()` to fail for raw collection-count overflow while valid default, 28-code imported-style, exact unknown-code and exact-boundary payloads still clone successfully.
- Sparse 129-rule / 258-distinct-code input remains cloneable because its raw collections are bounded, then correctly fails semantic validation on the distinct-universe boundary.
- Focused preflight isolates `Clone()`, `NormalizeAndValidate()` and the shared count helper and pins guard-before-enumeration/traversal ordering.

## Product integration

- Claim registration: `76b9821898fde47d248fc2dcf8d2e06e0bdc23a2`.
- PR: `#535` — `fix(quantity): guard cardinality before settings clone`.
- Squash merge on `main`: `e9bb3ca787dc3554a75cf8a55dbd190810823ab3`.
- The implementation branch was refreshed with current `main` before merge; concurrent work was preserved and no force push was used.

## Validation actually performed

- Re-fetched post-merge `QuantityCalculationSettings.cs` from `main`; blob `52276fa5bebbe594dc8ce969be493a53cb8b34a9` shows the shared count guard executing before clone enumeration and before validation traversal.
- Source/static review only in this remote session; the smoke/preflight were not executed from a repository checkout, so no execution PASS is claimed.
- No GitHub Actions or release workflow was dispatched. No licensed BricsCAD V25 runtime PASS is claimed or needed for this Core-only source boundary.

## Coordination

Earlier clone-validation/cardinality and Quantity Settings create-rule UI claims are completed. No WPF/store, CAD, report, installer, updater, release, material, grid or persistence surfaces were modified.

## Remaining boundary

This closes raw clone amplification. Native CAD intersection measurement, face/contact classification, engulf behavior, overlap precedence and double-deduction prevention remain separate semantic/runtime work and must not be inferred from persisted field names alone.

## Completion

Reservation released. Oversized raw Quantity Settings collections now fail before `Clone()` can deep-copy them, while valid and imported exact-code behavior remains unchanged.
