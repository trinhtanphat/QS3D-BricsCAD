# Work claim — Quantity intersection deduction plan

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-deduction-plan`
- Registered: `2026-08-11T22:04:00+07:00`
- Baseline main SHA observed: `00c7ca0d5a9630d78e1513003c7f5231ce09a749`
- Priority: P1 — continue the completed Setup & Rules runtime gate into a geometry-agnostic deduction plan contract that future BricsCAD measurement code can feed without mutating reports directly.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityIntersectionDeductionPlanner.cs` (new)
- `tests/QS3D.Core.SmokeTests/QuantityIntersectionDeductionPlannerSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/QuantityIntersectionDeductionPlannerSmokeRegistration.cs` (new)
- `scripts/preflight-quantity-intersection-deduction-planner.py` (new)
- this claim file for close-out

## Contract

- Input is already-measured candidate evidence: one concrete intersection volume plus four directed formwork/contact areas corresponding exactly to the five persisted deduction flags.
- Consume `QuantityCalculationDeductionGate`; do not reimplement or weaken its threshold, exact-code, native compatibility or direction semantics.
- Output an immutable deduction plan that preserves the source/target codes, whether an exact rule was found, and the accepted measured deductions after flag/threshold gating.
- A missing directed rule yields `RuleFound=false` and zero accepted deductions; never fall back to the reverse pair and never synthesize a rule.
- Reject malformed category codes and malformed candidate measurements before planning.
- Do not subtract from project/report totals, infer face normals, inspect CAD/BREP geometry, apply engulf semantics or cast unknown integer codes to `ElementCategory`.

## Excluded scope

- No Quantity Settings WPF/store/schema edits, no report builder mutation, no StructuralRegenerator, no BricsCAD CAD source, no Ribbon/updater/release/GitHub Actions.

## Validation plan

- Deterministic Core smoke for all-enabled plans, mixed disabled/below-threshold candidates, missing/reverse pairs, unknown integer codes, exact threshold inclusion, immutability and malformed-input refusal.
- Focused static preflight protects the gate-delegation, zero-on-missing, no-reverse/no-synthesis/no-report-mutation/no-CAD boundary.
- Re-fetch latest `main` after implementation and preserve concurrent winners.

## Completion condition

- Future CAD measurement code has a single safe Core handoff that turns measured candidate quantities into an accepted directed deduction plan without embedding business rules in the adapter.
