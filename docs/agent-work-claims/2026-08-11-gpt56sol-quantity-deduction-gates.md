# Work claim — Quantity calculation deduction gates

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-deduction-gates`
- Registered: `2026-08-11T21:39:00+07:00`
- Completed: `2026-08-11T21:45:00+07:00`
- Baseline main SHA observed: `ef26f8703078e1fed35e470a66fc578f22e6dc0c`
- Claim commit: `42fa9c398ccbb576655065f207cbac4394d8da79`
- Priority: P1 — continue Setup & Rules from persistence/lookup into a deterministic Core gate layer without inventing CAD intersection geometry.

## Delivered scope

- `src/QS3D.Core/Reporting/QuantityCalculationDeductionGate.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationDeductionGateSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationDeductionGateSmokeRegistration.cs`
- `scripts/preflight-quantity-calculation-deduction-gate.py`
- this claim file

## Implemented contract

- Consumes the existing defensive `QuantityCalculationRuleSet`; imported BLT integer codes are never cast to `ElementCategory`.
- Exposes exact directed pair decisions for all five persisted intersection flags.
- Uses only directly named thresholds: `MinConcreteVolumeM3` for candidate concrete-volume deductions, `MinSubtractAreaMm2` for candidate area deductions, and `MinFormworkAreaMm2` for retaining candidate formwork area.
- Rejects NaN, infinity and negative candidate measurements instead of silently clamping malformed geometry evidence.
- Preserves source -> target direction; a missing pair returns `false` and is never mirrored/synthesized.
- Native-category overloads delegate to the already-restricted exact-label compatibility lookup in `QuantityCalculationRuleSet`.

## Product commits

- `0eb070930f5e517d177562dd0c05f6e78de19777` — `feat(quantity): add deterministic deduction gates`
- `54fecd0dfc6834b194c908ede24c1c1384076acd` — `test(quantity): cover deduction gate thresholds`
- `b45f416909bf246eebbc064b3ab75384778719e6` — `test(quantity): register deduction gate smoke`
- `6c9ff16b61ab0b47d61723925f06f8e109376b6b` — `test(quantity): guard deduction gate boundaries`

## Validation evidence

- Re-fetched final Core gate, smoke, registration and focused preflight from current `main` after concurrent commits landed.
- Smoke source covers exact threshold inclusivity, below-threshold refusal, every flag, disabled rules, directed reverse independence, missing pairs, exact established native compatibility, defensive snapshots and malformed candidates.
- Focused preflight protects against integer-code enum casts, reverse lookup fallback, synthetic rules, CAD/BREP geometry, engulf assumptions and direct report/regenerator mutation in this lane.
- No GitHub Actions were dispatched. The smoke/preflight were added and source-reviewed here; this remote session does not claim they were executed in a repository checkout.

## Explicit remaining boundary

- This lane does not invent the missing CAD measurement stage. Face-normal classification, contact/BREP extraction, Boolean intersection measurement, engulf behavior and report mutation remain separate work.
- The BricsCAD V25 API does provide native solid Boolean intersection and BREP surface-area facilities, so a CAD adapter is technically possible; exact BLT parity still requires an authoritative measurement/precedence contract rather than guessing from field names.
- Ambiguous BLT category codes remain integer-code-only until an explicit mapping/reference establishes their QS3D equivalence.

## Completion

Reservation released. Core now has a deterministic threshold/flag gate that can consume measured candidate volume/area data without changing or inferring CAD geometry semantics.
