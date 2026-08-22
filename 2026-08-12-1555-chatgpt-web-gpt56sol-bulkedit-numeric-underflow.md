# Work claim — BulkEdit numeric underflow integrity

- Status: `COMPLETED`
- Agent: `ChatGPT web / GPT-5.6 Sol`
- Registered: `2026-08-12T15:55:00+07:00`
- Completed: `2026-08-12T16:08:00+07:00`
- Baseline main SHA: `41c2c5c7217986995151db282216bd9b4b18f09a`
- Priority: Proven Core bulk-edit numeric integrity defect: direct invariant parsing and multiplication could silently collapse mathematically non-zero semantic property values to exact zero.

## Reserved scope

Harden `BulkEditService.MultiplyNumericProperty()` so it fails closed when:

- a syntactically non-zero invariant numeric property token parses to exact zero because of IEEE-754 underflow; or
- finite non-zero parsed value and finite non-zero factor multiply to exact zero.

Preserve legitimate zero tokens/operands, representable subnormal results, exact numeric no-op behavior, existing overflow handling, atomicity, project freshness and generated-geometry dirty semantics.

## Implementation

- Claim publication: `8a17e54fdc9c8beae65fec6748a26fe1fe279f0c`.
- Product guard: `e16e5629b456c92b9ec614d25b422404e645a028`.
- Focused regression: `bb1a84e78ff490cead56bec10731464a8b2e48f2`.

`BulkEditService.MultiplyNumericProperty()` now rejects syntactically non-zero property tokens that parse to exact zero and rejects finite non-zero multiplication that underflows to exact zero. Explicit zero operands remain valid, exact numeric no-ops remain lexical/freshness no-ops, and representable subnormal results remain writable.

## Regression coverage

`BulkEditNumericNoOpSmoke` now covers:

- `"1e-4000"` parse underflow with no project/element/property mutation;
- `double.Epsilon * 0.5` multiplication underflow with no partial mutation;
- exact scientific zero `"0e-4000"` remaining a lexical/freshness no-op;
- representable `double.Epsilon * 2` remaining valid;
- explicit zero-factor multiplication remaining a legitimate zero-producing edit;
- the earlier geometry/non-geometry x1 no-op and real-change behavior.

## Validation actually obtained

- Read back `BulkEditService.cs` from implementation head `bb1a84e78ff490cead56bec10731464a8b2e48f2` and confirmed both underflow guards are live before mutation.
- Read back `BulkEditNumericNoOpSmoke.cs` from the same head and confirmed focused atomicity/zero/subnormal regressions are present.
- Executable .NET build/smoke: `NOT RUN` in this connector-only lane.
- GitHub Actions: `NOT DISPATCHED` under `CI_POLICY.md`.
- BricsCAD V25/V26 runtime: `NOT RUN`; no runtime PASS claimed.

## Excluded scope

- `SemanticNumber.Get()` regeneration parsing (already completed independently).
- `QuantityMath` arithmetic (already completed independently).
- Formula parsing, measured-solid parsing, Curtain/Grid/geometry arithmetic, persistence, native CAD/runtime, release/workflow changes.

## Coordination

Recent BulkEdit numeric no-op work addressed lexical x1 no-ops, not underflow. No overlapping ACTIVE claim was found for this underflow scope before implementation. Source/test writes were based on current blobs and read back from `main` after landing.

## Completion condition

Satisfied: product guard and focused regression are on `main`, source/test readback confirms the intended behavior, and this claim is closed with exact commit evidence.
