# Work claim — Quantity intersection deduction plan

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-deduction-plan`
- Registered: `2026-08-11T22:04:00+07:00`
- Completed: `2026-08-11T22:09:00+07:00`
- Baseline main SHA observed: `00c7ca0d5a9630d78e1513003c7f5231ce09a749`
- Claim commit: `716f86b964e7807e2fcd8280536e3f99f15ee2d4`
- Priority: P1 — continue the completed Setup & Rules runtime gate into a geometry-agnostic deduction plan contract that future BricsCAD measurement code can feed without mutating reports directly.

## Delivered scope

- `src/QS3D.Core/Reporting/QuantityIntersectionDeductionPlanner.cs`
- `tests/QS3D.Core.SmokeTests/QuantityIntersectionDeductionPlannerSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityIntersectionDeductionPlannerSmokeRegistration.cs`
- `scripts/preflight-quantity-intersection-deduction-planner.py`
- this claim file

## Implemented contract

- Added immutable `QuantityIntersectionCandidateMeasurement` input carrying one measured concrete intersection volume and the four measured directed formwork/contact areas corresponding exactly to the five persisted deduction flags.
- Added immutable `QuantityIntersectionDeductionPlan` output preserving source/target codes, exact-rule availability and only the candidate measurements accepted by the existing gate.
- `QuantityIntersectionDeductionPlanner` delegates all flag/threshold decisions to `QuantityCalculationDeductionGate`; it does not repeat numeric thresholds or category compatibility logic.
- Missing directed pairs return `RuleFound=false` with all accepted deductions equal to zero. Reverse lookup is never attempted and no rule is synthesized.
- Unknown imported integer category codes round-trip exactly; the planner does not cast them to `ElementCategory`.
- Negative category codes and NaN/infinite/negative candidate measurements fail closed before planning.

## Product commits

- `a18575534b85bc14305a05532a866e680690b98c` — `feat(quantity): plan accepted intersection deductions`
- `506a6b27ed46a17a61f5dd5cae7e0e2b8909342a` — `test(quantity): cover deduction plan handoff`
- `aabad2d43b195c0bd0bc1306875a0117509c6005` — `test(quantity): register deduction planner smoke`
- `fbfe725dbb4a7527b95aa293e344dd34c290dc9c` — `test(quantity): guard deduction planner handoff`
- `ed511bc74327414a2441f847553abf940a392475` — clarified the mixed accepted-deduction smoke assertion.

## Validation evidence

- Re-fetched the final planner and smoke from current `main` after concurrent repository movement and confirmed the registered files remained intact.
- Smoke source covers exact-threshold inclusion, all five enabled deductions, mixed disabled/below-threshold candidates, missing/reverse pair isolation, unknown integer-code preservation, candidate non-mutation and malformed-input refusal.
- Focused preflight forbids threshold duplication, reverse lookup, synthesized rules, integer-code enum casts, CAD/BREP/Solid3d dependencies, engulf assumptions, ProjectState/AuditTrail access, report builder mutation and StructuralRegenerator coupling.
- No GitHub Actions were dispatched. This remote session source-reviewed the final files but does not claim execution of the smoke/preflight in a repository checkout or licensed BricsCAD runtime.

## Remaining boundary

- The next missing layer is the BricsCAD measurement adapter that produces the five candidate measurements from real generated solids/contact faces. The planner now gives that adapter a narrow handoff so geometry code does not own deduction business rules.
- Exact BLT parity for contact-area classification, face orientation, engulf handling, multiple-overlap precedence and double-deduction prevention still requires authoritative reference behavior/sample outputs rather than inference from setting names.

## Completion

Reservation released. Future CAD measurement work can feed measured evidence into this planner and receive a deterministic accepted directed deduction plan without mutating project/report state.
