# Work claim — Quantity calculation deduction gates

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-deduction-gates`
- Registered: `2026-08-11T21:39:00+07:00`
- Baseline main SHA observed: `ef26f8703078e1fed35e470a66fc578f22e6dc0c`
- Priority: P1 — continue Setup & Rules from persistence/lookup into a deterministic Core gate layer without inventing CAD intersection geometry.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityCalculationDeductionGate.cs` (new)
- `tests/QS3D.Core.SmokeTests/QuantityCalculationDeductionGateSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/QuantityCalculationDeductionGateSmokeRegistration.cs` (new)
- `scripts/preflight-quantity-calculation-deduction-gate.py` (new)
- this claim file for close-out

## Contract

- Consume the existing defensive `QuantityCalculationRuleSet` rather than casting imported BLT category codes to `ElementCategory`.
- Expose exact directed pair decisions for the five persisted intersection flags.
- Apply only thresholds whose names directly establish their gate: `MinConcreteVolumeM3` for concrete-volume deductions, `MinSubtractAreaMm2` for area deductions, and `MinFormworkAreaMm2` for retaining formwork-area candidates.
- Reject non-finite or negative candidate measurements; do not silently clamp malformed geometry evidence.
- Preserve source -> target direction; never mirror or synthesize a missing rule.
- Do not implement face-normal classification, BREP/contact extraction, engulf semantics, Boolean measurement, category alias inference, or report mutation in this lane.

## Excluded scope

- No edits to Quantity Settings WPF/store/schema, Ribbon, report builder, StructuralRegenerator, CAD solid builders, updater/release or GitHub Actions.
- No BricsCAD native-runtime PASS claim.

## Validation plan

- Add deterministic Core smoke coverage for every flag, exact thresholds, below-threshold refusal, missing directed pair refusal, reverse-direction independence, unknown integer category codes, defensive snapshot behavior, and malformed candidate rejection.
- Add a focused source preflight protecting the no-mirroring/no-synthesis/no-category-cast boundary.
- Re-fetch current main after implementation and preserve concurrent winners.

## Completion condition

- Core has one deterministic gate API that a future CAD measurement adapter can call with measured candidate volume/areas, while all geometry semantics not supported by the supplied JSON/source remain explicit future work.
