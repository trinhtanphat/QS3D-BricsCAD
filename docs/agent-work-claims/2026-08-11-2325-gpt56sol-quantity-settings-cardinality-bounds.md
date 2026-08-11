# Work claim — Quantity Settings cardinality bounds

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-cardinality-20260811-2325`
- Registered: `2026-08-11T23:25:00+07:00`
- Baseline main SHA: `f3ab6727e8f5fbefc9596d312ae9193f31346875`
- Priority: P1 — harden imported/edited Quantity Settings against unbounded category universes and quadratic matrix-diagnostic allocation while preserving exact unknown integer-code compatibility.

## Reserved scope

Bound the cardinality of `QuantityCalculationSettings` so validated payloads cannot drive unbounded `ObservedCategoryCodes × ObservedCategoryCodes` work in matrix diagnostics. Preserve all existing schema, exact unknown-code, directed-rule and validation semantics.

## Expected surfaces

- `src/QS3D.Core/Reporting/QuantityCalculationSettings.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCardinalitySmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCardinalitySmokeRegistration.cs` (new)
- `scripts/preflight-quantity-settings-cardinality.py` (new)
- this claim file for close-out

## Contract

- Define one explicit maximum observed category-code universe large enough for current QS3D/native + imported BLT-compatible settings while keeping directed-matrix work bounded.
- Reject `CategoryRules` beyond that category-universe bound before deeper processing.
- Reject `IntersectionRules` beyond the corresponding maximum directed-pair count before deeper processing.
- While validating directed rules, track the union of every source/target code together with `CategoryRules` and fail as soon as the observed universe exceeds the bound.
- Unknown non-negative integer category codes remain supported inside the bound; no enum cast or alias inference is introduced.
- Valid current/default payloads and existing 28-code imported-style matrices remain accepted.

## Excluded scope

- No edits to Quantity Settings WPF or `QuantitySettingsStore.cs`; the active create-missing-rule UI claim and local recovery/build lanes own those surfaces.
- No changes to `QuantityCalculationMatrixDiagnostics`, deduction arithmetic, CAD/BREP geometry, reporting totals, Ribbon/Start Center, updater/release or GitHub Actions.
- No lowering of existing finite/range/duplicate/schema validation.

## Validation plan

- New deterministic Core smoke proves current default settings remain valid, a 28-code full directed matrix remains valid, exact unknown integer codes remain valid, category-rule overflow fails, directed-rule-count overflow fails, and sparse intersection rules with too many distinct observed codes fail before diagnostics can construct an unbounded cross product.
- Focused static preflight pins explicit limits, early count guards, incremental observed-code guarding, and forbids enum casts or automatic rule/category synthesis.
- Re-fetch current `main` immediately before each write and preserve concurrent winners without force push.
- No GitHub Actions dispatch; no native-runtime PASS claim is needed for this Core-only boundary.

## Coordination

The active Quantity Settings create-rule UI claim explicitly excludes Core quantity settings models/arithmetic, so this reservation is non-overlapping. Existing health/diagnostic, deduction-planner and clone-validation claims are completed. This lane does not touch other active ownership, material, grid, persistence or UI claims.

## Completion condition

Validated Quantity Settings have a documented finite cardinality boundary that prevents quadratic diagnostic/matrix memory amplification while retaining current/default and imported unknown-code behavior; deterministic smoke and source preflight are pushed to `main`, and this claim is marked `COMPLETED` with exact implementation SHAs.
